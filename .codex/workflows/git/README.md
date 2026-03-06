# Git Auto Workflow

Scripts:
- `auto-branch.ps1`: create/switch branch with optional base sync
- `auto-commit.ps1`: stage all, commit, optional push
- `auto-pull.ps1`: fetch + pull (rebase by default)
- `auto-workflow.ps1`: one-shot branch creation and optional commit

Examples:

```powershell
# 1) base sync + branch
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\git\auto-branch.ps1 -Name "tether-repair-loop" -Base main -SyncBase

# 2) pull latest on current branch
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\git\auto-pull.ps1

# 3) commit + push
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\git\auto-commit.ps1 -Message "feat: tighten owner interaction checks" -Push

# 4) one-shot (branch first, optional commit)
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\git\auto-workflow.ps1 -Task "network-regression" -Base main -CommitMessage "chore: network regression checkpoint" -Push
```

Notes:
- Branch names are normalized and auto-prefixed with `codex/`.
- Scripts fail fast if the current folder is not a Git repository.
- `auto-commit.ps1 -Push` pulls with rebase before push when upstream exists.
