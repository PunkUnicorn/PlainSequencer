using PlainSequencer.Script;
using PlainSequencer.SequenceItemActions;
using System.Collections.Generic;

namespace PlainSequencer.SequenceItemSupport
{
    public interface ISequenceItemActionBuilder
    {
        ISequenceItemActionBuilder WithThisSequenceItem(SequenceItem sequenceItem);

        ISequenceItemActionBuilder WithTemplateCompiling(out IEnumerable<string> errors);

        ISequenceItemActionBuilder WithAncestory(ISequenceItemActionHierarchy parent);

        ISequenceItemActionBuilder WithAncestory(ISequenceItemAction parent);

        ISequenceItemActionBuilder WithNextSequenceItemsAs(SequenceItem[] nextItems);

        ISequenceItemActionBuilder WithThisResponseModel(object model);

        ISequenceItemActionBuilder WithThisPeerIndex(int index);

        ISequenceItemActionBuilder Clone();

        ISequenceItemAction Build();
    }
}