FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /repo

# Copy solution-level files first for better layer caching
COPY global.json nuget.config ./
COPY src/Directory.Build.props src/Directory.Build.targets src/Custom.Build.props src/NServiceBus.snk src/NServiceBusTests.snk src/
COPY src/msbuild/ src/msbuild/
COPY src/NServiceBus.NHibernate.slnx src/

# Copy project files for restore
COPY src/NServiceBus.NHibernate/NServiceBus.NHibernate.csproj src/NServiceBus.NHibernate/
COPY src/NServiceBus.NHibernate.Tests/NServiceBus.NHibernate.Tests.csproj src/NServiceBus.NHibernate.Tests/
COPY src/NServiceBus.NHibernate.AcceptanceTests/NServiceBus.NHibernate.AcceptanceTests.csproj src/NServiceBus.NHibernate.AcceptanceTests/
COPY src/NServiceBus.NHibernate.AcceptanceTests.Oracle/NServiceBus.NHibernate.AcceptanceTests.Oracle.csproj src/NServiceBus.NHibernate.AcceptanceTests.Oracle/
COPY src/NServiceBus.NHibernate.AcceptanceTests.SqlTransport/NServiceBus.NHibernate.AcceptanceTests.SqlTransport.csproj src/NServiceBus.NHibernate.AcceptanceTests.SqlTransport/
COPY src/NServiceBus.NHibernate.PersistenceTests/NServiceBus.NHibernate.PersistenceTests.csproj src/NServiceBus.NHibernate.PersistenceTests/
COPY src/NServiceBus.NHibernate.TransactionalSession/NServiceBus.NHibernate.TransactionalSession.csproj src/NServiceBus.NHibernate.TransactionalSession/
COPY src/NServiceBus.NHibernate.TransactionalSession.AcceptanceTests/NServiceBus.NHibernate.TransactionalSession.AcceptanceTests.csproj src/NServiceBus.NHibernate.TransactionalSession.AcceptanceTests/
COPY src/NServiceBus.NHibernate.TransactionalSession.Tests/NServiceBus.NHibernate.TransactionalSession.Tests.csproj src/NServiceBus.NHibernate.TransactionalSession.Tests/

RUN dotnet restore src

# Copy remaining source
COPY src/ src/

RUN dotnet build src --configuration Debug --no-restore /p:GeneratePackageOnBuild=false

# Default: run the unit/integration tests
ENTRYPOINT ["dotnet", "test", "src/NServiceBus.NHibernate.Tests", "--configuration", "Debug", "--no-build", "--logger", "console;verbosity=normal"]
