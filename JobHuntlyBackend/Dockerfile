# Backend image for Fly.io (or any container host). Build context: this folder (JobHuntlyBackend).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore as its own layer so dependencies are cached between builds.
COPY JobHuntly.Api/JobHuntly.Api.csproj JobHuntly.Api/
COPY JobHuntly.Application/JobHuntly.Application.csproj JobHuntly.Application/
COPY JobHuntly.Domain/JobHuntly.Domain.csproj JobHuntly.Domain/
COPY JobHuntly.Infrastructure/JobHuntly.Infrastructure.csproj JobHuntly.Infrastructure/
RUN dotnet restore JobHuntly.Api/JobHuntly.Api.csproj

COPY JobHuntly.Api/ JobHuntly.Api/
COPY JobHuntly.Application/ JobHuntly.Application/
COPY JobHuntly.Domain/ JobHuntly.Domain/
COPY JobHuntly.Infrastructure/ JobHuntly.Infrastructure/
RUN dotnet publish JobHuntly.Api/JobHuntly.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
# Small machines: skip the background GC thread and keep the heap modest.
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_gcConcurrent=false \
    DOTNET_TieredPGO=0
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "JobHuntly.Api.dll"]
