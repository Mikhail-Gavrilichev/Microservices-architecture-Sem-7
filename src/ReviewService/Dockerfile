FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props ./
COPY src/ReviewService/ ./src/ReviewService/

RUN dotnet restore src/ReviewService/ReviewService.Api/ReviewService.Api.csproj
RUN dotnet publish src/ReviewService/ReviewService.Api/ReviewService.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Npgsql проверяет наличие GSSAPI при старте; пакет убирает ложное сообщение
# "Cannot load library libgssapi_krb5.so.2" в логах.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ReviewService.Api.dll"]
