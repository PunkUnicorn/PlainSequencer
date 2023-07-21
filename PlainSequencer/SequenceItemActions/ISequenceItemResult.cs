using System;
using System.Collections.Generic;

namespace PlainSequencer.SequenceItemActions
{
    public interface ISequenceItemResult
    {
        string FailMessage { get; }

        ISequenceItemResult Fail(string msg, Exception e = null);
        ISequenceItemResult Fail(Exception e = null);

        bool IsFail { get; }

        Exception Exception { get; set; }

        string LiteralResponse { get; }

        object ActionResult { get; }

        Dictionary<string, object> NewVariables { get; }

        Dictionary<string, object> NewFileData { get; }

        void NullResult();

        void BlankResult();
    }
}