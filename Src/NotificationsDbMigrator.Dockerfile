# Use the official .NET SDK image to build the project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env

# Set the working directory
WORKDIR /src

COPY Directory.Packages.props .

# Copy required project files (Infrastructure has EF Design; no startup host needed)
COPY Src/TranzrMoves.Notifications.Contracts Src/TranzrMoves.Notifications.Contracts
COPY Src/TranzrMoves.Notifications.Infrastructure Src/TranzrMoves.Notifications.Infrastructure

ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet tool install --global dotnet-ef && \
    dotnet restore Src/TranzrMoves.Notifications.Infrastructure/TranzrMoves.Notifications.Infrastructure.csproj && \
    mkdir /migrations && \
    dotnet-ef migrations bundle --self-contained -r linux-x64 \
      --project Src/TranzrMoves.Notifications.Infrastructure \
      -o /migrations/migrator --force

# Copy the migration script
COPY Src/notifications-db-migration.sh /migrations/db-migration.sh

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /migrations

COPY --from=build-env /migrations/migrator migrator
COPY --from=build-env /migrations/db-migration.sh db-migration.sh

RUN chmod +x migrator
RUN chmod +x db-migration.sh

ENTRYPOINT ["/bin/sh", "./db-migration.sh"]
