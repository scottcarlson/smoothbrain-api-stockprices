# https://learn.microsoft.com/en-us/dotnet/core/docker/build-container?tabs=linux
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /ApiStockPrices

# Copy everything
COPY ./ApiStockPrices ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o /publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS smoothbrain-api-stockprices
WORKDIR /ApiStockPrices

ARG INTRINIO_API_KEY
ENV INTRINIO_API_KEY ${INTRINIO_API_KEY}

COPY --from=build-env /publish .
EXPOSE 80

# ENTRYPOINT ["tail", "-f", "/dev/null"]

# Use this entrypoint when converting to HTTP/2 streamable API to actually run the server
ENTRYPOINT ["dotnet", "ApiStockPrices.dll"]
