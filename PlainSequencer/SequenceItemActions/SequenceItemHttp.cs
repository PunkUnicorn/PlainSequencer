using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using PlainSequencer.Autofac;
using PlainSequencer.Logging;
using PlainSequencer.Options;
using PlainSequencer.Scriban;
using PlainSequencer.Script;
using PlainSequencer.SequenceItemSupport;
using PlainSequencer.Stuff.Interfaces;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static PlainSequencer.SequenceItemActions.SequenceItemStatic;

namespace PlainSequencer.SequenceItemActions
{
    public class SequenceItemHttp : SequenceItemAbstract, ISequenceItemAction, ISequenceItemActionRun, ISequenceItemActionHierarchy
    {
        public string WorkingMethod { get; private set; }
        public string WorkingUri { get; private set; }
        public string WorkingBody { get; private set; }

        [AutofacInjected]
        public IHttpClientProvider HttpClientProvider { get; set; }

        public SequenceItemHttp(ILogSequence logProgress, ISequenceSession session, ICommandLineOptions commandLineOptions, ISequenceItemActionBuilderFactory itemActionBuilderFactory, SequenceItemCreateParams @params)
            : base(logProgress, session, commandLineOptions, itemActionBuilderFactory, @params) { }

        public IEnumerable<string> Compile(SequenceItem sequenceItem)
        {
            return new string[] { };
        }

        protected override async Task<object> ActionAsyncInternal(CancellationToken cancelToken)
        {
            return await FailableRun<object>(logProgress, this, async delegate 
            {
                var w = Policy.Handle<Exception>()
                    .WaitAndRetryAsync(this.SequenceItem.max_retries ?? 0, (i) => TimeSpan.FromSeconds(1));

                if (this.sequenceItem.http == null)
                    throw new NullReferenceException($"{nameof(this.sequenceItem)}.{nameof(this.sequenceItem.http)} missing");

                var modelUrl = this.sequenceItem.http.url ?? "";

                var scribanModel = this.MakeScribanModel();

                this.WorkingUri = ScribanUtil.ScribanParse(this.sequenceItem.http.url, scribanModel);

                var request = MakeRequestWithHeaders(this.commandLineOptions, this.session.Script, this.sequenceItem);

                return await w.ExecuteAsync(async () =>
                {
                    ++this.ActionExecuteCount;

                    var http_response = await DoHttpActionAsync(request, scribanModel);
                    var statusCodeFail = !http_response.IsSuccessStatusCode;
                    if (statusCodeFail)
                        Fail($"Http response status code: {http_response.StatusCode}");

                    var responseContentLength = http_response?.Content?.Headers?.ContentLength ?? 0;
                    var responseContent = responseContentLength > 0
                        ? (await http_response?.Content?.ReadAsStringAsync())
                        : string.Empty;

                    LiteralResponse = responseContent;

                    this.logProgress?.DataInProgress(this, $" received {responseContentLength} bytes...", SequenceProgressLogLevel.Diagnostic);
                    dynamic responseModel = SequenceItemStatic.GetResponseItems(this.logProgress, this, responseContent);

                    SaveResponseContentsEtc(responseModel, http_response, responseContentLength, responseContent);

                    ActionResult = responseModel;
                    return ActionResult;
                });
            });
        }

        private async Task<HttpResponseMessage> DoHttpActionAsync(HttpRequestMessage request, object scribanModel)
        {
            this.WorkingMethod = this.sequenceItem.http.method;

            if (this.sequenceItem.http.query != null)
                WorkingUri = AppendQuery(this.WorkingUri, scribanModel);

            if (this.sequenceItem.http?.body != null)
            {
                this.WorkingBody = ScribanUtil.ScribanParse(this.sequenceItem.http.body, scribanModel);

                //if (this.sequenceItem.http.save.filename != null)
                //{
                //    SaveTextRequest(scribanModel, WorkingBody);
                //}
            }

            // Process the response content
            var contentType = request.Headers.Where(w => w.Key.Equals("accept", StringComparison.OrdinalIgnoreCase )).FirstOrDefault().Value.First().ToString();
            return await SortOutHttpMethodAndReturnResultAsync(this.sequenceItem.max_retries, request, this.WorkingMethod, contentType, WorkingBody);
        }

        private string AppendQuery(string workingUri, object scribanModel)
        {
            foreach (var query in this.sequenceItem.http.query)
            {
                var templatedValue = ScribanUtil.ScribanParse(query.Value, scribanModel);
                workingUri = QueryHelpers.AddQueryString(workingUri, query.Key, templatedValue);
            }

            return workingUri;
        }

        //private void SaveTextRequest(object scribanModel, string workingBody)
        //{
        //    var saveBodyFilename = ScribanUtil.ScribanParse(this.sequenceItem.http.save.filename, scribanModel);
        //    var saveBodyPath = Path.GetDirectoryName(saveBodyFilename);
        //    if (!Directory.Exists(saveBodyPath))
        //        Directory.CreateDirectory(saveBodyPath);

        //    File.WriteAllText(saveBodyFilename, workingBody);
        //}

        private void SaveResponseContentsEtc(dynamic responseModel, HttpResponseMessage httpResponse, long responseContentLength, string responseContent)
        {
            // Replace this with DoInlineSaveAsync(...), but take some or all of this code and use it in DoInlineSaveAsync(...)
            if (this.sequenceItem?.http?.save == null)
                return;

            var saveModel = MakeScribanModel();
            var saveModelDict = (IDictionary<string, object>)saveModel;
            saveModelDict.Add("response", responseModel);
            saveModelDict.Add("unique_string", Guid.NewGuid().ToString());

            var folderSaveName = this.sequenceItem.http.save?.working_directory ?? "";
            var saveTo = ScribanUtil.ScribanParse(folderSaveName, saveModel);

            var contentSaveName = this.sequenceItem.http.save?.filename ?? "";
            if (contentSaveName.Trim().Length > 0)
            {
                var contentFn = Path.Combine(saveTo, ScribanUtil.ScribanParse(contentSaveName, saveModel));
                this.logProgress?.Progress(this, $" saving content to '{contentFn }'...", SequenceProgressLogLevel.Diagnostic);

                Directory.CreateDirectory(Path.GetDirectoryName(contentFn));

                if (this.sequenceItem.http.save.is_content_binary)
                {
                    if (httpResponse != null) 
                        using (Stream output = File.OpenWrite(contentFn)) 
                            { Task.WaitAll(httpResponse.Content.CopyToAsync(output)); }
                }
                else
                    File.WriteAllText(contentFn, responseContent);
            }

            var responseSaveName = this.sequenceItem.http.save?.filename ?? "";
            if (httpResponse != null && responseSaveName.Trim().Length > 0)
            {
                var nonContentFn = Path.Combine(saveTo, ScribanUtil.ScribanParse(responseSaveName, saveModel));
                this.logProgress?.Progress(this, $" saving response info to '{nonContentFn}'...", SequenceProgressLogLevel.Diagnostic);

                // Yeah,... add these to the scriban model and then they can template out whatever save they want
                var nonContent = new
                {
                    httpResponse.StatusCode,
                    httpResponse.Headers,
                    httpResponse.ReasonPhrase,
                    httpResponse.IsSuccessStatusCode,
                    //httpResponse.TrailingHeaders,
                    httpResponse.RequestMessage,
                    ContentLength = responseContentLength
                };

                Directory.CreateDirectory(Path.GetDirectoryName(nonContentFn));
                File.WriteAllText(nonContentFn, JsonConvert.SerializeObject(nonContent));
            }
        }

        private async Task<HttpResponseMessage> SortOutHttpMethodAndReturnResultAsync(int? instantRetryCount, HttpRequestMessage request, string method, string mediaType, string workingBody)
        {
            // Need to change this to use a HttpRequestMessage instead of relying on the default headers.. :/
            // and clear the default request headers in the provider
            try
            {
                request.Method = new HttpMethod(method);
                request.RequestUri = new Uri(this.WorkingUri);

                if (request.Method != new HttpMethod("get"))
                    request.Content = new StringContent(workingBody, Encoding.UTF8, mediaType);

                var attemptDescription = ActionExecuteCount == 1
                    ? ""
                    : $" (retry {ActionExecuteCount-1})";

                logProgress?.DataOutProgress(this, $"HTTP {method} {WorkingUri}{attemptDescription}...", SequenceProgressLogLevel.Brief);
                if (this.SequenceItem.http.body != null)
                    logProgress?.DataOutProgress(this, $" using content body \n'{WorkingBody}'\n...", SequenceProgressLogLevel.Diagnostic);

                return await HttpClientProvider.Client.SendAsync(request);

            }
            catch (Exception e)
            {
                throw;
            }
        }

        private HttpRequestMessage MakeRequestWithHeaders(ICommandLineOptions o, SequenceScript yaml, SequenceItem entry = null)
        {
            var retval = new HttpRequestMessage();
            //var client = new HttpClient(); //HttpClientProvider.Client;

            //const int defaultClientTimeoutSeconds = 90;
            //client.Timeout = TimeSpan.FromSeconds(yaml.client_timeout_seconds ?? defaultClientTimeoutSeconds);

            var scribanModel = MakeScribanModel();

            //client.DefaultRequestHeaders.Accept.Clear();
            if (entry?.http?.header != null)
            {

                /* UN TESTED  - for POST*/

                //Add customer headers
                foreach (var addHeader in entry.http.header)
                    if (addHeader.Key.Equals("accept", StringComparison.InvariantCultureIgnoreCase))
                    {                        
                        var vals = (string)ScribanUtil.ScribanParse(addHeader.Value, scribanModel);
                        foreach (var val in vals.Split(',').Select(s => s.Trim()))
                        { 
                            var queryParts = val.Split(';');
                            if (queryParts.Length > 1)
                            {
                                retval.Headers.Add("accept", new MediaTypeWithQualityHeaderValue(queryParts[0]).MediaType);
                                foreach (var queryPart in queryParts.Skip(1))
                                {
                                    var keyVal = queryPart.Split('=');
                                    retval.Headers.Add(keyVal[0], keyVal[1]);
                                }
                            }
                            else retval.Headers.Add("accept", new MediaTypeWithQualityHeaderValue(val).MediaType);
                        }
                    }
                    else
                    {
                        retval.Headers.Add(addHeader.Key, ScribanUtil.ScribanParse(addHeader.Value, scribanModel));
                    }
            }

            var contentType = entry.http.content_type ?? "text/plain";

            if (retval.Headers.Accept.Count == 0)
                retval.Headers.Add("accept", new MediaTypeWithQualityHeaderValue(contentType).MediaType);

            return retval;
        }
    }
}
