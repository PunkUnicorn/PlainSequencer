using Newtonsoft.Json;
using PlainSequencer.Script;
using PlainSequencer.SequenceItemActions;
using System.Collections.Generic;
using System.Linq;

namespace PlainSequencer.SequenceItemSupport
{
    public class SequenceItemActionBuilder : ISequenceItemActionBuilder
    {
        private object model;
        private SequenceItem sequenceItem;
        private ISequenceItemActionHierarchy parent;
        private SequenceItem[] nextItems;
        private readonly ISequenceItemActionBuilderFactory sequenceItemActionBuilderFactory;
        private readonly ISequenceItemActionFactory sequenceItemActionFactory;
        public SequenceItemActionBuilder(ISequenceItemActionFactory sequenceItemActionFactory, ISequenceItemActionBuilderFactory sequenceItemActionBuilderFactory)
        {
            this.sequenceItemActionFactory = sequenceItemActionFactory;
            this.sequenceItemActionBuilderFactory = sequenceItemActionBuilderFactory;
        }

        public ISequenceItemAction Build()
        {
            var @params = new SequenceItemCreateParams 
            {
                Model = model,
                Parent = parent,
                NextSequenceItems = nextItems,
                SequenceItem = sequenceItem,
            };


            return sequenceItemActionFactory.ResolveSequenceItemAction(sequenceItem, @params);
        }

        public ISequenceItemActionBuilder Clone()
        {
            var builder = sequenceItemActionBuilderFactory.ResolveSequenceItemActionBuilder();

            return builder
                .WithAncestory(parent)
                .WithNextSequenceItemsAs(nextItems)
                .WithThisSequenceItem(sequenceItem)
                .WithThisResponseModel(model);
        }

        public ISequenceItemActionBuilder WithThisSequenceItem(SequenceItem sequenceItem)
        {
            this.sequenceItem = sequenceItem;
            return this;
        }

        public ISequenceItemActionBuilder WithTemplateCompiling(out IEnumerable<string> errors)
        {
            errors = null;




            return this;
        }
        public ISequenceItemActionBuilder WithAncestory(ISequenceItemAction parent)
        {
            this.parent = parent as ISequenceItemActionHierarchy;
            return this;
        }

        public ISequenceItemActionBuilder WithAncestory(ISequenceItemActionHierarchy parent)
        {
            this.parent = parent;
            return this;
        }

        public ISequenceItemActionBuilder WithNextSequenceItemsAs(SequenceItem[] nextItems)
        {
            this.nextItems = nextItems?.ToArray() ?? new SequenceItem[] { };
            return this;
        }

        public ISequenceItemActionBuilder WithThisResponseModel(object model)
        {
            var look2 = JsonConvert.SerializeObject(model, Formatting.Indented);
            this.model = model;
            var look1 = JsonConvert.SerializeObject(model, Formatting.Indented);
            return this;
        }
    }
}
