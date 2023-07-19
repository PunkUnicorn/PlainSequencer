using PlainSequencer.Script;
using System;
using System.Collections.Generic;

namespace PlainSequencer.SequenceItemActions
{
    public interface ISequenceItemAction
    {
        SequenceItem SequenceItem { get; }

        IEnumerable<string> Compile(SequenceItem sequenceItem);

        DateTime Started { get; set; }

        DateTime Finished { get; set;  }
    }
}