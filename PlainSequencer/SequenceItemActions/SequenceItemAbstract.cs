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
using static PlainSequencer.SequenceItemActions.ISequenceItemActionRun;
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
        protected readonly ILogSequence logProgress;
        protected readonly ISequenceSession session;
        protected readonly SequenceItem sequenceItem;
        protected readonly int peerIndex;

        public string LiteralResponse { get; protected set; }

        public object ActionResult { get; protected set; }

        public void NullResult() => ActionResult = null;

        public void BlankResult() => ActionResult = string.Empty;

        public Dictionary<string, object> NewVariables { get; } = new Dictionary<string, object>();

        public SequenceItemAbstract(ILogSequence logProgress, ISequenceSession session, ICommandLineOptions commandLineOptions, ISequenceItemActionBuilderFactory itemActionBuilderFactory, SequenceItemCreateParams @params)
        {
            this.logProgress = logProgress;
            this.session = session;
            this.sequenceItem = @params.SequenceItem;
            this.peerIndex = @params.PeerIndex;
            this.model = Clone(@params.Model);
            this.nextSequenceItems = @params.NextSequenceItems;
            this.Parent = @params.Parent;
            this.commandLineOptions = commandLineOptions;
            this.itemActionBuilderFactory = itemActionBuilderFactory;
            Children = new List<ISequenceItemActionHierarchy>();
        }

        protected object MakeScribanModel()
        {
            // Add mundane items
            var retval = new ExpandoObject() as dynamic;
            retval.now = $"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}";
            retval.run_id = this.session.RunId;
            retval.command_args = this.commandLineOptions;
            retval.model = this.model;
            retval.sequence_item = this.sequenceItem;
            retval.sequence_item_run = (ISequenceItemActionRun)this;
            retval.sequence_item_result = (ISequenceItemResult)this;
            retval.sequence_item_node = (ISequenceItemActionHierarchy)this;
            retval.peer_index = this.peerIndex;
            retval.prev_sequence_item = this?.Parent?.SequenceItem;
            retval.prev_sequence_item_run = (ISequenceItemActionRun)this?.Parent;
            retval.prev_sequence_item_result = (ISequenceItemResult)this?.Parent;
            retval.next_sequence_items = this.NextSequenceItems;
            retval.unique_no = session.UniqueNo;
            var asDict = (IDictionary<string, object>)retval;

            var paintersVariableLayers = new List<ISequenceItemActionHierarchy>() { this };
            for (ISequenceItemActionHierarchy c = this; c.Parent != null; c = c.Parent)
                paintersVariableLayers.Add(c);
            if (this != session.Top && session.Top is not null)
                paintersVariableLayers.Add(session.Top);
            paintersVariableLayers.Reverse();

            foreach (var variableLayer in paintersVariableLayers)
            {
                foreach (var @var in ((SequenceItemAbstract)variableLayer).NewVariables)
                    if (asDict.ContainsKey(var.Key))
                        asDict[var.Key] = var.Value;
                    else
                        asDict.Add(var.Key, var.Value);
            }

            return asDict.ToDictionary(k => k.Key, v => v.Value);
        }

        public int ActionExecuteCount { get; set; }

        public ISequenceItemActionHierarchy Parent { get; set; }

        public List<ISequenceItemActionHierarchy> Children { get; }

        public SequenceItem[] NextSequenceItems => this.nextSequenceItems.ToArray();

        public SequenceItem SequenceItem => this.sequenceItem;

        public int PeerIndex => peerIndex;

        public string FullAncestryName =>
            string.Join(" => ", 
                new[] { this.Name }.Concat(GetParentsNames()).Reverse() 
            );

        public string FullAncestryWithPeerName => $"{FullAncestryName} (FanOut#{peerIndex})";

        public string FullAncestryWithPeerWithRetryName => $"{FullAncestryWithPeerName}(Retry#{ActionExecuteCount-1})";

        public object Model => this.model;

        public string FailMessage => this.failMessage;

        public ISequenceItemResult Fail(Exception e = null)
        {
            if (e != null)
                failMessage = e.Message;

            isFail = true;
            Exception = e ?? Exception;
            return this;
        }

        public ISequenceItemResult Fail(string msg, Exception e = null)
        {
            failMessage = msg;
            isFail = true;
            Exception = e ?? Exception;
            return this;
        }

        private bool isFail;
        private string failMessage;

        public bool IsItemSuccess => !isFail;

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

        public DateTime Started { get; set; }

        public DateTime Finished { get; set; }

        public string SequenceDiagramNotation
        {
            get
            {
                var soloProgress = new LogSequence();
                foreach (var node in GetParents().Reverse().Concat(new[] { this }))
                {
                    var sia = (SequenceItemAbstract)node;
                    soloProgress.StartItem(sia);
                    soloProgress.Progress(sia, $"{sia.LiteralResponse}", SequenceProgressLogLevel.Diagnostic);

                    if (!sia.IsItemSuccess)
                        soloProgress.Fail(sia, sia.FailMessage);

                    soloProgress.FinishedItem(sia);
                }

                return soloProgress.GetSequenceDiagramNotation(null, SequenceProgressLogLevel.Diagnostic);
            }
        }

        public string Name => sequenceItem.name;

        public string SequenceDiagramKey => Name.Replace('-', ' ');

        protected abstract Task<object> ActionAsyncInternal(CancellationToken cancelToken);

        public ISequenceItemActionHierarchy[] GetParents()
        {
            var aboveMe = new List<ISequenceItemActionHierarchy>();
            for (var parent = this.Parent; parent != null; parent = parent.Parent)
                aboveMe.Add(parent);

            return aboveMe.ToArray();
        }

        public string[] GetParentsNames()
        {
            var aboveMe = new List<string>();
            for (var parent = this.Parent; parent != null; parent = parent.Parent)
                aboveMe.Add(parent.Name);

            return aboveMe.ToArray();
        }

        public async Task<object> ActionAsync(AddToFailHoleAsync addToFailHoleAsync, CancellationToken cancelToken)
        {
            var nextModel = await ActionAsyncInternal(cancelToken);
            if (!IsItemSuccess)
                await addToFailHoleAsync(this, (Dictionary<string, object>)MakeScribanModel(), cancelToken);

            if (IsItemSuccess || sequenceItem.is_continue_on_failure)
                return await CascadeNextActionAsync(addToFailHoleAsync, cancelToken, nextModel);

            return nextModel;
        }

        protected async Task<object> CascadeNextActionAsync(AddToFailHoleAsync addToFailHoleAsync, CancellationToken cancelToken, object nextModel)
        {
            var nextItem = nextSequenceItems.FirstOrDefault();
            var nextItemsNextItems = nextSequenceItems.Skip(1);

            if (nextItem == null)
                return nextModel;

            var runItem = itemActionBuilderFactory.Fetch(this, nextModel, nextItem, nextItemsNextItems.ToArray());

            if (runItem is not null)
                return await runItem.ActionAsync(addToFailHoleAsync, cancelToken);

            return nextModel;
        }
    }
}