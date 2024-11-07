using Newtonsoft.Json;
using PlainSequencer.Logging;
using PlainSequencer.Options;
using PlainSequencer.Scriban;
using PlainSequencer.Script;
using PlainSequencer.SequenceItemSupport;
using Polly;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static PlainSequencer.SequenceItemActions.SequenceItemStatic;

namespace PlainSequencer.SequenceItemActions
{

    public class SequenceItemSave : SequenceItemAbstract, ISequenceItemAction, ISequenceItemActionRun, ISequenceItemActionHierarchy
	{
		public SequenceItemSave(ILogSequence logProgress, ISequenceSession session, ICommandLineOptions commandLineOptions, ISequenceItemActionBuilderFactory itemActionBuilderFactory, SequenceItemCreateParams @params)
			: base(logProgress, session, commandLineOptions, itemActionBuilderFactory, @params) { }

		protected override async Task<object> ActionAsyncInternal(CancellationToken cancelToken) 
		{
			return await FailableRun(logProgress, this, async delegate 
			{
				if (this.sequenceItem.save == null)
					throw new NullReferenceException($"{nameof(this.sequenceItem)}.{nameof(this.sequenceItem.save)} missing");

				var w = Policy.Handle<Exception>()
					.WaitAndRetryAsync(this.SequenceItem.max_retries ?? 0, (i) => TimeSpan.FromSeconds(1));

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
                return await w.ExecuteAsync(async () =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
                {
                    ++this.ActionExecuteCount;

                    this.logProgress?.Progress(this, $"Saving model...", SequenceProgressLogLevel.Brief);

                    this.logProgress?.Progress(this, $"Save content template:\n{this.sequenceItem.save.content_template}\nfilename template:{this.sequenceItem.save.filename}", SequenceProgressLogLevel.Diagnostic);

                    var scribanModel = MakeScribanModel();

                    TextResponse = ScribanUtil.ScribanParse(this.sequenceItem.save.content_template, scribanModel);
                    BytesResponse = GetBytes(TextResponse);

                    var scribanProcessedFilename =  await DoSaveAsync(this.sequenceItem.save, TextResponse, scribanModel);

                    this.logProgress?.Progress(this, $"Saved {scribanProcessedFilename}", SequenceProgressLogLevel.Diagnostic);

                    ActionResult = model;

                    return ActionResult;
                });
			});
		}

        internal static async Task<string> DoSaveAsync(Save save, object content, object scribanModel)
        {
            var folderSaveName = save?.working_directory ?? "";
            var saveTo = ScribanUtil.ScribanParse(folderSaveName, scribanModel);

            var contentSaveName = save?.filename ?? "";
            if (contentSaveName.Trim().Length > 0)
            {
                var contentFn = Path.Combine(saveTo, ScribanUtil.ScribanParse(contentSaveName, scribanModel));
                //this.logProgress?.Progress(this, $" saving content to '{contentFn }'...", SequenceProgressLogLevel.Diagnostic);

                Directory.CreateDirectory(Path.GetDirectoryName(contentFn));

                if (save.is_content_binary)
                {
                    if (httpResponse != null)
                        using (Stream output = File.OpenWrite(contentFn))
                        { Task.WaitAll(httpResponse.Content.CopyToAsync(output)); }
                }
                else
                    File.WriteAllText(contentFn, responseContent);
            }


            var filename = ScribanUtil.ScribanParse(save.filename, scribanModel);

            if (save.is_content_binary)
                throw new NotImplementedException();
            else if (content is string)
                await File.WriteAllTextAsync(filename, (string)content);
            else
                await File.WriteAllTextAsync(filename, JsonConvert.SerializeObject(content, Formatting.Indented));

            return filename;
        }
    }
}