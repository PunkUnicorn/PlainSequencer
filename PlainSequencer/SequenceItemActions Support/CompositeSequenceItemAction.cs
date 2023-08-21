using PlainSequencer.Script;
using PlainSequencer.SequenceItemActions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static PlainSequencer.SequenceItemActions.ISequenceItemAction;

namespace PlainSequencer.SequenceItemSupport
{
    public class CompositeSequenceItemAction : ISequenceItemAction, ISequenceItemActionHierarchy
    {
        public ISequenceItemActionHierarchy Parent { get; set; }

        public List<ISequenceItemActionHierarchy> Children { get; set; }

        public SequenceItem[] NextSequenceItems { get; set; }

        public SequenceItem SequenceItem { get; set; }

        public DateTime Started
        {
            get => Children
                ?.Where(child => child != null)
                ?.Cast<ISequenceItemActionRun>()
                ?.Min(child => child.Started) ?? DateTime.MinValue;
        }

        public DateTime Finished
        {
            get => Children
                ?.Where(child => child != null)
                ?.Cast<ISequenceItemActionRun>()
                ?.Max(child => child.Finished) ?? DateTime.MinValue;
        }
        public string Name => SequenceItem.name;

        string ISequenceItemActionHierarchy.FullAncestryName => throw new NotImplementedException();

        public string FullAncestryWithPeerName => throw new NotImplementedException();

        public string FullAncestryWithPeerWithRetryName => throw new NotImplementedException();

        public object Model { get; set; }

        public static CompositeSequenceItemAction FanOutBuildFrom(ISequenceItemActionBuilder sequenceItemActionBuilder, IEnumerable<object> models)
        {
            var retval = new List<ISequenceItemActionHierarchy>();

            var first = SequenceItemStatic.Clone(models?.FirstOrDefault());
            if (first == null) return null;

            var peerIndex = 0;
            var firstNode = sequenceItemActionBuilder
                .WithThisResponseModel(first)
                .WithThisPeerIndex(++peerIndex)
                .Build();

            retval.Add(firstNode as ISequenceItemActionHierarchy);

            foreach (object others in models.Skip(1))
            {
                var itemAction = sequenceItemActionBuilder
                    .Clone()
                    .WithThisResponseModel(others)
                    .WithThisPeerIndex(++peerIndex)
                    .Build();

                retval.Add(itemAction as ISequenceItemActionHierarchy);
            }

            return new CompositeSequenceItemAction 
            { 
                Children = retval,
                NextSequenceItems = ((ISequenceItemActionHierarchy)firstNode).NextSequenceItems,
                Parent = ((ISequenceItemActionHierarchy)firstNode).Parent,
                SequenceItem = firstNode.SequenceItem,
                Model = models
            };
        }

        public async Task<object> ActionAsync(AddToFailHoleAsync addToFailHoleAsync, CancellationToken cancelToken)
        {
            var results = new Dictionary<int, object>();
            foreach (var run in Children.Cast<ISequenceItemAction>())
            {
                var result = await run.ActionAsync(addToFailHoleAsync, cancelToken);
                var actionRun = (ISequenceItemResult)run;
                var peerIndex = ((ISequenceItemActionHierarchy)run).PeerIndex;
                if (actionRun.IsItemSuccess || this.SequenceItem.is_continue_on_failure)
                    results.Add(peerIndex, result);
            }

            return results.Values.ToArray();
        }

        public ISequenceItemActionHierarchy[] GetParents() => throw new NotImplementedException();

        public int PeerIndex => throw new InvalidOperationException(nameof(PeerIndex));

        public string SequenceDiagramNotation => throw new InvalidOperationException(nameof(SequenceDiagramNotation));

        public string SequenceDiagramKey => throw new InvalidOperationException(nameof(SequenceDiagramKey));

        public bool IsFail
        {
            get => Children
                ?.Where(child => child != null)
                ?.Where(child => child is ISequenceItemResult)
                ?.Cast<ISequenceItemResult>()
                ?.Any(child => child.IsFail) ?? false;
        }
    }
}