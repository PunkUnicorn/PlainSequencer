using Autofac;
using PlainSequencer.Autofac;
using PlainSequencer.Logging;
using PlainSequencer.Options;
using PlainSequencer.SequenceItemActions;
using PlainSequencer.SequenceItemSupport;
using PlainSequencer.SequenceScriptLoader;
using PlainSequencer.Stuff;
using System;
using System.IO;

namespace PlainSequencer
{
    public class Program
    {
        private enum ReturnValues
        {
            Success = 0,
            Fail = 1,
            TestMalfunction = 2,
        }

        static int Main(string[] args)
        {
            string stdin = null;
            if (Console.IsInputRedirected)
                using (StreamReader reader = new StreamReader(Console.OpenStandardInput(), Console.InputEncoding))
                    stdin = reader.ReadToEnd();

            using (var container = ContainerConfig.ConfigureSequenceSession(args))
            using (var scope = container?.BeginLifetimeScope())
            {
                if (scope == null)
                    return (int)ReturnValues.TestMalfunction;

                var app = scope.Resolve<IApplication>();

                var startingModel = Application.TurnStringResponseIntoModel(stdin);

                if (app.RunAsync(startingModel).Result)
                    return (int)ReturnValues.Success;
                else
                    return (int)ReturnValues.Fail;
            }
        }

        public static class ContainerConfig
        {
            public static IContainer ConfigureSequenceSession(string[] args)
            {
                var commandObj = CommandLineOptions.CreateCommandLineOptions(args);
                if (commandObj == null)
                    return null;
                
                var builder = new ContainerBuilder();

                builder.RegisterInstance(commandObj).As<ICommandLineOptions>()
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

                RegisterSequenceItemActions(builder);

                return builder.Build();
            }

            public static void RegisterSequenceItemActions(ContainerBuilder builder)
            {
                builder.RegisterType<SequenceItemCheck>()
                    .PropertiesAutowired(AutofacInjectedAttribute.AutofacInjectedAttributeOnly)
                    .InstancePerDependency();

                builder.RegisterType<SequenceItemTransform>()
                    .PropertiesAutowired(AutofacInjectedAttribute.AutofacInjectedAttributeOnly)
                    .InstancePerDependency();

                builder.RegisterType<SequenceItemHttp>()
                    .PropertiesAutowired(AutofacInjectedAttribute.AutofacInjectedAttributeOnly)
                    .InstancePerDependency();

                builder.RegisterType<SequenceItemLoad>()
                    .PropertiesAutowired(AutofacInjectedAttribute.AutofacInjectedAttributeOnly)
                    .InstancePerDependency();

                builder.RegisterType<SequenceItemRun>()
                    .PropertiesAutowired(AutofacInjectedAttribute.AutofacInjectedAttributeOnly)
                    .InstancePerDependency();
            }
        }
    }
}
