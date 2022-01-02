using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Keda.Samples.DotNet.EventHub.Contracts;
using Keda.Samples.DotNet.EventHub.OrderProcessor;
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
        private static int counter = 0;

        protected EventsWorker(IConfiguration configuration, ILogger<EventsWorker<TMessage>> logger)
        {
            Configuration = configuration;
            Logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {            
            var eventHubProcessor = GetEventHubProcessor();
            eventHubProcessor.ProcessEventAsync += HandleEventAsync;
            eventHubProcessor.ProcessErrorAsync += HandleReceivedExceptionAsync;

            Logger.LogInformation($"Starting {eventHubProcessor.ConsumerGroup} consumer group in eventhub {eventHubProcessor.FullyQualifiedNamespace}");
            await eventHubProcessor.StartProcessingAsync(stoppingToken);
            Logger.LogInformation("Events listener started");

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
            var messageId = Guid.NewGuid().ToString(); //generating GUID as message ID is null
            counter++; //just for record
            try
            {
                var rawMessageBody = Encoding.UTF8.GetString(processEventArgs.Data.Body.ToArray());
                
                processEventArgs.Data.MessageId = messageId;

                DateTime before = DateTime.Now;
                Logger.LogInformation("Received event num. {Counter} {MessageId} -------------- at {TimeStarted}", counter, processEventArgs.Data.MessageId, GetTimeWithMileseconds(before));

                Type itemType = typeof(TMessage);               
                if (itemType == typeof(Order))
                {
                    var order = JsonConvert.DeserializeObject<TMessage>(rawMessageBody);

                    if (order != null)
                    {
                        await ProcessEvent(order, messageId,
                            processEventArgs.Data.Properties,
                            processEventArgs.CancellationToken);
                    }
                    else
                    {
                        Logger.LogError(
                            "Unable to deserialize to contract {ContractName} for event {MessageBody}",
                            typeof(TMessage), rawMessageBody);
                    }
                }
                else
                {
                    if (itemType == typeof(string))
                    {
                        var message = (TMessage)Convert.ChangeType(rawMessageBody, typeof(TMessage));
                        if (message != null)
                        {
                            await ProcessEvent(message, messageId,
                                processEventArgs.Data.Properties,
                                processEventArgs.CancellationToken);
                        }
                        else
                        {
                            Logger.LogError(
                                "Unable to deserialize to contract {ContractName} for event {MessageBody}",
                                typeof(TMessage), rawMessageBody);
                        }
                    }
                    else
                    {
                        Logger.LogError(
                            "Not supported type contract {ContractName} for event {MessageBody}",
                            typeof(TMessage), rawMessageBody);
                    }
                }                
                DateTime after = DateTime.Now;
                Logger.LogInformation("Completed event num. {Counter} - id {MessageId} - took {TimeProcessed} ms at {TimeCompleted}", counter, processEventArgs.Data.MessageId, after.Subtract(before).Milliseconds, GetTimeWithMileseconds(after));

                await processEventArgs.UpdateCheckpointAsync(processEventArgs.CancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Unable to handle message {messageId}");
            }
        }
        
        private Task HandleReceivedExceptionAsync(ProcessErrorEventArgs exceptionEvent)
        {
            Logger.LogError(exceptionEvent.Exception, "Unable to process message");
            return Task.CompletedTask;
        }

        private string GetTimeWithMileseconds(DateTime input)
        {
            StringBuilder retVal = new StringBuilder();
            retVal.Append(input.ToLongTimeString());
            retVal.Append(":");
            retVal.Append(input.Millisecond);
            return retVal.ToString();
        }

        protected abstract Task ProcessEvent(TMessage order, string messageId, IEnumerable<KeyValuePair<string, object>> userProperties, CancellationToken cancellationToken);
    }
}
