using Autofac;
using FluentAssertions;
using Newtonsoft.Json;
using PactNet.Mocks.MockHttpService.Models;
using PactTests;
using PactTests_Shared;
using PlainSequencer;
using PlainSequencer.Logging;
using PlainSequencer.Options;
using PlainSequencer.Script;
using PlainSequencer.Stuff;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

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
        private readonly ITestOutputHelper mrOutput;

        public Func<int> GetAvailablePort => mrPorty.GetAvailablePort;

        public ConsumeHttpSequencerPact ConsumeTestYamlPact { get; }
        public int Port { get; }

        public HttpSequencer_TypicalOperation_DoesntCrash(ITestOutputHelper output)
        {
            Port = GetAvailablePort();
            var consumerName = $"{nameof(HttpSequencer_TypicalOperation_DoesntCrash)}Consumer";
            ConsumeTestYamlPact = new ConsumeHttpSequencerPact(consumerName, Port);
            ConsumeTestYamlPact.MockProviderService.ClearInteractions();
            mrOutput = output;
        }

        [Fact]
        public void LoadYaml_ExpectSuccess()
        {
            int testPort = GetAvailablePort();

            string yamlContents = $@"---
sequence_items:
  - name: load-yaml-expect-success
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

                using var container = AutofacTestSession.ConfigureTestSession(testOptions);
                using var scope = container?.BeginLifetimeScope();

                scope.Should().NotBeNull("Test malfunction: can't create DI scope");

                var consumer = scope.Resolve<IApplication>();

                /* 𝓐𝓬𝓽 */
                var result = consumer.RunAsync(null).Result;

                /* 𝓐𝓼𝓼𝓮𝓻𝓽 */
                result.Should().BeTrue();
                consumeTestYamlPact.MockProviderService.VerifyInteractions();

                var sequenceNotation = container.Resolve<ILogSequence>().GetSequenceDiagramNotation(MethodBase.GetCurrentMethod().Name);
                mrOutput.WriteLine(sequenceNotation);
            }
        }

        [Fact]
        public void OneSequence()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮 */

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
                        http = new Http
                        {
                            method = "GET",
                            url = $"http://localhost:{Port}"
                        }
                    }
                }
            };

            var testOptions = new CommandLineOptions { Direct = testYamlSequence };
            using var container = AutofacTestSession.ConfigureTestSession(testOptions);
            using var scope = container?.BeginLifetimeScope();

            scope.Should().NotBeNull("Test malfunction: can't create DI scope");
            var consumer = scope.Resolve<IApplication>();

            /* 𝓐𝓬𝓽 */

            var result = consumer.RunAsync(null).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */

            result.Should().BeTrue();
            ConsumeTestYamlPact.MockProviderService.VerifyInteractions();

            var sequenceNotation = container.Resolve<ILogSequence>().GetSequenceDiagramNotation(MethodBase.GetCurrentMethod().Name);
            mrOutput.WriteLine(sequenceNotation);
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
            using var container = AutofacTestSession.ConfigureTestSession(testOptions);
            using var scope = container?.BeginLifetimeScope();

            scope.Should().NotBeNull("Test malfunction: can't create DI scope");
            var consumer = scope.Resolve<IApplication>();

            /* 𝓐𝓬𝓽 */

            var result = consumer.RunAsync(null).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */

            var sequenceNotation = container.Resolve<ILogSequence>().GetSequenceDiagramNotation(MethodBase.GetCurrentMethod().Name);
            mrOutput.WriteLine(sequenceNotation);

            result.Should().BeTrue();
            ConsumeTestYamlPact.MockProviderService.VerifyInteractions();
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
                        transform = new Transform
                        {
                            //new_model_template = "[ {{- for item in model.NestedArray -}} \"{{item}}\" {{- if !for.last -}} , {{- end}}{{end -}} ]"
                            new_model_template = @"{{
                                a=[]
                                for item in model.NestedArray
                                    a = a | array.add ''+item
                                end -}}

                            {{a}}"
                        }
                    },
                    /* Third, fan out to a call per nested array item */
                    new SequenceItem
                    {
                        name = "three-of-three-get-detail",
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
            using var container = AutofacTestSession.ConfigureTestSession(testOptions);
            using var scope = container?.BeginLifetimeScope();

            scope.Should().NotBeNull("Test malfunction: can't create DI scope");
            var consumer = scope.Resolve<IApplication>();

            /* 𝓐𝓬𝓽 */

            var result = consumer.RunAsync(null).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */

            var sequenceNotation = container.Resolve<ILogSequence>().GetSequenceDiagramNotation(MethodBase.GetCurrentMethod().Name);
            mrOutput.WriteLine(sequenceNotation);

            result.Should().BeTrue();
            ConsumeTestYamlPact.MockProviderService.VerifyInteractions();
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
                        check = new Check
                        {
                            pass_template = "{{if model.detail=='expectedMoreDetailString'}}true{{else}}false{{end}}",
                            fail_info_template = "Failed with value: '{{model.detail}}'"
                        }
                    }
                }
            };

            var testOptions = new CommandLineOptions { Direct = testYamlSequence };
            using var container = AutofacTestSession.ConfigureTestSession(testOptions);
            using var scope = container?.BeginLifetimeScope();

            scope.Should().NotBeNull("Test malfunction: can't create DI scope");
            var consumer = scope.Resolve<IApplication>();

            /* 𝓐𝓬𝓽 */
            var result = consumer.RunAsync(null).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */
            ConsumeTestYamlPact.MockProviderService.VerifyInteractions();

            result.Should().BeTrue();

            var sequenceNotation = container.Resolve<ILogSequence>().GetSequenceDiagramNotation(MethodBase.GetCurrentMethod().Name);
            mrOutput.WriteLine(sequenceNotation);
        }

        [Theory]
        [InlineData(false, "")]
        [InlineData(true, "{\"detail\":\"expectedMoreDetailString\"}")]
        public void ThreeSequences_CheckFails(bool outputDespiteErrors, string expectedOutput)
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮 */

            const string expectedMoreDetailString = nameof(expectedMoreDetailString);

            ConsumeTestYamlPact.MockProviderService
                .Given("There is an active endpoint that provides a complex object that contains an id")
                .UponReceiving("A GET request to retrieve the object")
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
                    Body =  new { Id = "00000001" }
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
                output_after_failure = outputDespiteErrors,
                sequence_items = new List<SequenceItem> {
                    /* First */
                    new SequenceItem
                    {
                        name = "one-of-three-check-at-end-fails",
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
                        check = new Check
                        {
                            pass_template = "{{if model.detail=='it will never be this'}}true{{else}}false{{end}}",
                            fail_info_template = "Expected fail, with value: '{{model.detail}}'"
                        }
                    }
                }
            };

            var testOptions = new CommandLineOptions { Direct = testYamlSequence };
            using var container = AutofacTestSession.ConfigureTestSession(testOptions);
            using var scope = container?.BeginLifetimeScope();

            scope.Should().NotBeNull("Test malfunction: can't create DI scope");
            var consumer = scope.Resolve<IApplication>();


            /* 𝓐𝓬𝓽 */

            var result = consumer.RunAsync(null).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */

            result.Should().BeFalse();
            ConsumeTestYamlPact.MockProviderService.VerifyInteractions();

            var output = container.Resolve<ConsoleOutputterTest>();
            var actual = output.Output.Length == 0
                ? output.Output
                : JsonConvert.SerializeObject(JsonConvert.DeserializeObject(output.Output), Formatting.None);

            actual.Should().Be(expectedOutput);


            /* 𝓢𝓮𝓺𝓾𝓮𝓷𝓬𝓮 𝓓𝓲𝓪𝓰𝓻𝓪𝓶 */

            var title = $"{MethodBase.GetCurrentMethod().Name} {nameof(outputDespiteErrors)}={outputDespiteErrors}";
            var sequenceNotation = container.Resolve<ILogSequence>().GetSequenceDiagramNotation(title);
            mrOutput.WriteLine(sequenceNotation);

            sequenceNotation.Should().StartWith($"title {title}");
            const string expectedSequenceNotation = "two of three check at end fails-xthree of three check at end fails:{\\n  \"detail\": \"expectedMoreDetailString\"\\n}";
            sequenceNotation.Should().Contain(expectedSequenceNotation);

            var expectedLastResultLine = "three of three check at end fails-xResult:{\\n  \"detail\": \"expectedMoreDetailString\"\\n}";
            if (outputDespiteErrors)
                sequenceNotation.Should().EndWith(expectedLastResultLine);
            else
                sequenceNotation.Should().NotEndWith(expectedLastResultLine);

            // stderr
            mrOutput.WriteLine($"\nStderr:\n{output.Error}");

            output.Error.Should().StartWith("expandable+");
            output.Error.Should().Contain(expectedSequenceNotation);
            output.Error.Should().EndWith($"end{Environment.NewLine}");
        }

        [Fact]
        public void FourSequences_Get_Load_Transform_Check()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮 */

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
                    Body = new[] { new { Id = "00000001" }, new { Id = "00000002" }, new { Id = "00000003" } }
                });

            using var t = new TempFile();
            File.WriteAllText(t.Filename, "colA,colB,colC\nrow1a,row1b,row1c\nrow2a,row2b,row2c");
            var testYamlSequence = new SequenceScript
            {
                sequence_items = new List<SequenceItem> {
                    /* First - http get */
                    new SequenceItem
                    {
                        name = "one-of-four-http-get",
                        http = new Http
                        {
                            header = new NamedStringList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                            method = "GET",
                            url = $"http://localhost:{Port}/first"
                        }
                    },
                    /* Second - csv */
                    new SequenceItem
                    {
                        name = "two-of-four-load-csv",
                        is_model_array = false,
                        load = new Load { csv = t.Filename }
                    },
                    /* Third - transform */
                    new SequenceItem
                    {
                        name = "three-of-four-transformers",
                        transform = new Transform
                        {
                            new_model_template = @"{{
                                newArray = []
                                for modelItem in model                                    
                                    for csvRow in csv
                                        newStr = [ modelItem.Id, csvRow.colB ] | array.join '_'
                                        newArray = newArray | array.add newStr
                                    end
                                end -}}

                                {{newArray}}",
                        }
                    },
                    /* Fourth - check */
                    new SequenceItem
                    {
                        name = "four-of-four-check",
                        check = new Check
                        {
                            pass_template = @"{{
                                expectedArray = ['00000001_row1b',
                                    '00000001_row2b',
                                    '00000002_row1b',
                                    '00000002_row2b',
                                    '00000003_row1b',
                                    '00000003_row2b']

                                if model.size != expectedArray.size -}}
                                    false {{-
                                    ret
                                end

                                for actualItem in model
                                    if actualItem != array.cycle expectedArray -}}
                                        false {{- 
                                        ret
                                    end
                                end-}}

                                true",

                            fail_info_template = 
                                @"Expected model:\n{{model}}\nto match:\n{{['00000001_row1b',
                                    '00000001_row2b',
                                    '00000002_row1b',
                                    '00000002_row2b',
                                    '00000003_row1b',
                                    '00000003_row2b']}}

                                model size: {{ model.size -}}"
                        }
                    }
                }
            };

            var testOptions = new CommandLineOptions { Direct = testYamlSequence };
            using var container = AutofacTestSession.ConfigureTestSession(testOptions);
            using var scope = container?.BeginLifetimeScope();

            scope.Should().NotBeNull("Test malfunction: can't create DI scope");
            var consumer = scope.Resolve<IApplication>();


            /* 𝓐𝓬𝓽 */

            var result = consumer.RunAsync(null).Result;


            /* 𝓢𝓮𝓺𝓾𝓮𝓷𝓬𝓮 𝓓𝓲𝓪𝓰𝓻𝓪𝓶 */

            var output = container.Resolve<ConsoleOutputterTest>();
            var title = $"{MethodBase.GetCurrentMethod().Name}";
            var sequenceNotation = container.Resolve<ILogSequence>().GetSequenceDiagramNotation(title);//, PlainSequencer.SequenceItemActions.SequenceProgressLogLevel.Brief);
            mrOutput.WriteLine(sequenceNotation);

            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */

            result.Should().BeTrue();

            output.Error.Should().BeEmpty();

            ConsumeTestYamlPact.MockProviderService.VerifyInteractions();

            JsonConvert.DeserializeObject<string[]>(output.Output)
                .Should()
                .ContainInOrder(new string[] { "00000001_row1b", "00000001_row2b", "00000002_row1b", "00000002_row2b", "00000003_row1b", "00000003_row2b" });
        }
    }
}
