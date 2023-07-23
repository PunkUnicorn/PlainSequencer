using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlainSequencer.SequenceItemActions
{
    public interface ISequenceItemActionRun
    {
        delegate Task AddToFailHoleAsync(SequenceItemAbstract sequenceItemAbstract, IDictionary<string, object> scribanModel, CancellationToken cancellationToken);

        Task<object> ActionAsync(AddToFailHoleAsync addToFailHoleAsync, CancellationToken cancelToken);

        int ActionExecuteCount { get; set; }

        object Model { get; }

        DateTime Started { get; }

        DateTime Finished { get; }

        string SequenceDiagramNotation { get; }

        string SequenceDiagramKey { get; }
    }
}