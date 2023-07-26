using System;
using System.Collections.Generic;

namespace PlainSequencer.SequenceItemActions
{
    public interface ISequenceItemResult
    {
        string FailMessage { get; }

        bool IsFail { get; }

        bool IsItemSuccess { get; }

        Exception Exception { get; }

        string LiteralResponse { get; }

        object ActionResult { get; }

        Dictionary<string, object> NewVariables { get; }
    }
}