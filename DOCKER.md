# Running the tests in Docker

These commands require **only Docker** — no local .NET SDK and no local SQL Server.

## Run the issue #959 regression test (zero dependencies)

The #959 test (`When_configuration_contains_non_saga_collection_mappings`) compiles
the NHibernate mappings in memory and never opens a database connection, so it needs
no SQL Server at all:

```bash
docker compose run --build issue-959-test
```

The container builds the solution and runs that single test. It exits `0` when the
test passes (the fix is present) and non-zero if it fails (the bug is present).

## Run the full unit / integration suite

The full suite needs SQL Server. Compose starts one in a container automatically, so
you still don't need anything installed locally:

```bash
docker compose up --build --abort-on-container-exit tests
```

This brings up `sql-server`, creates the `nservicebus` database (`db-init`), then runs
the `NServiceBus.NHibernate.Tests` project against it.

## Run the suite against your own SQL Server

If you'd rather point at a SQL Server running on your host, edit the connection string
in the `tests-local-sql` service in `docker-compose.yml` and run:

```bash
docker compose run --build tests-local-sql
```

## Clean up

```bash
docker compose down -v
```
