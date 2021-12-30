using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Keda.Samples.Dotnet.EventHub.OrderProcessor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Keda.Samples.DotNet.EventHub.OrderProcessor
{
    public class StringEventProcessor : EventsWorker<string>
    {
        public StringEventProcessor(IConfiguration configuration, ILogger<StringEventProcessor> logger)
            : base(configuration, logger)
        {
        }

        protected override async Task ProcessEvent(string eventBody, string sequenceId, IEnumerable<KeyValuePair<string, object>> userProperties, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Processing event: {eventBody} ", eventBody);

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

            Logger.LogInformation("Message {messageId} processed", sequenceId);
        }
    }
}
