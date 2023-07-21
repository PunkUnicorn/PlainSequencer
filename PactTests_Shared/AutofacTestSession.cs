using Autofac;
using PlainSequencer;
using PlainSequencer.Autofac;
using PlainSequencer.Logging;
using PlainSequencer.Options;
using PlainSequencer.SequenceItemSupport;
using PlainSequencer.SequenceScriptLoader;
using PlainSequencer.Stuff;
using PlainSequencer.Stuff.Interfaces;
using static PlainSequencer.Program;

namespace PactTests_Shared
{
    public static class AutofacTestSession
    {
        private static ConsoleOutputterTest testOutput => new ConsoleOutputterTest();
        public static string TestOutput => testOutput.Output;

        public static IContainer ConfigureTestSession(CommandLineOptions testOptions)
        {
            var builder = new ContainerBuilder();

            if (testOptions != null)
                builder.RegisterInstance(testOptions).As<ICommandLineOptions>()
                    .SingleInstance();

            builder.RegisterType<ConsoleOutputterTest>()
                .As<IConsoleOutputter>()
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<ServiceGlobalData>().As<IServiceGlobalData>()
                .SingleInstance();

            builder.RegisterType<ServiceFileData>().As<IServiceFileData>()
                .SingleInstance();

            builder.RegisterType<LogSequence>().As<ILogSequence>()
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
