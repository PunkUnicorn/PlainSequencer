using PlainSequencer.Script;
using PlainSequencer.SequenceItemActions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlainSequencer
{
    public interface ISequenceSession
    {
        string RunId { get; }
        ISequenceItemActionHierarchy Top { get; }
        int UniqueNo { get; }
        SequenceScript Script { get; }

        //Task AddToFailHoleAsync(SequenceItemAbstract sequenceItemAbstract, IDictionary<string, object> scribanModel, CancellationToken cancellationToken);
    }
}
