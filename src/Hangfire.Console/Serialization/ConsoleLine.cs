﻿using Newtonsoft.Json;

namespace Hangfire.Console.Serialization
{
    internal class ConsoleLine
    {
        /// <summary>
        /// Time offset since console timestamp in fractional seconds
        /// </summary>
        [JsonProperty("t", Required = Required.Always)]
        public double TimeOffset { get; set; }

        /// <summary>
        /// True if <see cref="Message"/> is a Hash reference.
        /// </summary>
        [JsonProperty("r", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsReference { get; set; }

        /// <summary>
        /// True if <see cref="Message"/> contains raw HTML.
        /// </summary>
        [JsonProperty("h", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsHtml { get; set; }

        /// <summary>
        /// Message text, or message reference, or progress bar id
        /// </summary>
        [JsonProperty("s", Required = Required.Always)]
        public string Message { get; set; }

        [JsonIgnore]
        private string _textColor;

        /// <summary>
        /// Text color for this message
        /// </summary>
        [JsonProperty("c", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TextColor
        {
            get { return _textColor; }
            set { _textColor = string.IsNullOrEmpty(value) ? null : value; }
        }

        /// <summary>
        /// Value update for a progress bar
        /// </summary>
        [JsonProperty("p", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public double? ProgressValue { get; set; }
    }
}
