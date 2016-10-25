using Newtonsoft.Json;

namespace Hangfire.Console.Serialization
{
    internal class ConsoleLine
    {
        [JsonProperty("t", Required = Required.Always)]
        public double TimeOffset { get; set; }

        [JsonProperty("s", Required = Required.Always)]
        public string Message { get; set; }

        [JsonProperty("c", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TextColor { get; set; }
    }
}
