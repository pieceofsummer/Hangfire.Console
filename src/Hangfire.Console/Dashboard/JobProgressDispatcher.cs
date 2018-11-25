using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Hangfire.Console.Serialization;
using Hangfire.Console.Storage;
using Hangfire.Dashboard;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Hangfire.Console.Dashboard
{
    /// <summary>
    /// Provides progress for jobs.
    /// </summary>
    internal class JobProgressDispatcher : IDashboardDispatcher
    {
        internal static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings()
        {
            ContractResolver = new DefaultContractResolver()
        };
        
        // ReSharper disable once NotAccessedField.Local
        private readonly ConsoleOptions _options;

        public JobProgressDispatcher(ConsoleOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }
        
        public async Task Dispatch(DashboardContext context)
        {
            if (!"POST".Equals(context.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                return;
            }
            
            var result = new Dictionary<string, double>();

            var jobIds = await context.Request.GetFormValuesAsync("jobs[]");
            if (jobIds.Count > 0)
            {
                // there are some jobs to process
                
                using (var storage = new ConsoleStorage(context.Storage.GetConnection()))
                {
                    foreach (var jobId in jobIds)
                    {
                        var state = storage.GetState(jobId);
                        if (ConsoleId.TryCreate(jobId, state, out var consoleId))
                        {
                            var progress = storage.GetProgress(consoleId);
                            if (progress.HasValue)
                                result[jobId] = progress.Value;
                        }
                        else
                        {
                            // return -1 to indicate the job is not in Processing state
                            result[jobId] = -1;
                        }
                    }
                }
            }

            var serialized = JsonConvert.SerializeObject(result, JsonSettings);

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(serialized);
        }
    }
}