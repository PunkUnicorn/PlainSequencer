using Autofac;
using PactNet.Mocks.MockHttpService.Models;
using PactTests;
using PactTests_Shared;
using PlainSequencer;
using PlainSequencer.Options;
using PlainSequencer.Script;
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
    public class HttpSequencer_TypicalOperation_DoesntCrash
    {
        private readonly PortAllocationFixture mrPorty = new PortAllocationFixture(1000);

        public Func<int> GetAvailablePort => mrPorty.GetAvailablePort;

        public ConsumeHttpSequencerPact ConsumeTestYamlPact { get; }
        public int Port { get; }

        public HttpSequencer_TypicalOperation_DoesntCrash()
        {
            Port = GetAvailablePort();
            var consumerName = $"{nameof(HttpSequencer_TypicalOperation_DoesntCrash)}Consumer";
            ConsumeTestYamlPact = new ConsumeHttpSequencerPact(consumerName, Port);
            ConsumeTestYamlPact.MockProviderService.ClearInteractions();
        }

        [Fact]
        public void LoadYaml_ExpectSuccess()
        {
            int testPort = GetAvailablePort();

            string yamlContents = $@"---
sequence_items:
  - name: load-yaml-expect-success
    breadcrumb: '{{{{run_id}}}} - {{{{sequence_item.command}}}} - {{{{sequence_item.send.url}}}}'
    http:
      method: GET
      url: http://localhost:{testPort}";

            using (var consumeTestYamlPact = new ConsumeHttpSequencerPact("FirstConsumer", testPort))
            using (var t = new TempFile())
            {
                consumeTestYamlPact.MockProviderService.ClearInteractions();

                /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮 */
                File.WriteAllText(t.Filename, yamlContents);

                consumeTestYamlPact.MockProviderService
                    .Given("There is an active endpoint")
                    .UponReceiving("A GET request to touch the endpoint")
                    .With(new ProviderServiceRequest
                    {
                        Method = HttpVerb.Get,
                        Path = "/",
                        Headers = new Dictionary<string, object> { { "Accept", "text/plain" } },
                    })
                    .WillRespondWith(new ProviderServiceResponse
                    {
                        Status = 200,
                        Headers = new Dictionary<string, object> { { "Content-Type", "application/json; charset=utf-8" } },
                        Body = { }
                    });

                var testOptions = new CommandLineOptions { YamlFile = t.Filename };

                using (var container = AutofacTestSession.ConfigureTestSession(testOptions))
                using (var scope = container?.BeginLifetimeScope())
                {
                    Assert.NotNull(scope); // "Test malfunction: can't create DI scope"
                    var consumer = scope.Resolve<IApplication>();

                    /* 𝓐𝓬𝓽 */
                    var result = consumer.RunAsync(null).Result;

                    /* 𝓐𝓼𝓼𝓮𝓻𝓽 */
                    Assert.True(result);
                    consumeTestYamlPact.MockProviderService.VerifyInteractions();
                }
            }
        }

        [Fact]
        public void OneSequence()
        {
            /*     _                                                 
                  / \     _ __   _ __    __ _   _ __     __ _    ___ 
                 / _ \   | '__| | '__|  / _` | | '_ \   / _` |  / _ \
                / ___ \  | |    | |    | (_| | | | | | | (_| | |  __/
               /_/   \_\ |_|    |_|     \__,_| |_| |_|  \__, |  \___|
                                                        |___/           */

            ConsumeTestYamlPact.MockProviderService
                .Given("There is an active endpoint")
                .UponReceiving("A GET request to the endpoint")
                .With(new ProviderServiceRequest
                {
                    Method = HttpVerb.Get,
                    Path = "/",
                    Headers = new Dictionary<string, object> { { "Accept", "text/plain" } },
                })
                .WillRespondWith(new ProviderServiceResponse
                {
                    Status = 200,
                    Headers = new Dictionary<string, object> { { "Content-Type", "text/plain" } },
                    Body = ""
                });

            /*
            * One sequence, the simplest GET with no params
            */
            var testYamlSequence = new SequenceScript
            {
                sequence_items = new List<SequenceItem> {
                    new SequenceItem
                    {
                        name = "first-and-only",
                        //breadcrumb = "{{run_id}} - {{sequence_item.command}} - {{sequence_item.send.url}}",
                        http = new Http
                        {
                            method = "GET",
                            url = $"http://localhost:{Port}"
                        }
                    }
                }
            };

            var testOptions = new CommandLineOptions { Direct = testYamlSequence };
            using (var container = AutofacTestSession.ConfigureTestSession(testOptions))
            using (var scope = container?.BeginLifetimeScope())
            {
                Assert.NotNull(scope); // "Test malfunction: can't create DI scope"
                var consumer = scope.Resolve<IApplication>();

                /*     _             _   
                      / \      ___  | |_ 
                     / _ \    / __| | __|
                    / ___ \  | (__  | |_ 
                   /_/   \_\  \___|  \__|   
                                            */

                var result = consumer.RunAsync(null).Result;



                /*     _                                _   
                      / \     ___   ___    ___   _ __  | |_ 
                     / _ \   / __| / __|  / _ \ | '__| | __|
                    / ___ \  \__ \ \__ \ |  __/ | |    | |_ 
                   /_/   \_\ |___/ |___/  \___| |_|     \__|    
                                                             */

                Assert.True(result);
                ConsumeTestYamlPact.MockProviderService.VerifyInteractions();

            }
        }

        [Fact]
        public void TwoSequences()
        {
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
                    Body = new List<object> { new { Id = "00000001" } }
                });

            ConsumeTestYamlPact.MockProviderService
                .Given("Given there is more detail for item id 00000001")
                .UponReceiving("A GET request for more detail for item id 00000001")
                .With(new ProviderServiceRequest
                {
                    Method = HttpVerb.Get,
                    Path = "/second/00000001",
                    Headers = new Dictionary<string, object> { { "Accept", "application/json" } },
                    Body = { }
                })
                .WillRespondWith(new ProviderServiceResponse
                {
                    Status = 200,
                    Headers = new Dictionary<string, object> { { "Content-Type", "application/json; charset=utf-8" } },
                    Body = new { detail = expectedMoreDetailString }
                });

            var testYamlSequence = new SequenceScript
            {
                sequence_items = new List<SequenceItem> {
                    /* First
                     * Get a list of ids, which will be a list of one id. 
                     * For each of these (that is, for the list of one) use that id in the next request */
                    new SequenceItem
                    {
                        name = "one-of-two",
                        //breadcrumb = "{{sequence_item.send.url}}",
                        http = new Http
                        {
                            header = new NamedStringList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                            method = "GET",
                            url = $"http://localhost:{Port}/first"
                        }
                    },
                    /* Second */
                    new SequenceItem
                    {
                        name = "two-of-two",
                        //breadcrumb = "{{sequence_item.send.url}} - {{model.Id}}",
                        is_model_array = true,
                        http = new Http
                        {
                            header = new NamedStringList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                            method = "GET",
                            url = $"http://localhost:{Port}/second/" + "{{model.Id}}"
                        }
                    }
                }
            };

            var testOptions = new CommandLineOptions { Direct = testYamlSequence };
            using (var container = AutofacTestSession.ConfigureTestSession(testOptions))
            using (var scope = container?.BeginLifetimeScope())
            {
                Assert.NotNull(scope); // "Test malfunction: can't create DI scope"
                var consumer = scope.Resolve<IApplication>();

                /* 𝓐𝓬𝓽 */

                var result = consumer.RunAsync(null).Result;


                /* 𝓐𝓼𝓼𝓮𝓻𝓽 */

                Assert.True(result);
                ConsumeTestYamlPact.MockProviderService.VerifyInteractions();
            }            
        }

        [Fact]
        public void ThreeSequences_ModelTransform()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮 */

            const string expectedMoreDetailForItem1 = nameof(expectedMoreDetailForItem1);
            const string expectedMoreDetailForItem2 = nameof(expectedMoreDetailForItem2);
            const string expectedMoreDetailForItem3 = nameof(expectedMoreDetailForItem3);

            ConsumeTestYamlPact.MockProviderService
                .Given("There is an active endpoint that provides a response of a single complex object")
                .UponReceiving("A GET request to retrieve the complex object")
                .With(new ProviderServiceRequest
                {
                    Method = HttpVerb.Get,
                    Path = "/get-complex-object",
                    Headers = new Dictionary<string, object> { { "Accept", "application/json" } },
                })
                .WillRespondWith(new ProviderServiceResponse
                {
                    Status = 200,
                    Headers = new Dictionary<string, object> { { "Content-Type", "application/json; charset=utf-8" } },
                    Body = new { 
                        Id = "00000001", 
                        NestedArray = new [] { "Item1", "Item2", "Item3" } 
                    }
                });

            void local_MakeMockProviderFor(string keyName, string expectedMoreDetailstring)
            { 
                ConsumeTestYamlPact.MockProviderService
                    .Given($"Given there is more detail for {keyName}")
                    .UponReceiving($"A GET request for more detail for {keyName}")
                    .With(new ProviderServiceRequest
                    {
                        Method = HttpVerb.Get,
                        Path = $"/detail/{keyName}",
                        Headers = new Dictionary<string, object> { { "Accept", "application/json" } },
                        Body = { }
                    })
                    .WillRespondWith(new ProviderServiceResponse
                    {
                        Status = 200,
                        Headers = new Dictionary<string, object> { { "Content-Type", "application/json; charset=utf-8" } },
                        Body = new { detail = expectedMoreDetailstring }
                    });
            }
            local_MakeMockProviderFor("Item1", expectedMoreDetailForItem1);
            local_MakeMockProviderFor("Item2", expectedMoreDetailForItem2);
            local_MakeMockProviderFor("Item3", expectedMoreDetailForItem3);

            var testYamlSequence = new SequenceScript
            {
                sequence_items = new List<SequenceItem> {
                    /* First
                     * Get a complex object with a nested array */
                    new SequenceItem
                    {
                        name = "one-of-three-get-complex-object",
                        //breadcrumb = "{{sequence_item.send.url}}",
                        http = new Http
                        {
                            header = new NamedStringList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                            method = "GET",
                            url = $"http://localhost:{Port}/get-complex-object"
                        },
                    },
                    /* Second, transform the model so property 'NestedItem' is now the whole model */
                    new SequenceItem
                    {
                        name = "two-of-three-transform-model",
                        //breadcrumb = "transform - {{model.Id}}\n{{model}}",
                        transform = new Transform
                        {
                            new_model_template = "[ {{- for item in model.NestedArray -}} \"{{item}}\" {{- if !for.last -}} , {{- end}}{{end -}} ]"
                        }
                    },
                    /* Third, fan out to a call per nested array item */
                    new SequenceItem
                    {
                        name = "three-of-three-get-detail",
                        //breadcrumb =  "{{sequence_item.send.url}} - {{model}}",
                        is_model_array = true,
                        http = new Http
                        {
                            header = new NamedStringList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                            method = "GET",
                            url = $"http://localhost:{Port}/detail/" + "{{model}}"
                        }
                    }
                }
            };

            var testOptions = new CommandLineOptions { Direct = testYamlSequence };
            using (var container = AutofacTestSession.ConfigureTestSession(testOptions))
            using (var scope = container?.BeginLifetimeScope())
            {
                Assert.NotNull(scope); // "Test malfunction: can't create DI scope"
                var consumer = scope.Resolve<IApplication>();

                /* 𝓐𝓬𝓽 */

                var result = consumer.RunAsync(null).Result;


                /* 𝓐𝓼𝓼𝓮𝓻𝓽 */

                Assert.True(result);
                ConsumeTestYamlPact.MockProviderService.VerifyInteractions();

            }            
        }

        [Fact]
        public void ThreeSequences_CheckPasses()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮 */

            const string expectedMoreDetailString = nameof(expectedMoreDetailString);

            ConsumeTestYamlPact.MockProviderService
                .Given("There is an active endpoint that provides a list of one id")
                .UponReceiving("A GET request to retrieve the list of one id")
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
                    Body = new List<object> { new { Id = "00000001" } }
                });

            ConsumeTestYamlPact.MockProviderService
                .Given("Given there is more detail for item id 00000001")
                .UponReceiving("A GET request for more detail for item id 00000001")
                .With(new ProviderServiceRequest
                {
                    Method = HttpVerb.Get,
                    Path = "/second/00000001",
                    Headers = new Dictionary<string, object> { { "Accept", "application/json" } },
                    Body = { }
                })
                .WillRespondWith(new ProviderServiceResponse
                {
                    Status = 200,
                    Headers = new Dictionary<string, object> { { "Content-Type", "application/json; charset=utf-8" } },
                    Body = new { detail = expectedMoreDetailString }
                });

            var testYamlSequence = new SequenceScript
            {
                sequence_items = new List<SequenceItem> {
                    /* First */
                    new SequenceItem
                    {
                        name = "one-of-three-check-passes",
                        //breadcrumb =  "{{sequence_item.send.url}}",
                        http = new Http
                        {
                            header = new NamedStringList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                            method = "GET",
                            url = $"http://localhost:{Port}/first"
                        }
                    },
                    /* Second */
                    new SequenceItem
                    {
                        name = "two-of-three-check-passes",
                        //breadcrumb =  "{{sequence_item.send.url}} - {{model.Id}}",
                        is_model_array = true,
                        http = new Http
                        {
                            header = new NamedStringList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                            method = "GET",
                            url = $"http://localhost:{Port}/second/" + "{{model.Id}}"
                        }
                    },
                    /* Check */
                    new SequenceItem
                    {
                        name = "three-of-three-check-passes",
                        //breadcrumb =  "check:\n{{sequence_item.check.pass_template}}\nwith:\n{{model.detail}}",
                        check = new Check
                        {
                            pass_template = "{{if model.detail=='expectedMoreDetailString'}}true{{else}}false{{end}}",
                            fail_info_template = "Model detail failed with value: {{model.detail}}"
                        }
                    }
                }
            };

            var testOptions = new CommandLineOptions { Direct = testYamlSequence };
            using (var container = AutofacTestSession.ConfigureTestSession(testOptions))
            using (var scope = container?.BeginLifetimeScope())
            {
                Assert.NotNull(scope); // "Test malfunction: can't create DI scope"
                var consumer = scope.Resolve<IApplication>();

                /* 𝓐𝓬𝓽 */
                var result = consumer.RunAsync(null).Result;


                /* 𝓐𝓼𝓼𝓮𝓻𝓽 */
                ConsumeTestYamlPact.MockProviderService.VerifyInteractions();

                Assert.True(result);
            }
        }

        [Fact]
        public void ThreeSequences_CheckFails()
        {
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
                    Body = new List<object> { new { Id = "00000001" } }
                });

            ConsumeTestYamlPact.MockProviderService
                .Given("Given there is more detail for item id 00000001")
                .UponReceiving("A GET request for more detail for item id 00000001")
                .With(new ProviderServiceRequest
                {
                    Method = HttpVerb.Get,
                    Path = "/second/00000001",
                    Headers = new Dictionary<string, object> { { "Accept", "application/json" } },
                    Body = { }
                })
                .WillRespondWith(new ProviderServiceResponse
                {
                    Status = 200,
                    Headers = new Dictionary<string, object> { { "Content-Type", "application/json; charset=utf-8" } },
                    Body = new { detail = expectedMoreDetailString }
                });

            var testYamlSequence = new SequenceScript
            {
                sequence_items = new List<SequenceItem> {
                    /* First */
                    new SequenceItem
                    {
                        name = "one-of-three-check-at-end-fails",
                        //breadcrumb =  "{{sequence_item.send.url}}",
                        http = new Http
                        {
                            header = new NamedStringList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                            method = "GET",
                            url = $"http://localhost:{Port}/first"
                        }
                    },
                    /* Second */
                    new SequenceItem
                    {
                        name = "two-of-three-check-at-end-fails",
                        //breadcrumb =  "{{sequence_item.send.url}}",
                        is_model_array = true,
                        http = new Http
                        {
                            header = new NamedStringList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                            method = "GET",
                            url = $"http://localhost:{Port}/second/" + "{{model.Id}}"
                        }
                    },
                    /* Check */
                    new SequenceItem
                    {
                        name = "three-of-three-check-at-end-fails",
                        //breadcrumb =  "check:\n{{sequence_item.check.pass_template}}\nwith:\n{{model.detail}}",
                        check = new Check
                        {
                            pass_template = "{{if model.detail=='it will never be this'}}true{{else}}false{{end}}",
                            fail_info_template = "Model detail failed with value: {{model.detail}}"
                        }
                    }
                }
            };

            var testOptions = new CommandLineOptions { Direct = testYamlSequence };
            using (var container = AutofacTestSession.ConfigureTestSession(testOptions))
            using (var scope = container?.BeginLifetimeScope())
            {
                Assert.NotNull(scope); // "Test malfunction: can't create DI scope"
                var consumer = scope.Resolve<IApplication>();


                /* 𝓐𝓬𝓽 */

                var result = consumer.RunAsync(null).Result;


                /* 𝓐𝓼𝓼𝓮𝓻𝓽 */

                Assert.False(result);
                ConsumeTestYamlPact.MockProviderService.VerifyInteractions();
            
            }           
        }
    }
}
