# HelloDevOps — a hands-on DevOps walkthrough with GitHub and .NET 11

A deliberately tiny ASP.NET Core 11 API with an xUnit test project. The goal is to learn the **process** around the code:

```
 feature branch ──► commit ──► push ──► Pull Request ──► CI (build + test + container)
                                              │
                         ruleset: main is protected, PR + green CI required
                                              │
                                         merge to main ──► image pushed to ghcr.io
                                              │
                                    git tag v1.0.0 ──► versioned release image
```

> **main vs master:** GitHub now names the default branch `main`. Everything here uses `main`. If you prefer `master`, replace it everywhere, including `ci.yml`.

---

## What's in this repo

| Path | Purpose |
|---|---|
| `src/HelloDevOps.Api/` | Minimal API: `/`, `/health`, `/add/{a}/{b}`, `/subtract/{a}/{b}` |
| `tests/HelloDevOps.Tests/` | xUnit tests for `Calculator` |
| `.github/workflows/ci.yml` | The pipeline: build, test, build the container, smoke-test it, push to GHCR |
| `.github/dependabot.yml` | Automatic dependency-update PRs |
| `Dockerfile` / `.dockerignore` | Multi-stage container build |
| `.gitattributes` | Keeps line endings consistent between Windows and Linux |

---

## Phase 0 — Install the tools (one time)

1. **.NET 11 SDK**: https://dotnet.microsoft.com/download. Check it with `dotnet --version`, which should print `11.0.x`.
2. **Git for Windows**, which includes **Git Bash**: https://git-scm.com. Check it with `git --version`.
3. **Docker Desktop**: https://www.docker.com/products/docker-desktop. Check it with `docker --version`.
4. *(Optional but useful)* the **GitHub CLI** `gh`: https://cli.github.com. Then run `gh auth login`.
5. An editor such as VS Code with the C# Dev Kit, or Visual Studio 2026.

Configure Git once in **Git Bash**:

```bash
git config --global user.name  "Your Name"
git config --global user.email "you@example.com"     # same email as your GitHub account
git config --global init.defaultBranch main
git config --global pull.rebase false                # "git pull" = fetch + merge (simplest)
```

---

## Phase 1 — Create the empty repo on GitHub

1. GitHub → **New repository** → name it `HelloDevOps`.
2. Choose **Public**. Rulesets on *private* repos need a paid plan (GitHub Pro or Team). On a free account, a public repo is the easiest option.
3. Do **not** tick *Add README*, *.gitignore* or *license*. You want it **empty**, so your first push doesn't conflict.
4. Click **Create repository** and copy the URL, for example `https://github.com/<you>/HelloDevOps.git`.

---

## Phase 2 — Scaffold the solution locally

In Git Bash:

```bash
mkdir HelloDevOps && cd HelloDevOps
git init                                    # creates a local repo on branch main

dotnet new gitignore                        # standard .NET .gitignore (ignores bin/, obj/ ...)
dotnet new sln -n HelloDevOps

dotnet new web   -n HelloDevOps.Api   -o src/HelloDevOps.Api     --framework net11.0
dotnet new xunit -n HelloDevOps.Tests -o tests/HelloDevOps.Tests --framework net11.0

dotnet sln add src/HelloDevOps.Api tests/HelloDevOps.Tests
dotnet add tests/HelloDevOps.Tests reference src/HelloDevOps.Api

rm tests/HelloDevOps.Tests/UnitTest1.cs     # template sample, we have our own
```

Now **copy the files from this zip over the top** of that folder, and answer *Overwrite* when asked (that overwrites `Program.cs`). Copy these:

```
src/HelloDevOps.Api/Program.cs
src/HelloDevOps.Api/Calculator.cs
tests/HelloDevOps.Tests/CalculatorTests.cs
.github/workflows/ci.yml
.github/dependabot.yml
Dockerfile
.dockerignore
.gitattributes
README.md
```

Then prove it all works **locally first** (this is the habit CI automates):

```bash
dotnet build
dotnet test                                 # should report all tests passed
dotnet run --project src/HelloDevOps.Api    # note the URL it prints, e.g. http://localhost:5xxx
```

Open `http://localhost:5xxx/add/2/3` in a browser. You should see `{"a":2,"b":3,"result":5}`. Press `Ctrl+C` to stop the app.

---

## Phase 3 — First commit, pushed straight to main

This is the **only** time you push directly to `main`. The ruleset doesn't exist yet.

```bash
git status                                  # see what's new (red = untracked)
git add .                                   # stage everything
git status                                  # green = staged
git commit -m "Initial commit: API, tests, CI, Dockerfile"

git remote add origin https://github.com/<you>/HelloDevOps.git
git push -u origin main                     # -u links local main to origin/main
```

Go to the repo's **Actions** tab. The **CI** workflow is running. Wait for both jobs (`build-and-test` and `docker`) to turn green ✅.

> Why wait? GitHub only lets you pick a job as a *required status check* after that job has run at least once.

---

## Phase 4 — Protect main with a ruleset

**Settings → Rules → Rulesets → New ruleset → New branch ruleset**

| Setting | Value |
|---|---|
| Ruleset name | `protect-main` |
| Enforcement status | **Active** |
| Target branches | **Add target → Include default branch** |
| ✅ Restrict deletions | stops anyone deleting `main` |
| ✅ Block force pushes | stops anyone rewriting `main`'s history |
| ✅ **Require a pull request before merging** | Required approvals: **0** if you work solo (you can't approve your own PR); use 1+ in a team. Optionally tick *Dismiss stale approvals when new commits are pushed*. |
| ✅ **Require status checks to pass** | Tick *Require branches to be up to date before merging*. Click **Add checks**, then add **`build-and-test`** and **`docker`**. |

Click **Create**.

Two more settings help:

* **Settings → General → Pull Requests**: tick **Automatically delete head branches**. GitHub then deletes the remote branch after merge.
* On the same page, you can allow only **Squash merging**. Each PR then becomes one clean commit on `main`.

**Test it.** Try a direct push and watch it be rejected:

```bash
echo "test" >> README.md
git commit -am "try direct push"
git push                                    # ❌ rejected: changes must be made through a pull request
git reset --hard HEAD~1                     # undo that local commit
```

---

## Phase 5 — The daily loop: feature branch → PR → merge

The exercise: add a `Multiply` feature.

### 5.1 Start from an up-to-date main

```bash
git switch main                             # (older syntax: git checkout main)
git pull                                    # get the latest main from GitHub
```

### 5.2 Create your feature branch

```bash
git switch -c feature/multiply              # create + switch (older: git checkout -b feature/multiply)
git branch                                  # * marks the branch you're on
```

Branch names such as `feature/...`, `fix/...` and `chore/...` make the purpose obvious.

### 5.3 Change the code locally

In `src/HelloDevOps.Api/Calculator.cs`, add:

```csharp
public static int Multiply(int a, int b) => a * b;
```

In `src/HelloDevOps.Api/Program.cs`, add this before `app.Run();`:

```csharp
app.MapGet("/multiply/{a:int}/{b:int}", (int a, int b) =>
    Results.Ok(new { a, b, result = Calculator.Multiply(a, b) }));
```

In `tests/HelloDevOps.Tests/CalculatorTests.cs`, add:

```csharp
[Fact]
public void Multiply_TwoNumbers_ReturnsProduct()
{
    Assert.Equal(12, Calculator.Multiply(3, 4));
}
```

Test locally:

```bash
dotnet test
```

### 5.4 Commit and push the branch

```bash
git status                                  # what changed?
git diff                                    # exactly what changed (press q to quit)
git add .
git commit -m "Add multiply endpoint and tests"
git push -u origin feature/multiply         # first push of a new branch needs -u
```

To make more commits on the same branch later, run `git add .`, then `git commit -m "..."`, then just `git push`.

### 5.5 Open a Pull Request

**On the web:** GitHub shows a yellow banner, *feature/multiply had recent pushes*. Click **Compare & pull request**, set base: `main` ← compare: `feature/multiply`, write a description, and click **Create pull request**.

**Or with the CLI:**

```bash
gh pr create --base main --fill             # --fill uses your commit message as title/body
gh pr checks --watch                        # watch CI live in the terminal
```

On the PR page, the **Merge** button stays blocked until `build-and-test` and `docker` are green. That's your ruleset at work.

### 5.6 Merge

Once the checks are green, click **Squash and merge**, then **Confirm**. With the CLI, run `gh pr merge --squash --delete-branch`.

Merging pushes to `main`, so CI runs again. This time the `docker` job **pushes the image** to GHCR (see Phase 7).

### 5.7 Clean up locally

```bash
git switch main
git pull                                    # bring the merged commit down
git branch -d feature/multiply              # delete local branch
git fetch --prune                           # forget remote branches deleted on GitHub
git branch -a                               # check: only main + origin/main remain
```

> **"error: the branch is not fully merged"?** That's normal after a **squash** merge. The squash created a *new* commit on main, so Git can't see that your branch's commits are "in" main. Once you've confirmed the PR merged on GitHub, force-delete it with `git branch -D feature/multiply`.

**That's the whole loop.** Repeat it for every change: `switch main → pull → switch -c → code → test → commit → push → PR → green → merge → clean up`.

---

## Phase 6 — Watch CI protect you (do this, it's the "aha" moment)

```bash
git switch main && git pull
git switch -c fix/broken-on-purpose
```

Break a test. For example, change `Assert.Equal(5, Calculator.Add(2, 3));` to expect `6`. Then:

```bash
git commit -am "Break a test on purpose"
git push -u origin fix/broken-on-purpose
gh pr create --base main --fill             # or use the web UI
```

CI goes red ❌ and the **Merge button is blocked**. Fix the test, then `git commit -am "Fix test"` and `git push`. The PR updates itself, CI reruns and goes green, and now you can merge. (Or close the PR and delete the branch.)

---

## Phase 7 — Containers

### 7.1 Build and run locally

With Docker Desktop running:

```bash
docker build -t hellodevops .               # uses the Dockerfile in this folder
docker images                               # your image is listed
docker run -d --name hello -p 8080:8080 hellodevops
curl http://localhost:8080/health           # {"status":"healthy"}
curl http://localhost:8080/add/2/3
docker logs hello                           # app output
docker stop hello && docker rm hello        # clean up
```

How the **multi-stage Dockerfile** works:
* The `sdk:11.0` image (large) compiles and publishes the app.
* Only the published output is copied into `aspnet:11.0` (much smaller), which runs as a **non-root** user on port **8080**.
* The `.csproj` is copied and restored *before* the source, so Docker caches the NuGet restore layer.

### 7.2 What CI does with the container

* **On every PR:** builds the image and starts it, then calls `/health` (a "smoke test"). Nothing is published.
* **On merge to main:** also pushes `ghcr.io/<you>/hellodevops:latest` and `:sha-xxxxxxx`.
* **On a version tag:** pushes `ghcr.io/<you>/hellodevops:1.0.0` (see Phase 8).

After your first merge, open your GitHub profile → **Packages**. The image is listed there. To pull and run it from anywhere:

```bash
docker run -p 8080:8080 ghcr.io/<you>/hellodevops:latest
```

> The package starts **private**. To make pulls work without logging in, open the package page → **Package settings** → **Change visibility** → Public.
> To pull a private image, log in first with a Personal Access Token that has `read:packages`: `echo <PAT> | docker login ghcr.io -u <you> --password-stdin`.

---

## Phase 8 — Releases with tags (semantic versioning)

When `main` is in a state you want to release:

```bash
git switch main && git pull
git tag -a v1.0.0 -m "First release"
git push origin v1.0.0
```

CI runs for the tag and publishes `ghcr.io/<you>/hellodevops:1.0.0`. To create a GitHub Release with auto-generated notes from your PRs, go to **Releases → Draft a new release**, pick the tag, and click **Generate release notes**. With the CLI, run `gh release create v1.0.0 --generate-notes`.

Versioning: `MAJOR.MINOR.PATCH`. Bump PATCH for fixes, MINOR for new features (multiply = v1.1.0), and MAJOR for breaking changes.

---

## Phase 9 — Dependabot (automated maintenance)

`.github/dependabot.yml` checks weekly for newer NuGet packages, GitHub Actions versions and Docker base images, and opens **PRs** for them. Those PRs go through exactly the same CI and ruleset as yours. You just review the green ones and merge. Also turn on **Settings → Advanced Security → Dependabot alerts** to get notified about security vulnerabilities.

---

## Git cheat sheet

| Goal | Command |
|---|---|
| Where am I / what changed? | `git status`, `git diff`, `git log --oneline --graph -10` |
| Update main | `git switch main && git pull` |
| New branch | `git switch -c feature/name` |
| Stage + commit | `git add .` then `git commit -m "message"` |
| First push of a branch | `git push -u origin feature/name` |
| Later pushes | `git push` |
| Bring latest main into my branch | `git switch feature/name && git merge main` (or `git pull origin main`) |
| Undo unstaged changes to a file | `git restore path/to/file` |
| Unstage a file | `git restore --staged path/to/file` |
| Undo the last local (unpushed) commit, keep changes | `git reset --soft HEAD~1` |
| Park work temporarily | `git stash`, later `git stash pop` |
| Delete local branch | `git branch -d name` (`-D` after a squash merge) |
| Remove stale remote branches | `git fetch --prune` |
| List all branches | `git branch -a` |

**Merge conflict?** It happens when `main` changed the same lines you changed. Run `git merge main` on your branch, open the files marked `<<<<<<<` / `=======` / `>>>>>>>`, keep the right code, then run `git add .`, `git commit`, and `git push`.

---

## Where to go next (the "D" in CD)

1. **Environments:** **Settings → Environments** → create `staging` and `production`. You can add *required reviewers* to `production`, so a deploy waits for a human click.
2. **A deploy job:** add a third job that `needs: docker` and deploys the image, for example to **Azure Container Apps**, **AWS App Runner** or **Google Cloud Run**. Authenticate with **OIDC** (`permissions: id-token: write`) instead of storing cloud passwords as secrets.
3. **Code coverage:** run `dotnet test --collect:"XPlat Code Coverage"` and publish the report.
4. **Code scanning:** enable **CodeQL** in **Settings → Advanced Security**.
5. **Infrastructure as Code:** describe the cloud resources in Bicep or Terraform, stored in the same repo and deployed by the same pipeline.
