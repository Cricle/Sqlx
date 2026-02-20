# E2E Tests for Multiple Result Sets

This directory contains End-to-End (E2E) tests for the multiple result sets feature across different database systems using Testcontainers.

## Prerequisites

To run the E2E tests, you need:

1. **Docker Desktop** installed and running
   - Windows: [Docker Desktop for Windows](https://docs.docker.com/desktop/install/windows-install/)
   - macOS: [Docker Desktop for Mac](https://docs.docker.com/desktop/install/mac-install/)
   - Linux: [Docker Engine](https://docs.docker.com/engine/install/)

2. **.NET 10 SDK** (or the target framework specified in the project)

## Running E2E Tests

### Automatic Docker Detection

The E2E tests now automatically detect if Docker is available:

- **Docker Available**: Tests run normally against real database containers
- **Docker Not Available**: Tests are marked as "Inconclusive" and gracefully skipped
- **No Manual Intervention Required**: You can run the full test suite without worrying about Docker availability

### Running the Tests

1. **Ensure Docker is running** (optional - tests will skip if not available):
   ```bash
   docker ps
   ```
   This should return a list of running containers (or empty if no containers are running).

2. **Run the E2E tests**:
   ```bash
   # Run all E2E tests
   dotnet test --filter "FullyQualifiedName~MultipleResultSetsE2ETests"
   
   # Run MySQL tests only
   dotnet test --filter "FullyQualifiedName~MultipleResultSetsE2ETests.MySQL"
   
   # Run PostgreSQL tests only
   dotnet test --filter "FullyQualifiedName~MultipleResultSetsE2ETests.PostgreSQL"
   
   # Run SQL Server tests only
   dotnet test --filter "FullyQualifiedName~MultipleResultSetsE2ETests.SqlServer"
   ```

3. **Run all tests including E2E**:
   ```bash
   dotnet test
   ```
   E2E tests will automatically skip if Docker is not available.

## What Gets Tested

The E2E tests verify Sqlx functionality across three major database systems. Currently focused on:

### Multiple Result Sets (Current Coverage)
- ✅ Async method with multiple result sets
- ✅ Sync method with multiple result sets  
- ✅ Multiple SELECT statements
- ✅ Database-specific identity functions (LAST_INSERT_ID, SCOPE_IDENTITY, currval)

### Future E2E Test Coverage (Planned)
The following features are thoroughly tested in unit tests and can be extended to E2E tests as needed:
- CRUD Operations (Insert, Update, Delete, Select)
- Transactions (Commit, Rollback)
- Batch Operations
- Output Parameters
- SqlBuilder dynamic queries
- Type Conversions
- Complex queries with JOINs
- Stored Procedures

### Databases Tested
- **MySQL 8.0**: Full multiple result sets support
- **PostgreSQL 16**: Full multiple result sets support
- **SQL Server 2022**: Full multiple result sets support

## Test Containers

The tests use the following Docker images:

- **MySQL**: `mysql:8.0`
- **PostgreSQL**: `postgres:16`
- **SQL Server**: `mcr.microsoft.com/mssql/server:2022-latest`

These containers are automatically:
- Started before the tests run
- Configured with test databases and credentials
- Cleaned up after the tests complete

## Troubleshooting

### Docker Not Running

If you see errors like:
```
Docker is either not running or misconfigured
```

**Solution**: Start Docker Desktop and ensure it's running:
```bash
docker ps
```

### Port Conflicts

If containers fail to start due to port conflicts:

**Solution**: Stop any services using the default database ports:
- MySQL: 3306
- PostgreSQL: 5432
- SQL Server: 1433

Or let Testcontainers use random ports (default behavior).

### Slow First Run

The first time you run the tests, Docker needs to download the database images. This can take several minutes depending on your internet connection.

**Solution**: Be patient on the first run. Subsequent runs will be much faster as the images are cached.

### Container Cleanup

If containers are not cleaned up properly:

**Solution**: Manually remove them:
```bash
# List all containers
docker ps -a

# Remove specific container
docker rm -f <container-id>

# Remove all stopped containers
docker container prune
```

## CI/CD Integration

The E2E tests are integrated into the CI/CD pipeline with automatic Docker detection:

### GitHub Actions Workflow

The project includes two separate test jobs in `.github/workflows/ci-cd.yml`:

1. **Unit Tests Job** (`test`):
   - Runs all tests except E2E tests
   - Uses filter: `--filter "FullyQualifiedName!~E2ETests"`
   - Collects code coverage
   - Runs in ~10 minutes

2. **E2E Tests Job** (`e2e-test`):
   - Runs only E2E tests against real databases
   - Uses filter: `--filter "FullyQualifiedName~E2ETests"`
   - Verifies Docker availability before running
   - Automatically cleans up containers after tests
   - Runs in ~15 minutes
   - Depends on unit tests passing first

### Behavior in CI

- **Docker Available** (GitHub Actions Ubuntu runners): Tests run automatically against MySQL, PostgreSQL, and SQL Server
- **Docker Not Available**: Tests are marked as "Inconclusive" and don't fail the build
- **No Configuration Required**: The tests detect Docker availability automatically

### Local Development

When running tests locally:
- Without Docker: E2E tests are skipped gracefully
- With Docker: E2E tests run against real database containers

Example commands:
```bash
# Run only unit tests (fast)
dotnet test --filter "FullyQualifiedName!~E2ETests"

# Run only E2E tests (requires Docker)
dotnet test --filter "FullyQualifiedName~E2ETests"

# Run all tests
dotnet test
```

## Performance Notes

- **Container Startup**: ~10-30 seconds per database
- **Test Execution**: ~1-5 seconds per test
- **Total Time**: ~2-5 minutes for all E2E tests

## See Also

- [Testcontainers Documentation](https://dotnet.testcontainers.org/)
- [Multiple Result Sets Documentation](../../docs/multiple-result-sets.md)
- [Main Test Suite](MultipleResultSetsTests.cs)
