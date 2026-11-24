# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Packages.props Directory.Build.props ./
COPY src/OrderFlow.Contracts/OrderFlow.Contracts.csproj src/OrderFlow.Contracts/
COPY src/OrderFlow.Notifications.Worker/OrderFlow.Notifications.Worker.csproj src/OrderFlow.Notifications.Worker/
RUN dotnet restore src/OrderFlow.Notifications.Worker/OrderFlow.Notifications.Worker.csproj

COPY src/ src/
RUN dotnet publish src/OrderFlow.Notifications.Worker/OrderFlow.Notifications.Worker.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrderFlow.Notifications.Worker.dll"]
