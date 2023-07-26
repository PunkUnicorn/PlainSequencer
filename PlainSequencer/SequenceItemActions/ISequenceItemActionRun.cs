using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlainSequencer.SequenceItemActions
{
    public interface ISequenceItemActionRun
    {
        //delegate Task AddToFailHoleAsync(SequenceItemAbstract sequenceItemAbstract, IDictionary<string, object> scribanModel, CancellationToken cancellationToken);

        //Task<object> ActionAsync(AddToFailHoleAsync addToFailHoleAsync, CancellationToken cancelToken);
        // ^ I think this should be in ISequenceItemAction, and then composite doesn't take ISequenceActionRun
        // Possibly Model too, but also in ..ActionRun(?)

        int ActionExecuteCount { get; set; }

        //object Model { get; }

        DateTime Started { get; set; }

        DateTime Finished { get; set; }

        //string SequenceDiagramNotation { get; }

        //string SequenceDiagramKey { get; }

        Exception Exception { get; set; }

        ISequenceItemResult Fail(string msg, Exception e = null);
        ISequenceItemResult Fail(Exception e = null);

        void NullResult();

        void BlankResult();
    }
}