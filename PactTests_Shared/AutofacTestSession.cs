using Autofac;
using PlainSequencer;
using PlainSequencer.Autofac;
using PlainSequencer.Logging;
using PlainSequencer.Options;
using PlainSequencer.SequenceItemSupport;
using PlainSequencer.SequenceScriptLoader;
using PlainSequencer.Stuff;
using static PlainSequencer.Program;

namespace PactTests_Shared
{
    public static class AutofacTestSession
    {
        public static IContainer ConfigureTestSession(CommandLineOptions testOptions)
        {
            var builder = new ContainerBuilder();

            if (testOptions != null)
                builder.RegisterInstance(testOptions).As<ICommandLineOptions>()
                    .SingleInstance();

            builder.RegisterType<SequenceLogger>().As<ISequenceLogger>()
                .SingleInstance();

            builder.RegisterType<Application>()
                .As<ISequenceSession>()
                .As<IApplication>()
                .SingleInstance();

            builder.RegisterInstance(new LoadScript()).As<ILoadScript>()
                .SingleInstance();

            builder.RegisterType<AutofacSequenceItemActionFactory>().As<ISequenceItemActionFactory>()
                .SingleInstance();

            builder.RegisterType<SequenceItemActionBuilder>().As<ISequenceItemActionBuilder>()
                .SingleInstance();

            builder.RegisterType<AutofacSequenceItemActionBuilderFactory>().As<ISequenceItemActionBuilderFactory>()
                .SingleInstance();

            builder.RegisterType<HttpClientProvider>().As<IHttpClientProvider>()
                .SingleInstance();

            ContainerConfig.RegisterSequenceItemActions(builder);

            return builder.Build();
        }

    }
}
