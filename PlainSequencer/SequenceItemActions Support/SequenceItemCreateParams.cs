using PlainSequencer.Script;
using PlainSequencer.SequenceItemActions;
using System.Collections.Generic;

namespace PlainSequencer.SequenceItemSupport
{
    public class SequenceItemCreateParams : ISequenceItemActionHierarchy
    {
        public ISequenceItemActionHierarchy Parent { get; set; }
        public SequenceItem[] NextSequenceItems { get; set; }
        public SequenceItem SequenceItem { get; set; }
        public object Model { get; set; }

        public List<ISequenceItemActionHierarchy> Children => throw new System.NotImplementedException($"Cannot access {nameof(Children)} before it's created");
    }
}