using Newtonsoft.Json;

namespace Hangfire.Console.Serialization
{
    internal class ConsoleLine
    {
        [JsonProperty("t", Required = Required.Always)]
        public double TimeOffset { get; set; }

        [JsonProperty("s", Required = Required.Always)]
        public string Message { get; set; }

        [JsonIgnore]
        private string _textColor;

        [JsonProperty("c", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TextColor
        {
            get { return _textColor; }
            set { _textColor = string.IsNullOrEmpty(value) ? null : value; }
        }

        [JsonProperty("p", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? ProgressValue { get; set; }
    }
}
