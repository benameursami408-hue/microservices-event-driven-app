FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["services/InterventionService/InterventionService.Api/InterventionService.Api.csproj", "services/InterventionService/InterventionService.Api/"]
COPY ["services/InterventionService/InterventionService.Application/InterventionService.Application.csproj", "services/InterventionService/InterventionService.Application/"]
COPY ["services/InterventionService/InterventionService.Domain/InterventionService.Domain.csproj", "services/InterventionService/InterventionService.Domain/"]
COPY ["services/InterventionService/InterventionService.Infrastructure/InterventionService.Infrastructure.csproj", "services/InterventionService/InterventionService.Infrastructure/"]
COPY ["building-blocks/SharedEvents/SharedEvents.csproj", "building-blocks/SharedEvents/"]
RUN dotnet restore "services/InterventionService/InterventionService.Api/InterventionService.Api.csproj"

COPY . .
WORKDIR "/src/services/InterventionService/InterventionService.Api"
RUN dotnet publish "InterventionService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "InterventionService.Api.dll"]
