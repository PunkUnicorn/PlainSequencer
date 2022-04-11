using Autofac;
using PlainSequencer.Script;
using PlainSequencer.SequenceItemActions;
using PlainSequencer.SequenceItemSupport;
using System;

namespace PlainSequencer.Autofac
{
    // https://stackoverflow.com/questions/49595198/autofac-resolving-through-factory-methods

    public class AutofacSequenceItemActionFactory : ISequenceItemActionFactory
    {
        private readonly ILifetimeScope scope;

        public AutofacSequenceItemActionFactory(ILifetimeScope scope)
        {
            this.scope = scope;
        }

        public ISequenceItemAction ResolveSequenceItemAction(SequenceItem sequenceItem, SequenceItemCreateParams @params) 
        {
            var parameters = new[] { new NamedParameter("params", @params) };

            var type = GetSequenceItemType(sequenceItem);

            if (type == nameof(SequenceItem.http))
                return scope.Resolve<SequenceItemHttp>(parameters);
            else if (type == nameof(SequenceItem.run))
                return scope.Resolve<SequenceItemRun>(parameters);
            else if (type == nameof(SequenceItem.check))
                return scope.Resolve<SequenceItemCheck>(parameters);
            else if (type == nameof(SequenceItem.load))
                return scope.Resolve<SequenceItemLoad>(parameters);
            else if (type == nameof(SequenceItem.transform))
                return scope.Resolve<SequenceItemTransform>(parameters);
            else
                throw new ArgumentException(type);
        }

        private static string GetSequenceItemType(SequenceItem sequenceItem)
        {
            if (sequenceItem.http != null)
                return nameof(SequenceItem.http);
            else if (sequenceItem.run != null)
                return nameof(SequenceItem.run);
            else if (sequenceItem.check != null)
                return nameof(SequenceItem.check);
            else if (sequenceItem.load != null)
                return nameof(SequenceItem.load);
            else if (sequenceItem.transform != null)
                return nameof(SequenceItem.transform);

            throw new ArgumentException($"Unknown sequence item type sequence item named: '{sequenceItem.name}'");
        }
    }
}

