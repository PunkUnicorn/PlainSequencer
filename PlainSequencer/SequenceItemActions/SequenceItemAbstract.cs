using PlainSequencer.Logging;
using PlainSequencer.Options;
using PlainSequencer.Script;
using PlainSequencer.SequenceItemSupport;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static PlainSequencer.SequenceItemActions.SequenceItemStatic;

namespace PlainSequencer.SequenceItemActions
{
    public abstract class SequenceItemAbstract : ISequenceItemResult, ISequenceItemActionRun, ISequenceItemActionHierarchy
    {
        // https://stackoverflow.com/questions/49595198/autofac-resolving-through-factory-methods

        private readonly ISequenceItemActionBuilderFactory itemActionBuilderFactory;
        protected readonly object model;
        protected readonly ICommandLineOptions commandLineOptions;
        protected readonly IEnumerable<SequenceItem> nextSequenceItems;
        protected readonly IProgressLogger logProgress;
        protected readonly ISequenceSession session;
        protected readonly SequenceItem sequenceItem;
        
        public string LiteralResponse { get; protected set; }

        public object ActionResult { get; protected set; }

        public SequenceItemAbstract(IProgressLogger logProgress, ISequenceSession session, ICommandLineOptions commandLineOptions, ISequenceItemActionBuilderFactory itemActionBuilderFactory, SequenceItemCreateParams @params)
        {
            this.logProgress = logProgress;
            this.session = session;
            this.sequenceItem = @params.SequenceItem;
            this.model = Clone(@params.Model);
            this.nextSequenceItems = @params.NextSequenceItems;
            this.Parent = @params.Parent;
            this.commandLineOptions = commandLineOptions;
            this.itemActionBuilderFactory = itemActionBuilderFactory;
            Children = new List<ISequenceItemActionHierarchy>();
        }

        protected object MakeScribanModel()  
        {
            var retval = new ExpandoObject() as dynamic;
            retval.now = $"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}";
            retval.run_id = this.session.RunId;
            retval.command_args = this.commandLineOptions;
            retval.model = this.model;
            retval.sequence_item = this.sequenceItem;
            retval.prev_sequence_item = this?.Parent?.SequenceItem;
            retval.next_sequence_items = this.NextSequenceItems;
            retval.unique_no = session.UniqueNo;
            var asDict = (IDictionary<string, object>)retval;
            return asDict.ToDictionary(k => k.Key, v => v.Value);
        }


        public int ActionExecuteCount { get; set; }

        public ISequenceItemActionHierarchy Parent { get; set; }

        public List<ISequenceItemActionHierarchy> Children { get; }

        public SequenceItem[] NextSequenceItems => this.nextSequenceItems.ToArray();

        public SequenceItem SequenceItem => this.sequenceItem;

        public object Model => this.model;

        public string FailMessage => this.failMessage;

        public ISequenceItemResult Fail(Exception e=null)
        {
            if (e != null)
                failMessage = e.Message;

            isFail = true;
            Exception = e ?? Exception;
            return this;
        }

        public ISequenceItemResult Fail(string msg, Exception e=null)
        {
            failMessage = msg;
            isFail = true;
            Exception = e ?? Exception;
            return this;
        }

        private bool isFail;
        private string failMessage;

        public bool IsFail 
        { 
            get 
            { 
                if (isFail)
                    return true;

                return Children?.Cast<ISequenceItemResult>()?.Any(child => child.IsFail) ?? false;
            } 
        }

        public Exception Exception { get; set; }

        protected abstract Task<object> ActionAsyncInternal(CancellationToken cancelToken);

        public async Task<object> ActionAsync(CancellationToken cancelToken)
        {
            var nextModel = await ActionAsyncInternal(cancelToken);
            return await CascadeNextActionAsync(cancelToken, nextModel);
        }

        protected async Task<object> CascadeNextActionAsync(CancellationToken cancelToken, object nextModel)
        {
            var nextItem = nextSequenceItems.FirstOrDefault();
            var nextItemsNextItems = nextSequenceItems.Skip(1).ToArray();

            if (nextItem == null)
                return nextModel;

            var runItem = itemActionBuilderFactory.Fetch(this, nextModel, nextItem, nextItemsNextItems);
            return await runItem.ActionAsync(cancelToken);
        }
    }
}