using Autofac;
using PlainSequencer.SequenceItemSupport;

namespace PlainSequencer.Autofac
{
    // C# factory with Autofac
    // https://stackoverflow.com/questions/49595198/autofac-resolving-through-factory-methods

    public class AutofacSequenceItemActionBuilderFactory : ISequenceItemActionBuilderFactory
    {
        private readonly ILifetimeScope scope;
        public AutofacSequenceItemActionBuilderFactory(ILifetimeScope scope) => this.scope = scope;
        public ISequenceItemActionBuilder ResolveSequenceItemActionBuilder() => scope.Resolve<ISequenceItemActionBuilder>();

    }
}

