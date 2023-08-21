using PlainSequencer.Script;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlainSequencer.SequenceItemActions
{
    public interface ISequenceItemAction
    {
        delegate Task AddToFailHoleAsync(SequenceItemAbstract sequenceItemAbstract, IDictionary<string, object> scribanModel, CancellationToken cancellationToken);

        SequenceItem SequenceItem { get; }

        Task<object> ActionAsync(AddToFailHoleAsync addToFailHoleAsync, CancellationToken cancelToken);

        object Model { get; }

        //IEnumerable<string> Compile(SequenceItem sequenceItem);

        bool IsFail { get; }

        string SequenceDiagramNotation { get; }

        string SequenceDiagramKey { get; }

        // https://graphviz.org/Gallery/directed/git.html
        // https://graphviz.org/Gallery/neato/ER.html
        // https://graphviz.org/Gallery/twopi/happiness.html
        //object GetCallGraph();

        DateTime Started { get; }

        DateTime Finished { get; }
    }
}