# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Central package management + shared build props first for layer caching
COPY Directory.Packages.props Directory.Build.props ./
COPY src/OrderFlow.Contracts/OrderFlow.Contracts.csproj src/OrderFlow.Contracts/
COPY src/OrderFlow.Orders.Domain/OrderFlow.Orders.Domain.csproj src/OrderFlow.Orders.Domain/
COPY src/OrderFlow.Orders.Application/OrderFlow.Orders.Application.csproj src/OrderFlow.Orders.Application/
COPY src/OrderFlow.Orders.Infrastructure/OrderFlow.Orders.Infrastructure.csproj src/OrderFlow.Orders.Infrastructure/
COPY src/OrderFlow.Orders.Api/OrderFlow.Orders.Api.csproj src/OrderFlow.Orders.Api/
RUN dotnet restore src/OrderFlow.Orders.Api/OrderFlow.Orders.Api.csproj

COPY src/ src/
RUN dotnet publish src/OrderFlow.Orders.Api/OrderFlow.Orders.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderFlow.Orders.Api.dll"]
