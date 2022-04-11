using PlainSequencer.Script;
using PlainSequencer.SequenceItemActions;

namespace PlainSequencer.SequenceItemSupport
{
    public interface ISequenceItemActionFactory
    {
        ISequenceItemAction ResolveSequenceItemAction(SequenceItem sequenceItem, SequenceItemCreateParams @params);
    }
}