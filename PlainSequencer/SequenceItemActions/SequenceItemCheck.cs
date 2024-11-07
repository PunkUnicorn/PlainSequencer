using Newtonsoft.Json;
using PlainSequencer.Logging;
using PlainSequencer.Options;
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
    public class SequenceItemCheckException : Exception
    {
        public SequenceItemCheckException(SequenceItem check, SequenceItemCheck checker, object scribanModel)
            : base($"Failed check: '{check.name}'\nWith pass template: '{check.check.pass_template}'\nFail message: {check.check.FailMessage(scribanModel)}")
        {
            SequenceItem = check;
            Checker = checker;
        }

        public SequenceItem SequenceItem { get; }
        public SequenceItemCheck Checker { get; }
    }

    public class SequenceItemCheck : SequenceItemAbstract, ISequenceItemAction, ISequenceItemActionRun, ISequenceItemActionHierarchy
    {
        public SequenceItemCheck(ILogSequence logProgress, ISequenceSession session, ICommandLineOptions commandLineOptions, ISequenceItemActionBuilderFactory itemActionBuilderFactory, SequenceItemCreateParams @params)
            : base(logProgress, session, commandLineOptions, itemActionBuilderFactory, @params) { }

        public IEnumerable<string> Compile(SequenceItem sequenceItem)
        {
            return new string[] { };
        }

        protected override async Task<object> ActionAsyncInternal(CancellationToken cancelToken)
        {
            return await FailableRun<object>(logProgress, this, async delegate
            {
                if (this.sequenceItem.check == null)
                    throw new NullReferenceException($"{nameof(this.sequenceItem)}.{nameof(this.sequenceItem.check)} missing");

                // Do retries within a failable run so only the final fail is registered, but increment ActionExecuteCount with each retry
                var w = Policy.Handle<Exception>()
                    .WaitAndRetryAsync(this.SequenceItem.max_retries ?? 0, (i) => TimeSpan.FromSeconds(1));

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
                return await w.ExecuteAsync(async () =>
                {
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
                    ++this.ActionExecuteCount;

                    var scribanModel = MakeScribanModel();

                    var result = this.sequenceItem.check.IsPass(scribanModel);
                    TextResponse = result.ToString();
                    BytesResponse = GetBytes(TextResponse);
                    ActionResult = this.model;

                    if (result)
                    {
                        this.logProgress?.Progress(this, $"Passed.", SequenceProgressLogLevel.Brief);
                        this.logProgress?.Progress(this, $"With template:\n{sequenceItem.check.pass_template}\nWith model:\n{JsonConvert.SerializeObject(this.model, Formatting.Indented)}", SequenceProgressLogLevel.Diagnostic);
                    }
                    else
                        Fail(new SequenceItemCheckException(this.sequenceItem, this, scribanModel));

                    await DoInlineSaveAsync(ActionResult, scribanModel, sequenceItem.check.save, sequenceItem.check.saves);

                    return ActionResult;
                });
            });
        }
    }
}