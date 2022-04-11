using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using Xunit;

namespace PactTests
{
    public class PortAllocationFixture : IDisposable
    {
        public int StartingPort { get; set; }

        private int lastAllocatedPort;

        private List<int> AllocatedAlready { get; set; } = new List<int>();
        public ServiceProvider ServiceProvider { get; }

        public PortAllocationFixture(int startingPort)
        {
            lastAllocatedPort = startingPort-1;
            StartingPort = startingPort;
        }

        public int GetAvailablePort() 
        { 
            var usePort = ++lastAllocatedPort;
            do
            { 
                for ( ;AllocatedAlready.Contains(usePort); usePort++);
                usePort = StackOverflowUtils.GetAvailablePort(usePort);
            } while (AllocatedAlready.Contains(usePort));

            lastAllocatedPort = usePort;
            AllocatedAlready.Add(usePort);
            return usePort;
        }

        public void Dispose()
        {
            // ... clean up test data from the database ...
        }
    }


    //[CollectionDefinition("PortAllocationCollection", DisableParallelization = false)]
    //public class MyCollectionDefinition : ICollectionFixture<PortAllocationFixture>
    //{
    //    //Nothing needed here
    //}
}
