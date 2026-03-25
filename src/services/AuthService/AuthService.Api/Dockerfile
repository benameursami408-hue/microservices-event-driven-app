FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY AuthService/AuthService.Api/AuthService.Api.csproj AuthService/AuthService.Api/
COPY AuthService/AuthService.Application/AuthService.Application.csproj AuthService/AuthService.Application/
COPY AuthService/AuthService.Domain/AuthService.Domain.csproj AuthService/AuthService.Domain/
COPY AuthService/AuthService.Infrastructure/AuthService.Infrastructure.csproj AuthService/AuthService.Infrastructure/
COPY SharedEvents/SharedEvents.csproj SharedEvents/

RUN dotnet restore AuthService/AuthService.Api/AuthService.Api.csproj

COPY . .

WORKDIR /src/AuthService/AuthService.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AuthService.Api.dll"]
