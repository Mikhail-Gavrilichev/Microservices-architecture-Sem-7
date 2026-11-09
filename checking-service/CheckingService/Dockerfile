FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY CheckingService/CheckingService.csproj CheckingService/
RUN dotnet restore CheckingService/CheckingService.csproj
COPY CheckingService/ CheckingService/
RUN dotnet publish CheckingService/CheckingService.csproj -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /publish .
RUN mkdir /app/photos
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CheckingService.dll"]
