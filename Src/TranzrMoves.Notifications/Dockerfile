FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /workspace
COPY ["Directory.Packages.props", "."]
COPY ["Src/TranzrMoves.Notifications/TranzrMoves.Notifications.csproj", "Src/TranzrMoves.Notifications/"]
COPY ["Src/TranzrMoves.Notifications.Application/TranzrMoves.Notifications.Application.csproj", "Src/TranzrMoves.Notifications.Application/"]
COPY ["Src/TranzrMoves.Notifications.Infrastructure/TranzrMoves.Notifications.Infrastructure.csproj", "Src/TranzrMoves.Notifications.Infrastructure/"]
COPY ["Src/TranzrMoves.Notifications.Contracts/TranzrMoves.Notifications.Contracts.csproj", "Src/TranzrMoves.Notifications.Contracts/"]
COPY ["Src/TranzrMoves.Observability/TranzrMoves.Observability.csproj", "Src/TranzrMoves.Observability/"]
RUN dotnet restore "Src/TranzrMoves.Notifications/TranzrMoves.Notifications.csproj"
COPY Src ./Src
WORKDIR /workspace/Src/TranzrMoves.Notifications
RUN dotnet build "./TranzrMoves.Notifications.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TranzrMoves.Notifications.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TranzrMoves.Notifications.dll"]
