# ume-nuget

Shared NuGet libraries developed by **Team Turkos** at Umeå kommun. The packages are open source but primarily built for Team Turkos' own services. This repository produces two packages:

| Package         | NuGet ID              | Description                                                               |
| --------------- | --------------------- | ------------------------------------------------------------------------- |
| **Toolkit**     | `Umea.se.Toolkit`     | Shared runtime building blocks for ASP.NET Core APIs and Azure Functions. |
| **TestToolkit** | `Umea.se.TestToolkit` | Integration-test harness with pre-wired mocks and test base classes.      |

Both packages target **.NET 10** and are published to an Azure Artifacts feed (`turkos.umea.se`) and, for stable releases, to [NuGet.org](https://www.nuget.org/profiles/umeakommun).

---

## CI/CD & Deployment

The repository uses **Azure DevOps Pipelines** with three pipeline definitions:

### 1. Release Validation (`release-validation.yml`)

- **Trigger:** PR build validation on `main`
- **Purpose:** Builds and tests changed packages to gate merges
- Detects which packages changed and only builds those
- Runs tests with code coverage

### 2. Release Orchestrator (`release-orchestrator.yml`)

- **Trigger:** Merged pull request to `main`
- **Stages:** Validate → Get PR info → Run publish pipeline → Finalize (tag commit, tag build, update work items)
- Prevents manual runs from `main` — releases happen only via merged pull requests

### 3. Publish Packages (`publish-packages.yml`)

- **Trigger:** Called by the release orchestrator, or manually for pre-releases
- **Stages:** Validate → Check changes → Pack → Publish (per package)
- Generates a date-based version, runs tests, packs `.nupkg`, and pushes to:
  - **Azure Artifacts** (`turkos.umea.se`) — all releases
  - [NuGet.org](https://www.nuget.org/profiles/umeakommun) — stable releases only

## Versioning

Package versions follow a **date-based** scheme:

| Release Type    | Format                                                | Example                       |
| --------------- | ----------------------------------------------------- | ----------------------------- |
| **Stable**      | `YYYY.M.D.<commit-count>`                             | `2026.3.9.147`                |
| **Pre-release** | `YYYY.M.D.<baseline-count>-dev.<branch-hash>.<delta>` | `2026.3.9.147-dev.a1b2c3d4.3` |

- The date component uses **Stockholm local time**
- The commit count is scoped to the package path for independent versioning
- `baseline-count` is the `main` commit count at the branch's last sync point, indicating which stable release the pre-release builds on
- `delta` is the number of commits on the branch ahead of `main`
- `branch-hash` is a truncated SHA-256 of the branch name for uniqueness
- Version tags are written as `<package>/<version>` (e.g., `toolkit/2026.3.9.147`)

---

## License

This project is licensed under the **GNU Affero General Public License v3.0 (AGPL-3.0)**. See the [LICENSE](LICENSE) file for full terms.
