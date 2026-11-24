# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Central package management + shared build props first for layer caching
COPY Directory.Packages.props Directory.Build.props ./
COPY src/OrderFlow.Contracts/OrderFlow.Contracts.csproj src/OrderFlow.Contracts/
COPY src/OrderFlow.Inventory.Domain/OrderFlow.Inventory.Domain.csproj src/OrderFlow.Inventory.Domain/
COPY src/OrderFlow.Inventory.Application/OrderFlow.Inventory.Application.csproj src/OrderFlow.Inventory.Application/
COPY src/OrderFlow.Inventory.Infrastructure/OrderFlow.Inventory.Infrastructure.csproj src/OrderFlow.Inventory.Infrastructure/
COPY src/OrderFlow.Inventory.Api/OrderFlow.Inventory.Api.csproj src/OrderFlow.Inventory.Api/
RUN dotnet restore src/OrderFlow.Inventory.Api/OrderFlow.Inventory.Api.csproj

COPY src/ src/
RUN dotnet publish src/OrderFlow.Inventory.Api/OrderFlow.Inventory.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderFlow.Inventory.Api.dll"]
