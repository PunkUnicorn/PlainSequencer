using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using PlainSequencer.Logging;
using PlainSequencer.Options;
using PlainSequencer.Scriban;
using PlainSequencer.Script;
using PlainSequencer.SequenceItemSupport;
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
        public string WorkingUri { get; private set; }
        public string WorkingBody { get; private set; }

        public SequenceItemHttp(IProgressLogger logProgress, ISequenceSession session, ICommandLineOptions commandLineOptions, ISequenceItemActionBuilderFactory itemActionBuilderFactory, SequenceItemCreateParams @params)
            : base(logProgress, session, commandLineOptions, itemActionBuilderFactory, @params) { }

        public IEnumerable<string> Compile(SequenceItem sequenceItem)
        {
            return new string[] { };
        }

        protected override async Task<object> ActionAsyncInternal(CancellationToken cancelToken)
        {
            return await FailableRun<object>(this, async delegate {
                ++this.ActionExecuteCount;
            
                if (this.sequenceItem.http == null)
                    throw new NullReferenceException($"{nameof(this.sequenceItem)}.{nameof(this.sequenceItem.http)} missing");

                var modelUrl = this.sequenceItem.http.url ?? "";

                var scribanModel = this.MakeScribanModel();

                this.WorkingUri = ScribanUtil.ScribanParse(this.sequenceItem.http.url, scribanModel);

                this.logProgress?.Progress(this, $"Processing {this.WorkingUri}...");

                dynamic responseModel = null;
                using (var client = MakeClientWithHeaders(this.commandLineOptions, this.session.Script, this.sequenceItem))
                {
                    var http_response = await DoHttpActionAsync(client, scribanModel);
                    var statusCodeFail = !http_response.IsSuccessStatusCode;
                    if (statusCodeFail)
                        Fail($"Http response status code: {http_response.StatusCode}");

                    var responseContentLength = http_response?.Content?.Headers?.ContentLength ?? 0;
                    var responseContent = responseContentLength > 0
                        ? (await http_response?.Content?.ReadAsStringAsync())
                        : string.Empty;

                    LiteralResponse = responseContent;

                    this.logProgress?.Progress(this, $" received {responseContentLength} bytes...");
                    responseModel = SequenceItemStatic.GetResponseItems(this.sequenceItem, responseContent);

                    SaveResponseContentsEtc(responseModel, http_response, responseContentLength, responseContent);
                }

                ActionResult = responseModel;
                return ActionResult;
            });
        }

        private async Task<HttpResponseMessage> DoHttpActionAsync(HttpClient client, object scribanModel)
        {
            this.logProgress?.Progress(this, $" using method '{this.sequenceItem.http.method}'...");

            if (this.sequenceItem.http.query != null)
                WorkingUri = AppendQuery(this.WorkingUri, scribanModel);

            if (this.sequenceItem.http?.body != null)
            {
                this.WorkingBody = ScribanUtil.ScribanParse(this.sequenceItem.http.body, scribanModel);
                this.logProgress?.Progress(this, $" using content body \n'{this.WorkingBody}'\n...");

                if (this.sequenceItem.http.save.request_content_filename != null)
                {
                    SaveTextRequest(scribanModel, WorkingBody);
                }
            }

            this.logProgress?.Progress(this, $" using url '{this.WorkingUri}'...");

            // Process the response content
            var contentType = client.DefaultRequestHeaders.Where(w => w.Key.Equals("accept", StringComparison.OrdinalIgnoreCase )).FirstOrDefault().Value.First().ToString();
            return await SortOutHttpMethodAndReturnResultAsync(this.sequenceItem.max_retries, client, this.sequenceItem.http.method, contentType, WorkingBody);
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

        private void SaveTextRequest(object scribanModel, string workingBody)
        {
            var saveBodyFilename = ScribanUtil.ScribanParse(this.sequenceItem.http.save.request_content_filename, scribanModel);
            var saveBodyPath = Path.GetDirectoryName(saveBodyFilename);
            if (!Directory.Exists(saveBodyPath))
                Directory.CreateDirectory(saveBodyPath);

            File.WriteAllText(saveBodyFilename, workingBody);
        }

        private void SaveResponseContentsEtc(dynamic responseModel, HttpResponseMessage httpResponse, long responseContentLength, string responseContent)
        {
            if (this.sequenceItem?.http?.save == null)
                return;

            var saveModel = MakeScribanModel();
            var saveModelDict = (IDictionary<string, object>)saveModel;
            saveModelDict.Add("response", responseModel);
            saveModelDict.Add("unique_string", Guid.NewGuid().ToString());

            var folderSaveName = this.sequenceItem.http.save?.folder ?? "";
            var saveTo = ScribanUtil.ScribanParse(folderSaveName, saveModel);

            var contentSaveName = this.sequenceItem.http.save?.response_content_filename ?? "";
            if (contentSaveName.Trim().Length > 0)
            {
                var contentFn = Path.Combine(saveTo, ScribanUtil.ScribanParse(contentSaveName, saveModel));
                this.logProgress?.Progress(this, $" saving content to '{contentFn }'...");

                Directory.CreateDirectory(Path.GetDirectoryName(contentFn));

                if (this.sequenceItem.http.save.response_content_is_binary)
                {
                    if (httpResponse != null) 
                        using (Stream output = File.OpenWrite(contentFn)) 
                            { Task.WaitAll(httpResponse.Content.CopyToAsync(output)); }
                }
                else
                    File.WriteAllText(contentFn, responseContent);
            }

            var responseSaveName = this.sequenceItem.http.save?.response_info_filename ?? "";
            if (httpResponse != null && responseSaveName.Trim().Length > 0)
            {
                var nonContentFn = Path.Combine(saveTo, ScribanUtil.ScribanParse(responseSaveName, saveModel));
                this.logProgress?.Progress(this, $" saving response info to '{nonContentFn}'...");

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

        private async Task<HttpResponseMessage> SortOutHttpMethodAndReturnResultAsync(int? instantRetryCount, HttpClient client, string method, string mediaType, string workingBody)
        {
            try
            {
                HttpResponseMessage methodResult;

                var policy = Policy
                  .Handle<Exception>()
                  .Retry(instantRetryCount ?? 1);

                var ret = policy.Execute<Task<HttpResponseMessage>>(async () => 
                { 
                    var url = this.WorkingUri;
                    switch (method.ToUpper())
                    {
                        case "GET":
                            methodResult = await client.GetAsync(url);
                            break;

                        case "PUT":
                            /*UNTESTED*/
                            methodResult = await client.PutAsync(url, new StringContent(workingBody, Encoding.UTF8, mediaType));
                            break;

                        case "POST":
                            /*UNTESTED*/
                            methodResult = await client.PostAsync(url, new StringContent(workingBody, Encoding.UTF8, mediaType));
                            break;

                        case "PATCH":
                            /*UNTESTED*/
                            methodResult = await client.PatchAsync(url, new StringContent(workingBody, Encoding.UTF8, mediaType));
                            break;

                        default:
                            throw new InvalidOperationException($"Unknown {nameof(method)}: '{method}'");
                    }
                    return methodResult;
                });

                return await ret;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private HttpClient MakeClientWithHeaders(ICommandLineOptions o, SequenceScript yaml, SequenceItem entry = null)
        {
            var client = new HttpClient();

            const int defaultClientTimeoutSeconds = 90;
            client.Timeout = TimeSpan.FromSeconds(yaml.client_timeout_seconds ?? defaultClientTimeoutSeconds);

            //var scribanModel = new { now = $"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}", yaml.run_id, command_args = o, model = new { }, sequence_item = new { }, unique_no = this.session.UniqueNo};
            var scribanModel = MakeScribanModel();

            client.DefaultRequestHeaders.Accept.Clear();
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
                                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(queryParts[0]));
                                foreach (var queryPart in queryParts.Skip(1))
                                {
                                    var keyVal = queryPart.Split('=');
                                    client.DefaultRequestHeaders.Add(keyVal[0], keyVal[1]);
                                }
                            }
                            else client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(val));
                        }
                    }
                    else
                    { 
                        client.DefaultRequestHeaders.Add(addHeader.Key, ScribanUtil.ScribanParse(addHeader.Value, scribanModel));
                    }
            }

            var contenType = entry.http.content_type ?? "text/plain";

            if (client.DefaultRequestHeaders.Accept.Count == 0)
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contenType));

            return client;
        }
    }
}
