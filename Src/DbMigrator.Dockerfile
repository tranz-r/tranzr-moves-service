# Use the official .NET 8 SDK image to build the project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env

# Set the working directory
WORKDIR /src

# Copy the rest of the project files
COPY Src/TranzrMoves.Domain src/TranzrMoves.Domain
COPY Src/TranzrMoves.Application src/TranzrMoves.Application
COPY Src/TranzrMoves.Infrastructure src/TranzrMoves.Infrastructure


ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet tool install --global dotnet-ef && \
    mkdir /migrations && \
    dotnet-ef migrations bundle --self-contained -r linux-x64 --project src/TranzrMoves.Infrastructure -o /migrations/migrator --force

# Copy the migration script
COPY Src/db-migration.sh /migrations/db-migration.sh

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /migrations

COPY --from=build-env /migrations/migrator migrator
COPY --from=build-env /migrations/db-migration.sh db-migration.sh


RUN chmod +x migrator
RUN chmod +x db-migration.sh

ENTRYPOINT ["/bin/sh", "./db-migration.sh"]
