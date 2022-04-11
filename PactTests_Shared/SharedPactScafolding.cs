using PactNet.Mocks.MockHttpService.Models;
using System.Collections.Generic;

namespace PactTests
{
    public static class SharedPactScafolding
    {
        public static void BuildSuccessConsumerForId(ConsumeHttpSequencerPact consumer, string id)
        {
            consumer.MockProviderService
                .Given($"Given there is more detail for item id {id}")
                .UponReceiving($"A GET request for more detail for item id {id}")
                .With(new ProviderServiceRequest
                {
                    Method = HttpVerb.Get,
                    Path = $"/second/{id}",
                    Headers = new Dictionary<string, object> { { "Accept", "application/json" } },
                    Body = { }
                })
                .WillRespondWith(new ProviderServiceResponse
                {
                    Status = 200,
                    Headers = new Dictionary<string, object> { { "Content-Type", "application/json; charset=utf-8" } },
                    Body = new { detail = $"More detail for id {id}" }
                });
        }

        public static void BuildFailConsumerForId(ConsumeHttpSequencerPact consumer, string id)
        {
            consumer.MockProviderService
                .Given($"Given there is more detail for item id {id}")
                .UponReceiving($"A GET request for more detail for item id {id}")
                .With(new ProviderServiceRequest
                {
                    Method = HttpVerb.Get,
                    Path = $"/second/{id}",
                    Headers = new Dictionary<string, object> { { "Accept", "application/json" } },
                    Body = { }
                })
                .WillRespondWith(new ProviderServiceResponse
                {
                    Status = 410,
                    Headers = new Dictionary<string, object> { { "Content-Type", "text/html" } },
                    Body = "<html><body>"
                });
        }

    }
}
