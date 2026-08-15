---
name: gh-stack
description: Create and manage GitHub stacked branches and pull requests in ApesDb with the official gh-stack CLI extension. Use for stacked diffs, dependent PRs, branch chains, splitting a feature into reviewable layers, submitting or syncing stacks, updating a lower layer, checking stack status, adopting existing branches or PRs, and merging or unstacking a stack.
---

# GitHub stacked pull requests

Use `gh stack` to keep dependent changes in small, ordered PRs. Treat the bottom branch as the foundation closest to the trunk and the top branch as the most dependent layer.

## Preserve repository state

- Inspect `git status --short --branch`, `git diff`, `git diff --cached`, remotes, and the current branch before changing branches or stack metadata.
- Preserve unrelated user changes. Never stash, reset, discard, overwrite, or absorb them into a stack without explicit permission.
- If the worktree is dirty, identify which changes belong to each requested layer before creating branches. Stop when unrelated changes cannot be separated safely.
- Stage exact paths for each layer. Do not default to `git add -A` or `gh stack add -A`.
- Keep each layer independently reviewable and place dependencies below their consumers.
- Use `codex/`-prefixed branch names unless the user specifies another convention.

## Preflight

Run these read-only checks first:

```bash
gh auth status
gh stack --help
git remote -v
git status --short --branch
gh repo view --json defaultBranchRef,mergeCommitAllowed,rebaseMergeAllowed,squashMergeAllowed
```

Use the repository default branch as the trunk and the push remote as the stack remote. In this repository those are normally `main` and `origin`; pass them explicitly where supported. If multiple remotes exist, set `remote.pushDefault` only when the correct push target is established.

## Plan the layers

Describe the proposed bottom-to-top branch order before creating it. Prefer boundaries such as:

1. shared contracts, models, or migrations;
2. backend behavior and HTTP API;
3. frontend behavior;
4. integration coverage or dependent cleanup.

Keep unrelated work in a separate stack. If two layers do not depend on one another, prefer separate branches or stacks instead of inventing a dependency.

## Create a stack

Always provide explicit branch names so commands remain non-interactive:

```bash
gh stack init --base main codex/<story>-foundation
git add <exact-paths>
git commit -m "<focused commit message>"

gh stack add codex/<story>-api
git add <exact-paths>
git commit -m "<focused commit message>"

gh stack add codex/<story>-ui
git add <exact-paths>
git commit -m "<focused commit message>"
```

After every commit, inspect `git status --short` and the layer diff against its parent. Uncommitted changes carry into a newly added branch; ensure they are intentional before running `gh stack add`.

To adopt an existing branch chain, list branches bottom-to-top:

```bash
gh stack init --base main codex/<story>-foundation codex/<story>-api codex/<story>-ui
```

## Inspect and navigate

Use only non-interactive forms:

```bash
gh stack view --json
gh stack checkout <branch-or-pr-number>
gh stack bottom
gh stack down
gh stack up
gh stack top
gh stack trunk
```

Never run plain `gh stack view`, `gh stack checkout`, or `gh stack modify`; they open interactive interfaces. Restructure only after agreeing on the exact new order and reviewing the consequences.

## Submit and update PRs

Treat submission as an external mutation. Run it only when the user asks to push or create/update PRs.

Create draft PRs by default:

```bash
gh stack submit --auto --remote origin
gh stack view --json
```

Use `--open` only when the user asks for ready-for-review PRs:

```bash
gh stack submit --auto --open --remote origin
```

Always pass `--auto`; otherwise submission can open an interactive editor. Report every created or updated PR URL and its bottom-to-top position.

For branches or PRs managed outside local `gh stack` tracking, link them bottom-to-top only when requested:

```bash
gh stack link --base main --remote origin <bottom> <middle> <top>
```

## Change a lower layer

Put a fix in the layer that owns it, then propagate it upward:

```bash
gh stack checkout codex/<story>-foundation
# edit, test, stage exact paths, and commit
gh stack rebase --upstack --remote origin
gh stack push --remote origin
gh stack view --json
```

Push only when the user authorized updating remote branches. If a rebase conflicts, inspect the affected files, resolve only the intended changes, stage them, and run `gh stack rebase --continue`. Use `gh stack rebase --abort` when a safe resolution is unclear. Never use destructive Git recovery commands.

## Synchronize

Run synchronization only with a clean worktree and explicit authorization because it fetches, rebases, force-with-lease pushes, and updates stack state:

```bash
gh stack sync --remote origin
gh stack view --json
```

Add `--prune` only when the user explicitly authorizes deletion of merged local branches.

## Merge or unstack

Treat merging and remote unstacking as destructive external actions. Restate the exact stack or PR boundary immediately before running either operation.

This repository permits squash merges, so merge with:

```bash
gh stack merge <stack-or-pr-number> --yes --squash
```

Verify reviews, checks, draft state, and the requested merge boundary first. Use `gh stack merge`, not `gh pr merge`, for stacked PRs.

Unstack only when the user explicitly asks to dissolve or restructure the stack:

```bash
gh stack unstack <stack-number>
```

Use `gh stack unstack --local` only to remove local tracking while deliberately keeping the GitHub stack intact. Unstacking does not delete the underlying branches or PRs, but still changes stack metadata.

## Completion checks

- Run the relevant tests for each layer before submission and again for the complete top-of-stack state.
- Confirm `gh stack view --json` shows the intended order, bases, PR states, and no required rebases.
- Confirm `git status --short` contains no unexpected files.
- Report branch order, PR links, tests, and any remaining conflicts or review blockers.
