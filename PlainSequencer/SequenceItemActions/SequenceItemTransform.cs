using PlainSequencer.Logging;
using PlainSequencer.Options;
using PlainSequencer.Scriban;
using PlainSequencer.Script;
using PlainSequencer.SequenceItemSupport;
using Polly;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static PlainSequencer.SequenceItemActions.SequenceItemStatic;

namespace PlainSequencer.SequenceItemActions
{

    public class SequenceItemTransform : SequenceItemAbstract, ISequenceItemAction, ISequenceItemActionRun, ISequenceItemActionHierarchy
	{
		public SequenceItemTransform(ILogSequence logProgress, ISequenceSession session, ICommandLineOptions commandLineOptions, ISequenceItemActionBuilderFactory itemActionBuilderFactory, SequenceItemCreateParams @params)
			: base(logProgress, session, commandLineOptions, itemActionBuilderFactory, @params) { }

		public IEnumerable<string> Compile(SequenceItem sequenceItem)
		{
			return new string[] { };
		}

		protected override async Task<object> ActionAsyncInternal(CancellationToken cancelToken) 
		{
			return await FailableRun(logProgress, this, async delegate 
			{
				if (this.sequenceItem.transform == null)
					throw new NullReferenceException($"{nameof(this.sequenceItem)}.{nameof(this.sequenceItem.transform)} missing");

				var w = Policy.Handle<Exception>()
					.WaitAndRetryAsync(this.SequenceItem.max_retries ?? 0, (i) => TimeSpan.FromSeconds(1));

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
                return await w.ExecuteAsync(async () =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
                {
                    ++this.ActionExecuteCount;

                    this.logProgress?.Progress(this, $"Transforming model...", SequenceProgressLogLevel.Brief);

                    this.logProgress?.Progress(this, $"Transforming:\n{this.sequenceItem.transform.new_model_template}", SequenceProgressLogLevel.Diagnostic);

                    var scribanModel = MakeScribanModel();

                    var scribanProcessedTemplate = ScribanUtil.ScribanParse(this.sequenceItem.transform.new_model_template, scribanModel);

                    LiteralResponse = scribanProcessedTemplate;
                    this.logProgress?.Progress(this, $"Transformed to:\n{scribanProcessedTemplate}", SequenceProgressLogLevel.Diagnostic);

                    if (this.sequenceItem.transform.new_model_is_plain_text)
                        ActionResult = scribanProcessedTemplate;
                    else
                        ActionResult = SequenceItemStatic.GetResponseItems(this.logProgress, this, scribanProcessedTemplate);

                    await DoInlineSaveAsync(ActionResult, scribanModel, sequenceItem.transform.save, sequenceItem.transform.saves);

                    return ActionResult;
                });
			});
		}
    }
}