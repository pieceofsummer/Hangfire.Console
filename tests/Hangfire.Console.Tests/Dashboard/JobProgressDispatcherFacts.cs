using System.Collections.Generic;
using Hangfire.Console.Dashboard;
using Newtonsoft.Json;
using Xunit;

namespace Hangfire.Console.Tests.Dashboard
{
    public class JobProgressDispatcherFacts
    {
        [Fact]
        public void JsonSettings_PreservesDictionaryKeyCase()
        {
            var result = new Dictionary<string, double>
            {
                ["AAA"] = 1.0,
                ["Bbb"] = 2.0,
                ["ccc"] = 3.0
            };

            var json = JsonConvert.SerializeObject(result, JobProgressDispatcher.JsonSettings);

            Assert.Equal("{\"AAA\":1.0,\"Bbb\":2.0,\"ccc\":3.0}", json);
        }
    }
}