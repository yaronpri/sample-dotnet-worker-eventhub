using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Keda.Samples.Dotnet.EventHub.OrderProcessor;
using Keda.Samples.DotNet.EventHub.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Keda.Samples.DotNet.EventHub.OrderProcessor
{
    public class OrdersEventProcessor : EventsWorker<Order>
    {
        public OrdersEventProcessor(IConfiguration configuration, ILogger<OrdersEventProcessor> logger)
            : base(configuration, logger)
        {
        }

        protected override async Task ProcessEvent(Order order, string messageId, IEnumerable<KeyValuePair<string, object>> userProperties, CancellationToken cancellationToken)
        {
            //Logger.LogInformation("Message Id {MessageId} - Processing order {OrderId} for {OrderAmount} units of {OrderArticle} bought by {CustomerFirstName} {CustomerLastName}", messageId, order.Id, order.Amount, order.ArticleNumber, order.Customer.FirstName, order.Customer.LastName);

            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);

            //Logger.LogInformation("Message Id {MessageId} - Processed Order {OrderId} ", messageId, order.Id);
        }
    }    
}
