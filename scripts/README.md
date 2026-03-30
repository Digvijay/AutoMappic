# AutoMappic Scripts

This directory contains internal scripts and utilities used for project maintenance, automated validation, and development workflows.

## Integration Tests (`scripts/integration/`)

These scripts are used to verify the behavior of the sample applications (like `TodoApi`) in a real-world, running environment. They are primarily used during the CI/CD pipeline or for high-confidence manual sanity checks before a release.

| Script | Purpose |
| :--- | :--- |
| `run_todo_test.sh` | Orchestrates the `TodoApi` sample, performs multiple `curl` operations to verify **Smart-Sync** (Insert/Update/Delete) in-place, and cleans up the running process. |

### Usage
To run the integration tests locally, ensure `dotnet` and `jq` are installed, then execute:

```bash
./scripts/integration/run_todo_test.sh
```

---

## Log Cleanup
Stale build and API logs are automatically cleaned up during the release process to maintain a clean repository root.
