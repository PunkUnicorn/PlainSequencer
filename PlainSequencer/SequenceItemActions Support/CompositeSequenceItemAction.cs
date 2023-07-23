using Newtonsoft.Json;
using PlainSequencer.Script;
using PlainSequencer.SequenceItemActions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static PlainSequencer.SequenceItemActions.ISequenceItemActionRun;

namespace PlainSequencer.SequenceItemSupport
{
    public class CompositeSequenceItemAction : ISequenceItemAction, ISequenceItemActionHierarchy, ISequenceItemActionRun, ISequenceItemResult
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
            set => throw new NotImplementedException();
        }

        public DateTime Finished
        {
            get => Children
                ?.Where(child => child != null)
                ?.Cast<ISequenceItemActionRun>()
                ?.Max(child => child.Finished) ?? DateTime.MinValue;
            set => throw new NotImplementedException();
        }
        public string Name => SequenceItem.name; //string.Join(",", Children
                                                 //?.Where(child => child != null)
                                                 //?.Cast<ISequenceItemActionHierarchy>()
                                                 //?.Select(child => child.Name));

        string ISequenceItemActionHierarchy.FullAncestryName => throw new NotImplementedException();

        public string FullAncestryWithPeerName => throw new NotImplementedException();

        public string FullAncestryWithPeerWithRetryName => throw new NotImplementedException();

        public int ActionExecuteCount { get; set; }

        public object Model { get; set; }

        public bool IsItemSuccess => throw new InvalidOperationException();

        private bool isFail;
        public bool IsFail =>
            isFail ||
            (Children?.Any(child => child == null) ?? false) || 
            (Children?.Cast<ISequenceItemResult>()?.Any(child => child.IsFail) ?? false);

        private string failMessage;
        private Exception exception;
        public Exception Exception
        {
            get
            {
                var exceptions = Children?.Cast<ISequenceItemResult>()?.Select(child => child.Exception) ?? new Exception[] { };

                if (exception != null)
                    exceptions = new[] { exception }.Concat(exceptions);

                if (!exceptions.Any())
                    return null;

                if (exceptions.Take(2).Count() == 1)
                    return exceptions.First();
                else if(exceptions.Take(2).Count() > 1)
                    return new AggregateException("Composite contains exceptions", exceptions);

                return null;
            }
            set
            {
                exception = value;
            }
        }

        public string LiteralResponse
        {
            get
            {
                var responses = Children?.Cast<ISequenceItemResult>()?.Select(child => child.LiteralResponse) ?? new string[] { };
                return JsonConvert.SerializeObject(responses.ToArray());
            }
        }

        public object ActionResult
        {
            get
            {
                var allResults = Children?.Cast<ISequenceItemResult>()?.Select(s => s.ActionResult) ?? new object[] { };

                return allResults.ToArray();
            }
        }
        public Dictionary<string, object> NewVariables { get => throw new NotImplementedException(); }

        public void NullResult() => throw new NotImplementedException();
        public void BlankResult() => throw new NotImplementedException();

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
            foreach (var run in Children.Cast<ISequenceItemActionRun>())
                await run.ActionAsync(addToFailHoleAsync, cancelToken);

            // return the results array in the same order as the children nodes
            var allResults = Children
                .Cast<ISequenceItemResult>()
                .Select(s => s.ActionResult)
                .Where(task => task != null);

            return allResults.ToArray();
        }

        public IEnumerable<string> Compile(SequenceItem sequenceItem)
        {
            throw new System.NotImplementedException();
        }

        public ISequenceItemResult Fail(Exception e = null)
        {
            failMessage = e?.Message;
            exception = e;
            isFail = true;
            return this;
        }

        public ISequenceItemResult Fail(string msg, Exception e = null)
        {
            failMessage = msg;
            exception = e;
            isFail = true;
            return this;
        }

        public ISequenceItemActionHierarchy[] GetParents() => throw new NotImplementedException();

        public string[] GetParentsNames() => throw new NotImplementedException();//Children.FirstOrDefault()?.GetParents();

        public string FullAncestryName() => Children.FirstOrDefault()?.FullAncestryName;

        public int PeerIndex => throw new NotImplementedException();

        public string SequenceDiagramNotation => throw new NotImplementedException();

        public string SequenceDiagramKey => throw new NotImplementedException();

        public string FailMessage
        {
            get
            {
                var allResults = new[] { failMessage }.Concat(Children
                    .Cast<ISequenceItemResult>()
                    .Select(s => s.FailMessage)
                    .Where(msg => msg != null));

                return string.Join("\nAND\n", allResults);
            }
        }
    }
}