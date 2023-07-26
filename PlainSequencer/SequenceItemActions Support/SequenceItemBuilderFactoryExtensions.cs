using PlainSequencer.Script;
using PlainSequencer.SequenceItemActions;
using System.Collections.Generic;

namespace PlainSequencer.SequenceItemSupport
{
    public static class SequenceItemBuilderFactoryExtensions
    {
        public static ISequenceItemAction Fetch(this ISequenceItemActionBuilderFactory builderFactory, ISequenceItemActionHierarchy parent, object model, SequenceItem thisItem, SequenceItem[] nextItems)
        {
            var builder = builderFactory.ResolveSequenceItemActionBuilder()
                    .WithThisSequenceItem(thisItem)
                    .WithAncestory(parent)
                    .WithNextSequenceItemsAs(nextItems)
                    .WithThisPeerIndex(0);

            if (thisItem.is_model_array && model as IEnumerable<object> is null)
                model = new[] { model };

            if (thisItem.is_model_array)
                return FetchComposite(parent, (IEnumerable<object>)model, builder);

            var actionItem = builder
               .WithThisResponseModel(model)
               .Build();

            parent?.Children?.Add(actionItem as ISequenceItemActionHierarchy);
            return actionItem as ISequenceItemAction;
        }

        private static ISequenceItemAction FetchComposite(ISequenceItemActionHierarchy parent, object model, ISequenceItemActionBuilder builder)
        {
            var models = (IEnumerable<object>)model;
            var retval = CompositeSequenceItemAction.FanOutBuildFrom(builder, models);
            
            if (retval is not null)
                parent?.Children?.AddRange(retval.Children);
            //its a child of two parents, the first http item and the compound item!??
            return retval;
        }
    }
}
