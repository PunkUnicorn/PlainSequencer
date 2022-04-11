using System.Threading;
using System.Threading.Tasks;

namespace PlainSequencer.SequenceItemActions
{
    public interface ISequenceItemActionRun
    {
        Task<object> ActionAsync(CancellationToken cancelToken);

        int ActionExecuteCount { get; set; }

        object Model { get; }
    }
}