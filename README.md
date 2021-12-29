# .NET Core worker processing Azure Event Hub scaled by KEDA
A simple Docker container written in .NET that will receive messages from a Azure Event Hub and scale via KEDA.

The message processor will receive a single message at a time (per instance), and sleep for 1 second to simulate performing work. When adding a massive amount of queue messages, KEDA will drive the container to scale out according to the event source (Azure EventHub).
