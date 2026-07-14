# Git Cheat Sheet

A quick reference for the Git commands most commonly used during software development.

---

# Check Repository Status

Display the current status of the working directory.

```bash
git status
```

Shows:

- Current branch
- Modified files
- New files
- Deleted files
- Files staged for commit

---

# Stage Changes

Stage all modified files.

```bash
git add .
```

Stage a single file.

```bash
git add README.md
```

Stage a folder.

```bash
git add docs/
```

---

# Create a Commit

Create a local commit.

```bash
git commit -m "docs: add Git cheat sheet"
```

Common commit prefixes:

| Prefix | Description |
|---------|-------------|
| feat | New feature |
| fix | Bug fix |
| docs | Documentation |
| refactor | Internal code improvement |
| test | Tests |
| chore | Maintenance or configuration |

---

# Push Changes

Upload commits to the remote repository.

```bash
git push
```

First push for a new branch:

```bash
git push -u origin main
```

---

# Pull Changes

Download and merge the latest changes.

```bash
git pull
```

Recommended workflow:

```bash
git status
git pull
```

---

# Clone Repository

Clone a repository for the first time.

```bash
git clone <repository-url>
```

Example:

```bash
git clone https://github.com/username/staff-engineer-handbook.git
```

Open the project.

```bash
cd staff-engineer-handbook
code .
```

---

# View Commit History

Complete history.

```bash
git log
```

Compact history.

```bash
git log --oneline
```

Graph view.

```bash
git log --graph --decorate --oneline --all
```

---

# View Changes

Unstaged changes.

```bash
git diff
```

Staged changes.

```bash
git diff --staged
```

---

# Unstage Files

Remove a file from the staging area.

```bash
git restore --staged README.md
```

---

# Discard Local Changes

Discard changes to a file.

```bash
git restore README.md
```

---

# Branch Management

Create and switch to a new branch.

```bash
git switch -c feature/my-feature
```

Switch branches.

```bash
git switch main
```

List branches.

```bash
git branch
```

---

# Push a New Branch

```bash
git push -u origin feature/my-feature
```

---

# Remote Repositories

List remotes.

```bash
git remote -v
```

Add a remote.

```bash
git remote add origin <repository-url>
```

Update a remote URL.

```bash
git remote set-url origin <repository-url>
```

---

# Recommended Daily Workflow

Start your work.

```bash
git status
git pull
```

Review changes.

```bash
git diff
```

Create a commit.

```bash
git add .
git commit -m "docs: update handbook"
git push
```

Verify a clean repository.

```bash
git status
```

Expected output:

```text
On branch main
Your branch is up to date with 'origin/main'.

nothing to commit, working tree clean
```