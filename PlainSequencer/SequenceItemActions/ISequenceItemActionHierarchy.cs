using PlainSequencer.Script;
using System;
using System.Collections.Generic;

namespace PlainSequencer.SequenceItemActions
{
    public interface ISequenceItemActionHierarchy
    {
        string Name { get; }

        SequenceItem SequenceItem { get; }

        ISequenceItemActionHierarchy Parent { get; set; }

        List<ISequenceItemActionHierarchy> Children { get; }

        SequenceItem[] NextSequenceItems { get; }

        ISequenceItemActionHierarchy[] GetParents();

        int PeerIndex { get; }

        string FullAncestryName { get; }

        string FullAncestryWithPeerName { get; }

        string FullAncestryWithPeerWithRetryName { get; }
    }
}