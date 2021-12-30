using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Keda.Samples.Dotnet.EventHub.OrderProcessor
{
    public abstract class EventsWorker<TMessage> : BackgroundService
    {
        protected ILogger<EventsWorker<TMessage>> Logger { get; }
        protected IConfiguration Configuration { get; }

        protected EventsWorker(IConfiguration configuration, ILogger<EventsWorker<TMessage>> logger)
        {
            Configuration = configuration;
            Logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queueName = Configuration.GetValue<string>("KEDA_SERVICEBUS_QUEUE_NAME");
            var eventHubProcessor = GetEventHubProcessor();
            eventHubProcessor.ProcessEventAsync += HandleEventAsync;
            eventHubProcessor.ProcessErrorAsync += HandleReceivedExceptionAsync;

            Logger.LogInformation($"Starting {eventHubProcessor.ConsumerGroup} consumer group in eventhub {eventHubProcessor.FullyQualifiedNamespace}");
            await eventHubProcessor.StartProcessingAsync(stoppingToken);
            Logger.LogInformation("Message pump started");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            Logger.LogInformation("Closing message pump");
            await eventHubProcessor.StopProcessingAsync(cancellationToken: stoppingToken);
            Logger.LogInformation("Message pump closed : {Time}", DateTimeOffset.UtcNow);
        }        

        private EventProcessorClient GetEventHubProcessor()
        {
            var authenticationMode = Configuration.GetValue<AuthenticationMode>("GLOBAL_AUTH_MODE");

            EventProcessorClient eventHubClient;

            switch (authenticationMode)
            {
                case AuthenticationMode.ConnectionString:
                    Logger.LogInformation($"Authentication by using connection string");
                    eventHubClient = EventHubClientFactory.CreateWithConnectionStringAuthentication(Configuration);
                    break;
                case AuthenticationMode.ServicePrinciple:
                    Logger.LogInformation("Authentication by using service principle");
                    eventHubClient = EventHubClientFactory.CreateWithServicePrincipleAuthentication(Configuration);
                    break;
                case AuthenticationMode.ManagedIdentity:
                    Logger.LogInformation("Authentication by using managed identity");
                    eventHubClient = EventHubClientFactory.CreateWithManagedIdentityAuthentication(Configuration, Logger);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return eventHubClient;
        }

        private async Task HandleEventAsync(ProcessEventArgs processEventArgs)
        {
            try
            {
                var rawMessageBody = Encoding.UTF8.GetString(processEventArgs.Data.Body.ToArray());
                Logger.LogInformation("Received event {MessageId} with body {MessageBody}",
                    processEventArgs.Data.MessageId, rawMessageBody);

                var order = JsonConvert.DeserializeObject<TMessage>(rawMessageBody);
                if (order != null)
                {
                    await ProcessEvent(order, processEventArgs.Data.MessageId,
                        processEventArgs.Data.Properties,
                        processEventArgs.CancellationToken);
                }
                else
                {
                    Logger.LogError(
                        "Unable to deserialize to contract {ContractName} for event {MessageBody}",
                        typeof(TMessage), rawMessageBody);
                }

                Logger.LogInformation("Event {MessageId} processed", processEventArgs.Data.MessageId);

                await processEventArgs.UpdateCheckpointAsync(processEventArgs.CancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unable to handle message");
            }
        }

        private Task HandleReceivedExceptionAsync(ProcessErrorEventArgs exceptionEvent)
        {
            Logger.LogError(exceptionEvent.Exception, "Unable to process message");
            return Task.CompletedTask;
        }

        protected abstract Task ProcessEvent(TMessage order, string messageId, IEnumerable<KeyValuePair<string, object>> userProperties, CancellationToken cancellationToken);
    }
}
