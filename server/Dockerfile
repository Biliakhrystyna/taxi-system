FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY TaxiSystem.Api/TaxiSystem.Api.csproj TaxiSystem.Api/
RUN dotnet restore TaxiSystem.Api/TaxiSystem.Api.csproj
COPY TaxiSystem.Api/ TaxiSystem.Api/
RUN dotnet publish TaxiSystem.Api/TaxiSystem.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "TaxiSystem.Api.dll"]
