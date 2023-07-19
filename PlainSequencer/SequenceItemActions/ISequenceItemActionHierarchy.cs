using PlainSequencer.Script;
using System;
using System.Collections.Generic;

namespace PlainSequencer.SequenceItemActions
{
    public interface ISequenceItemActionHierarchy
    {
        DateTime Started { get; }

        DateTime Finished { get; }

        string Name { get; }

        //string Notes { get; }

        SequenceItem SequenceItem { get; }

        ISequenceItemActionHierarchy Parent { get; set; }

        List<ISequenceItemActionHierarchy> Children { get; }

        SequenceItem[] NextSequenceItems { get; }

        string[] GetParents();

        string FullAncestryName { get; }

        string PeerUniqueFullName { get; }

        string PeerUniqueWithRetryIndexName { get; }
    }
}