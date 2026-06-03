FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /workspace
COPY ["Directory.Packages.props", "."]
COPY ["Src/TranzrMoves.Worker/TranzrMoves.Worker.csproj", "Src/TranzrMoves.Worker/"]
COPY ["Src/TranzrMoves.Application/TranzrMoves.Application.csproj", "Src/TranzrMoves.Application/"]
COPY ["Src/TranzrMoves.Infrastructure/TranzrMoves.Infrastructure.csproj", "Src/TranzrMoves.Infrastructure/"]
COPY ["Src/TranzrMoves.Domain/TranzrMoves.Domain.csproj", "Src/TranzrMoves.Domain/"]
COPY ["Src/TranzrMoves.Observability/TranzrMoves.Observability.csproj", "Src/TranzrMoves.Observability/"]
RUN dotnet restore "Src/TranzrMoves.Worker/TranzrMoves.Worker.csproj"
COPY Src ./Src
WORKDIR /workspace/Src/TranzrMoves.Worker
RUN dotnet build "./TranzrMoves.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TranzrMoves.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TranzrMoves.Worker.dll"]
