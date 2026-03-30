FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /workspace
COPY ["Src/TranzrMoves.Api/TranzrMoves.Api.csproj", "Src/TranzrMoves.Api/"]
COPY ["Src/TranzrMoves.Application/TranzrMoves.Application.csproj", "Src/TranzrMoves.Application/"]
COPY ["Src/TranzrMoves.Infrastructure/TranzrMoves.Infrastructure.csproj", "Src/TranzrMoves.Infrastructure/"]
COPY ["Src/TranzrMoves.Domain/TranzrMoves.Domain.csproj", "Src/TranzrMoves.Domain/"]
RUN dotnet restore "Src/TranzrMoves.Api/TranzrMoves.Api.csproj"
COPY Src ./Src
WORKDIR /workspace/Src/TranzrMoves.Api
RUN dotnet build "./TranzrMoves.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TranzrMoves.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TranzrMoves.Api.dll"]
