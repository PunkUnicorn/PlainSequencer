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
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace PactTests_ClosedTests
{
    public class HttpSequencer_TypicalOperation_FansOut
    {
        private readonly PortAllocationFixture mrPorty = new PortAllocationFixture(3000);
        private readonly ITestOutputHelper mrOutput;

        public Func<int> GetAvailablePort => mrPorty.GetAvailablePort;

        private ConsumeHttpSequencerPact ConsumeTestYamlPact { get; }
        private int Port { get; }

        public HttpSequencer_TypicalOperation_FansOut(ITestOutputHelper output)
        {
            Port = GetAvailablePort();
            var consumerName = $"{nameof(HttpSequencer_TypicalOperation_FansOut)}Consumer";
            ConsumeTestYamlPact = new ConsumeHttpSequencerPact(consumerName, Port);
            ConsumeTestYamlPact.MockProviderService.ClearInteractions();
            mrOutput = output;
        }

        private SequenceScript MakeYamlSequence(int port, string commandPostfix, bool outputDespiteErrors=false)
        {
            return new SequenceScript
            {
                output_after_failure = outputDespiteErrors,
                sequence_items = new List<SequenceItem>
                {
                    /* First */
                    new SequenceItem
                    {
                        name = $"one-of-two-{commandPostfix}",
                        http = new Http
                        {
                            header = new NamedStringList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                            method = "GET",
                            url = $"http://localhost:{port}/first"
                        }
                    },
                    /* Second */
                    new SequenceItem
                    {
                        name = $"two-of-two-{commandPostfix}",
                        is_model_array = true,
                        http = new Http
                        {
                            header = new NamedStringList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                            method = "GET",
                            url = $"http://localhost:{port}/second/" + "{{model.Id}}"
                        }
                    },
                }
            };
        }

        [Fact]
        public void FansOutThree_ExpectedSuccess()
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
                    Body = new List<object> {
                        new { Id = "00000001" },
                        new { Id = "00000002" },
                        new { Id = "00000003" },
                    }
                });

            SharedPactScafolding.BuildSuccessConsumerForId(ConsumeTestYamlPact, "00000001");
            SharedPactScafolding.BuildSuccessConsumerForId(ConsumeTestYamlPact, "00000002");
            SharedPactScafolding.BuildSuccessConsumerForId(ConsumeTestYamlPact, "00000003");          

            var testOptions = new CommandLineOptions { Direct = MakeYamlSequence(Port, "expect-success") };
            using (var container = AutofacTestSession.ConfigureTestSession(testOptions))
            using (var scope = container?.BeginLifetimeScope())
            {
                Assert.NotNull(scope); // "Test malfunction: can't create DI scope"
                var provider = scope.Resolve<IApplication>();


                /* 𝓐𝓬𝓽 */

                var result = provider.RunAsync(null).Result;

                /* 𝓐𝓼𝓼𝓮𝓻𝓽 */

                Assert.True(result);
                ConsumeTestYamlPact.MockProviderService.VerifyInteractions();

                var output = container.Resolve<ConsoleOutputterTest>();
                var actual = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(output.Output), Formatting.None);
                var expected = "[{'detail':'More detail for id 00000001'},{'detail':'More detail for id 00000002'},{'detail':'More detail for id 00000003'}]".Replace('\'', '"');

                actual.Should().Be(expected);

                /* 𝓢𝓮𝓺𝓾𝓮𝓷𝓬𝓮 𝓓𝓲𝓪𝓰𝓻𝓪𝓶 */

                var sequenceNotation = container.Resolve<ILogSequence>().GetSequenceDiagramNotation(MethodBase.GetCurrentMethod().Name);
                mrOutput.WriteLine(sequenceNotation);

                Regex MakeRegex(int i)
                    => new Regex(@$"^.*one of two expect success->two of two expect success:.*0000000{i}.*$", RegexOptions.Multiline);

                sequenceNotation.Should().StartWith($"title {MethodBase.GetCurrentMethod().Name}");
                sequenceNotation.Should().MatchRegex(MakeRegex(1));
                sequenceNotation.Should().MatchRegex(MakeRegex(2));
                sequenceNotation.Should().MatchRegex(MakeRegex(3));

                output.Error.Should().BeEmpty();
            }
        }

        [Theory]
        [InlineData(false, "")]
        [InlineData(true, "[{\"detail\":\"More detail for id 00000001\"},{\"detail\":\"More detail for id 00000003\"}]")]
        public void FansOutThree_ExpectedFailForOne(bool outputDespiteErrors, string expectedOutput)
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮 */

                ConsumeTestYamlPact.MockProviderService
                .Given("There is an active endpoint that provides a list of three ids")
                .UponReceiving("A GET request to retrieve the list of three ids")
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
                    Body = new List<object> {
                        new { Id = "00000001" }, // <--- Expected success
                        new { Id = "00000002" }, // <--- Expected fail
                        new { Id = "00000003" }, // <--- Expected success
                    }
                });

            SharedPactScafolding.BuildSuccessConsumerForId(ConsumeTestYamlPact, "00000001");
            SharedPactScafolding.BuildFailConsumerForId(ConsumeTestYamlPact, "00000002");
            SharedPactScafolding.BuildSuccessConsumerForId(ConsumeTestYamlPact, "00000003");

            var testOptions = new CommandLineOptions { Direct = MakeYamlSequence(Port, "expect-fail-on-the-second-only", outputDespiteErrors) };

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
                Regex MakeRegex(int i, char a) 
                    => new Regex(@$"^.*one of two expect fail on the second only-{a}two of two expect fail on the second only:.*0000000{i}.*$", RegexOptions.Multiline);

                sequenceNotation.Should().MatchRegex(MakeRegex(1, '>'));
                sequenceNotation.Should().MatchRegex(MakeRegex(2, 'x'));
                sequenceNotation.Should().MatchRegex(MakeRegex(3, '>'));

                var expectedLastLineRegex = new Regex("^.*two of two expect fail on the second only-xResult:.*More detail for id 00000001.*More detail for id 00000003.*$", RegexOptions.Multiline);
                if (outputDespiteErrors)
                    sequenceNotation.Should().MatchRegex(expectedLastLineRegex);
                else
                    sequenceNotation.Should().NotMatchRegex(expectedLastLineRegex);

                if (output.Error.Length > 0)
                    mrOutput.WriteLine($"\nStderr:\n{output.Error}");
            }
        }

        [Theory]
        [InlineData(false, "")]
        [InlineData(true, "[{\"detail\":\"More detail for id 00000003\"}]")]
        public void FansOutThree_ExpectedFailForTwo(bool outputDespiteErrors, string expectedOutput)
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮 */

            ConsumeTestYamlPact.MockProviderService
                .Given("There is an active endpoint that provides a list of three ids")
                .UponReceiving("A GET request to retrieve the list of three ids")
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
                    Body = new List<object> {
                        new { Id = "00000001" }, // <--- Expected fail
                        new { Id = "00000002" }, // <--- Expected fail
                        new { Id = "00000003" }, // <--- Expected success
                    }
                });

            SharedPactScafolding.BuildFailConsumerForId(ConsumeTestYamlPact, "00000001");
            SharedPactScafolding.BuildFailConsumerForId(ConsumeTestYamlPact, "00000002");
            SharedPactScafolding.BuildSuccessConsumerForId(ConsumeTestYamlPact, "00000003");            

            var testOptions = new CommandLineOptions { Direct = MakeYamlSequence(Port, "expect-fail-on-the-first-two", outputDespiteErrors) };

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
                Regex MakeRegex(int i, char a)
                    => new Regex(@$"^.*one of two expect fail on the first two-{a}two of two expect fail on the first two:.*0000000{i}.*$", RegexOptions.Multiline);

                sequenceNotation.Should().MatchRegex(MakeRegex(1, 'x'));
                sequenceNotation.Should().MatchRegex(MakeRegex(2, 'x'));
                sequenceNotation.Should().MatchRegex(MakeRegex(3, '>'));

                var expectedLastLineRegex = new Regex("^.*two of two expect fail on the first two-xResult:.*More detail for id 00000003.*$", RegexOptions.Multiline);
                if (outputDespiteErrors)
                    sequenceNotation.Should().MatchRegex(expectedLastLineRegex);
                else
                    sequenceNotation.Should().NotMatchRegex(expectedLastLineRegex);

                // stderr
                mrOutput.WriteLine($"\nStderr:\n{output.Error}");

                output.Error.Should().StartWith("expandable+");
                output.Error.Should().MatchRegex(MakeRegex(1, 'x'));
                output.Error.Should().MatchRegex(MakeRegex(2, 'x'));
                output.Error.Should().NotMatchRegex(MakeRegex(3, '>'));
                output.Error.Should().EndWith($"end{Environment.NewLine}");
            }
        }
    }
}
