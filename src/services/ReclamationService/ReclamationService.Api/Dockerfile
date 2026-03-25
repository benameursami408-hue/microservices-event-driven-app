FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ReclamationService/ReclamationService.Api/ReclamationService.Api.csproj ReclamationService/ReclamationService.Api/
COPY ReclamationService/ReclamationService.Application/ReclamationService.Application.csproj ReclamationService/ReclamationService.Application/
COPY ReclamationService/ReclamationService.Domain/ReclamationService.Domain.csproj ReclamationService/ReclamationService.Domain/
COPY ReclamationService/ReclamationService.Infrastructure/ReclamationService.Infrastructure.csproj ReclamationService/ReclamationService.Infrastructure/
COPY SharedEvents/SharedEvents.csproj SharedEvents/

RUN dotnet restore ReclamationService/ReclamationService.Api/ReclamationService.Api.csproj

COPY . .

WORKDIR /src/ReclamationService/ReclamationService.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ReclamationService.Api.dll"]
