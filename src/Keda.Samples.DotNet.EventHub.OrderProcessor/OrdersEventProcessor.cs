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
    public class OrdersQueueProcessor : EventsWorker<Order>
    {
        public OrdersQueueProcessor(IConfiguration configuration, ILogger<OrdersQueueProcessor> logger)
            : base(configuration, logger)
        {
        }

        protected override async Task ProcessEvent(Order order, string messageId, IEnumerable<KeyValuePair<string, object>> userProperties, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Processing order {OrderId} for {OrderAmount} units of {OrderArticle} bought by {CustomerFirstName} {CustomerLastName}", order.Id, order.Amount, order.ArticleNumber, order.Customer.FirstName, order.Customer.LastName);

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

            Logger.LogInformation("Order {OrderId} processed", order.Id);
        }
    }
}
