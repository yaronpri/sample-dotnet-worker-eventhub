FROM mcr.microsoft.com/dotnet/core/runtime:3.1.1-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1.101-alpine AS build
WORKDIR /src
COPY ["Keda.Samples.DotNet.EventHub.OrderProcessor/Keda.Samples.DotNet.EventHub.OrderProcessor.csproj", "Keda.Samples.DotNet.EventHub.OrderProcessor/"]
COPY ["Keda.Samples.DotNet.EventHub.Contracts/Keda.Samples.DotNet.EventHub.Contracts.csproj", "Keda.Samples.DotNet.EventHub.Contracts/"]
RUN dotnet restore "Keda.Samples.DotNet.EventHub.OrderProcessor/Keda.Samples.DotNet.EventHub.OrderProcessor.csproj"
COPY . .
WORKDIR "/src/Keda.Samples.DotNet.EventHub.OrderProcessor"
RUN dotnet build "Keda.Samples.DotNet.EventHub.OrderProcessor.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Keda.Samples.DotNet.EventHub.OrderProcessor.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Keda.Samples.DotNet.EventHub.OrderProcessor.dll"]
