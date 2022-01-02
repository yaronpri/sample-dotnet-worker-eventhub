using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Bogus;
using Keda.Samples.DotNet.EventHub.Contracts;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Keda.Samples.Dotnet.EventHub.OrderGenerator
{
    class Program
    {        
        static async Task Main(string[] args)
        {
            Console.WriteLine("Let's send some orders, how many events per second you want to send ?");
            var requestedAmount = DetermineOrderAmount();
            Console.WriteLine("For how many seconds you would like to send it?");
            var requestedSeconds = DetermineSecondAmount();

            await SendEvents(requestedAmount, requestedSeconds);

            Console.WriteLine("That's it, see you later!");
        }

        private static async Task SendEvents(int requestedAmount, int requestedSeconds)
        {            
            var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";            
            IConfiguration Config = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.{env}.json")
                .Build();
            var connectionString = Config.GetSection("EVENTHUB_CONNECTIONSTRING").Value;
            var eventHubName = Config.GetSection("EVENTHUB_NAME").Value;


            var producerClient = new EventHubProducerClient(connectionString, eventHubName);

            for (int sec = 0; sec < requestedSeconds; sec++)
            {
                DateTime before = DateTime.Now;

                using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();
                for (int i = 1; i <= requestedAmount; i++)
                {
                    var order = GenerateOrder();
                    var rawOrder = JsonConvert.SerializeObject(order);

                    var eventData = new EventData(rawOrder);

                    if (!eventBatch.TryAdd(eventData))
                    {
                        throw new Exception($"Event {i} is too large for the batch and cannot be sent.");
                    }
                }
                await producerClient.SendAsync(eventBatch);
                Console.WriteLine($"Second {sec + 1} - A batch of {requestedAmount} - events has been published.");

                var after = DateTime.Now.Subtract(before);

                await Task.Delay(TimeSpan.FromMilliseconds(1000 - after.Milliseconds)); //add the time need to wait for 1 second 
            }                                         
        }

        private static Order GenerateOrder()
        {
            var customerGenerator = new Faker<Customer>()
                .RuleFor(u => u.FirstName, (f, u) => f.Name.FirstName())
                .RuleFor(u => u.LastName, (f, u) => f.Name.LastName());

            var orderGenerator = new Faker<Order>()
                .RuleFor(u => u.Customer, () => customerGenerator)
                .RuleFor(u => u.Id, f => Guid.NewGuid().ToString())
                .RuleFor(u => u.Amount, f => f.Random.Int())
                .RuleFor(u => u.ArticleNumber, f => f.Commerce.Product());

            return orderGenerator.Generate();
        }

        private static int DetermineOrderAmount()
        {
            var rawAmount = Console.ReadLine();
            if (int.TryParse(rawAmount, out int amount))
            {
                return amount;
            }

            Console.WriteLine("That's not a valid amount, let's try that again");
            return DetermineOrderAmount();
        }

        private static int DetermineSecondAmount()
        {
            var rawAmount = Console.ReadLine();
            if (int.TryParse(rawAmount, out int amount))
            {
                return amount;
            }

            Console.WriteLine("That's not a valid seconds, let's try that again");
            return DetermineOrderAmount();
        }
    }
}
