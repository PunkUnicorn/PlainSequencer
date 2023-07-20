using Autofac;
using PactNet.Mocks.MockHttpService.Models;
using PactTests;
using PactTests_Shared;
using PlainSequencer;
using PlainSequencer.Logging;
using PlainSequencer.Options;
using PlainSequencer.Script;
using PlainSequencer.SequenceScriptLoader;
using PlainSequencer.Stuff;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace PactTests_ClosedTests
{
    /// <summary>
    /// Big text from:
    /// https://www.messletters.com/en/big-text/
    /// 
    /// Stylish text from:
    /// https://lingojam.com/StylishTextGenerator
    /// 
    /// Blocky text from:
    /// https://fsymbols.com/generators/carty/
    /// </summary>
    public class HttpSequencer_InvalidInput_DoesntCrash
    {
        private readonly PortAllocationFixture mrPorty = new PortAllocationFixture(2000);
        public Func<int> GetAvailablePort => mrPorty.GetAvailablePort;

        [Fact]
        public void NoYaml()
        {
            /* Arrange */
            var testOptions = new CommandLineOptions();

            /* Act */
            var provider = new Application(testOptions, null, null, null, new ConsoleOutputter(), new LogSequence());
            var result = provider.RunAsync(null).Result;

            /* Assert */
            Assert.False(result);
        }

        [Fact]
        public void InvalidUrl()
        {
            /* Arrange */
            var testYamlSequence = new SequenceScript
            {
                sequence_items = new List<SequenceItem> {
                    new SequenceItem
                    {
                        name = "invalid_url",
                        http = new Http
                        {
                            header = new NamedStringList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                            method = "GET",
                            url = "http://doesnt-even-exist-7djemd/totally-invalid-url"
                        }
                    }
                }
            };
            var testOptions = new CommandLineOptions { Direct = testYamlSequence };

            using (var container = AutofacTestSession.ConfigureTestSession(testOptions))
            using (var scope = container?.BeginLifetimeScope())
            {
                Assert.NotNull(scope); // "Test malfunction: can't create DI scope"
                var provider = scope.Resolve<IApplication>();

                /* Act */
                var result = provider.RunAsync(null).Result;

                /* Assert */
                Assert.False(result);
            }
        }

        [Fact]
        public void InvalidUrlForSecondSequenceItem()
        {
            int testPort = GetAvailablePort();

            using (var ConsumeTestYamlPact = new ConsumeHttpSequencerPact("FirstConsumer", testPort))
            {
                ConsumeTestYamlPact.MockProviderService.ClearInteractions();


                /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮 */
                const string expectedMoreDetailString = nameof(expectedMoreDetailString);

                ConsumeTestYamlPact.MockProviderService
                    .Given("There is an active endpoint that provides a list of ids")
                    .UponReceiving("A GET request to retrieve the list")
                    .With(new ProviderServiceRequest
                    {
                        Method = HttpVerb.Get,
                        Path = "/first",
                        Headers = new Dictionary<string, object> { { "Accept", "application/json" } },
                    })
                    .WillRespondWith(new ProviderServiceResponse
                    {
                        Status = 200,
                        Headers = new Dictionary<string, object> { { "Content-Type", "application/json; charset=utf-8" } },
                        Body = new Dictionary<string, object> { { "Id", "00000001" } }
                    });

                var testYamlSequence = new SequenceScript
                {
                    sequence_items = new List<SequenceItem> {
                        /* First */
                        new SequenceItem
                        {
                            name = "one-of-two-url-ok",
                            http = new Http
                            {
                                header = new NamedStringList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                                method = "GET",
                                url = $"http://localhost:{testPort}/first"
                            }
                        },
                        /* Second */
                        new SequenceItem
                        {
                            name = "two-of-two-url-doesnt-exist",
                            http = new Http
                            {
                                header = new NamedStringList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                                method = "GET",
                                url = "http://doesnt-even-exist-7djemd/totally-invalid-url/{{model.Id}}"
                            }
                        }
                    }
                };

                var testOptions = new CommandLineOptions { Direct = testYamlSequence };

                using (var container = AutofacTestSession.ConfigureTestSession(testOptions))
                using (var scope = container?.BeginLifetimeScope())
                {
                    Assert.NotNull(scope); // "Test malfunction: can't create DI scope"
                    var provider = scope.Resolve<IApplication>();

                    /* 𝓐𝓬𝓽 */
                    var result = provider.RunAsync(null).Result;

                    /* 𝓐𝓼𝓼𝓮𝓻𝓽 */
                    Assert.False(result);
                    ConsumeTestYamlPact.MockProviderService.VerifyInteractions();
                }
            }
        }

        [Fact]
        public void LoadYaml_ExpectFail()
        {
            const string invalidYamlContents = @"---
sequence_items:
  - name: should-be-invalid-yaml
    htt.... <!SYSTEM ERRORFZzzxsh he҉͇͈͎̞ ̙̫̖̻͖͞co̙͙̖̠̟̯̙m̫̦̹͔e͚̦͓̖̝s̘͖̣̼̫̠̙̀";

            using (var t = new TempFile())
            {
                /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮 */
                File.WriteAllText(t.Filename, invalidYamlContents);

                var testOptions = new CommandLineOptions { YamlFile = t.Filename };

                /* 𝓐𝓬𝓽 */
                var provider = new Application(testOptions, new LoadScript(), null, null, new ConsoleOutputter(), new LogSequence());
                var result = provider.RunAsync(null).Result;

                /* 𝓐𝓼𝓼𝓮𝓻𝓽 */
                Assert.False(result);
            }
        }
    }
}
