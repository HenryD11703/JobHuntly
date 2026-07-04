FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["JobHuntly.Api/JobHuntly.Api.csproj", "JobHuntly.Api/"]
COPY ["JobHuntly.Application/JobHuntly.Application.csproj", "JobHuntly.Application/"]
COPY ["JobHuntly.Domain/JobHuntly.Domain.csproj", "JobHuntly.Domain/"]
COPY ["JobHuntly.Infrastructure/JobHuntly.Infrastructure.csproj", "JobHuntly.Infrastructure/"]
RUN dotnet restore "JobHuntly.Api/JobHuntly.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/JobHuntly.Api"
RUN dotnet build "JobHuntly.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "JobHuntly.Api.csproj" -c Release -o /app/publish

# Final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "JobHuntly.Api.dll"]
