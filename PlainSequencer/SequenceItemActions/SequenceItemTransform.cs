using PlainSequencer.Logging;
using PlainSequencer.Options;
using PlainSequencer.Scriban;
using PlainSequencer.Script;
using PlainSequencer.SequenceItemSupport;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static PlainSequencer.SequenceItemActions.SequenceItemStatic;

namespace PlainSequencer.SequenceItemActions
{

    public class SequenceItemTransform : SequenceItemAbstract, ISequenceItemAction, ISequenceItemActionRun, ISequenceItemActionHierarchy
	{
		public SequenceItemTransform(IProgressLogger logProgress, ISequenceSession session, ICommandLineOptions commandLineOptions, ISequenceItemActionBuilderFactory itemActionBuilderFactory, SequenceItemCreateParams @params)
			: base(logProgress, session, commandLineOptions, itemActionBuilderFactory, @params) { }

		public IEnumerable<string> Compile(SequenceItem sequenceItem)
		{
			return new string[] { };
		}

		protected override async Task<object> ActionAsyncInternal(CancellationToken cancelToken) 
		{
			return await FailableRun<object>(this, async delegate {
				++this.ActionExecuteCount;

				if (this.sequenceItem.transform == null)
					throw new NullReferenceException($"{nameof(this.sequenceItem)}.{nameof(this.sequenceItem.transform)} missing");

				this.logProgress?.Progress(this, $"Transforming model {this.sequenceItem.transform.new_model_template}...");

				var scribanModel = MakeScribanModel(); 

                var scribanProcessedTemplate = ScribanUtil.ScribanParse(this.sequenceItem.transform.new_model_template, scribanModel);

				LiteralResponse = scribanProcessedTemplate;

				var responseModel = SequenceItemStatic.GetResponseItems(this.sequenceItem, scribanProcessedTemplate);

				ActionResult = responseModel;
				return ActionResult;
			});
		}

	}
}