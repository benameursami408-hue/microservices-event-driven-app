FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NotificationService/NotificationService.Api/NotificationService.Api.csproj NotificationService/NotificationService.Api/
COPY NotificationService/NotificationService.Application/NotificationService.Application.csproj NotificationService/NotificationService.Application/
COPY NotificationService/NotificationService.Domain/NotificationService.Domain.csproj NotificationService/NotificationService.Domain/
COPY NotificationService/NotificationService.Infrastructure/NotificationService.Infrastructure.csproj NotificationService/NotificationService.Infrastructure/
COPY SharedEvents/SharedEvents.csproj SharedEvents/

RUN dotnet restore NotificationService/NotificationService.Api/NotificationService.Api.csproj

COPY . .

WORKDIR /src/NotificationService/NotificationService.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "NotificationService.Api.dll"]
