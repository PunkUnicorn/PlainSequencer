using Newtonsoft.Json;
using PlainSequencer.Logging;
using PlainSequencer.Options;
using PlainSequencer.Script;
using PlainSequencer.SequenceItemSupport;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static PlainSequencer.SequenceItemActions.SequenceItemStatic;

namespace PlainSequencer.SequenceItemActions
{
    public class SequenceItemCheckException : Exception
    {
        public SequenceItemCheckException(SequenceItem check, SequenceItemCheck checker, object model)
            : base($"{check.name} - Failed check '{check.check.pass_template}'\n{check.check.FailMessage(model)}\n...with model:\n{JsonConvert.SerializeObject(model ?? "null", Formatting.Indented)}")
        {
            SequenceItem = check;
            Checker = checker;
            Model = model;
        }

        public SequenceItem SequenceItem { get; }
        public SequenceItemCheck Checker { get; }
        public object Model { get; }
    }

    public class SequenceItemCheck : SequenceItemAbstract, ISequenceItemAction, ISequenceItemActionRun, ISequenceItemActionHierarchy
    {
        public SequenceItemCheck(IProgressLogger logProgress, ISequenceSession session, ICommandLineOptions commandLineOptions, ISequenceItemActionBuilderFactory itemActionBuilderFactory, SequenceItemCreateParams @params)
            : base(logProgress, session, commandLineOptions, itemActionBuilderFactory, @params) { }

        public IEnumerable<string> Compile(SequenceItem sequenceItem)
        {
            return new string[] { };
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected override async Task<object> ActionAsyncInternal(CancellationToken cancelToken)
        {
            return await FailableRun<object>(this, async delegate
            {
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
                ++this.ActionExecuteCount;

                if (this.sequenceItem.check == null)
                    throw new NullReferenceException($"{nameof(this.sequenceItem)}.{nameof(this.sequenceItem.check)} missing");

                //var scribanModel = new
                //{
                //    run_id = this.session.RunId,
                //    command_args = this.commandLineOptions,
                //    this.model,
                //    sequence_item = this.sequenceItem,
                //    unique_no = session.UniqueNo
                //};
                var scribanModel = MakeScribanModel();

                var result = this.sequenceItem.check.IsPass(scribanModel);
                LiteralResponse = result.ToString();
                var resultString = result ? "Success" : "Fail";
                this.logProgress?.Progress(this, $":{resultString}:{sequenceItem.name}");
                ActionResult = this.model;

                if (!result)
                    Fail( new SequenceItemCheckException(this.sequenceItem, this, scribanModel));

                return ActionResult;
            });
        }
    }
}