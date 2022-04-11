using PactNet;
using PactNet.Infrastructure.Outputters;
using PactNet.Mocks.MockHttpService;
using PactNet.Models;
using System;

namespace PactTests
{
    public class ConsumeHttpSequencerPact : IDisposable
    {
        public IPactBuilder PactBuilder { get; private set; }
        public IMockProviderService MockProviderService { get; private set; }

        private class ConsoleOutputter : IOutput
        {
            public void WriteLine(string line)
            {
                Console.WriteLine(line);
            }
        }

        public ConsumeHttpSequencerPact(string consumerName, int port)
        {
            PactBuilder = new PactBuilder( new PactConfig { Outputters = new[] { new ConsoleOutputter() }, SpecificationVersion = "2.0.0" }); //Configures the Specification Version

            PactBuilder
              .ServiceConsumer(consumerName)
              .HasPactWith("Something API");

            //needs elevated, or port opening
            MockProviderService = PactBuilder.MockService(port, false, IPAddress.Any);
        }

        public void Dispose()
        {
            PactBuilder.Build();
        }
    }
}
