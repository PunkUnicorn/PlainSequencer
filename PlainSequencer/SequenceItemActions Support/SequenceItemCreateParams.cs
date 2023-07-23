using PlainSequencer.Script;
using PlainSequencer.SequenceItemActions;
using System;
using System.Collections.Generic;

namespace PlainSequencer.SequenceItemSupport
{
    public class SequenceItemCreateParams : ISequenceItemActionHierarchy
    {
        public ISequenceItemActionHierarchy Parent { get; set; }
        public SequenceItem[] NextSequenceItems { get; set; }
        public SequenceItem SequenceItem { get; set; }
        public int PeerIndex { get; set; }

        public object Model { get; set; }

        public List<ISequenceItemActionHierarchy> Children => throw new System.NotImplementedException($"Cannot access {nameof(Children)} before it's created");

        public DateTime Started => throw new System.NotImplementedException(nameof(Started));

        public DateTime Finished => throw new NotImplementedException(nameof(Finished));

        public string Name => throw new NotImplementedException(nameof(Name));

        public string FullAncestryWithPeerName => throw new NotImplementedException();

        public string FullAncestryWithPeerWithRetryName => throw new NotImplementedException();

        string ISequenceItemActionHierarchy.FullAncestryName => throw new NotImplementedException();

        public string FullAncestryName() => throw new NotImplementedException();

        public ISequenceItemActionHierarchy[] GetParents() => throw new NotImplementedException();

        public string[] GetParentsNames() => throw new NotImplementedException();
    }
}