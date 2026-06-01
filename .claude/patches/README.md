# Patches

Patch files that need to be applied **outside** the lexbox repo (most
commonly: in a submodule's upstream repo) and that can't be applied
from inside lexbox itself.

## harmony-AGENTS.md.patch

Adds an `AGENTS.md` to the `sillsdev/harmony` repo (the CRDT substrate
checked into lexbox as the `backend/harmony` submodule). The
`harmony-sentinel` polish agent (in `.claude/agents/`) reads this file
as its authoritative source of substrate-author standards.

### How to apply

```bash
# from your harmony checkout (NOT lexbox)
cd /path/to/sillsdev-harmony   # wherever you cloned sillsdev/harmony
git checkout -b add-agents-md
git apply /path/to/lexbox/.claude/patches/harmony-AGENTS.md.patch
git add AGENTS.md
git commit -m "Add AGENTS.md for substrate-author standards"
git push -u origin add-agents-md
# then open a PR in sillsdev/harmony
```

Or apply via the submodule from inside lexbox (creates uncommitted
work in the submodule that you then push from the submodule's own
git):

```bash
cd backend/harmony
git apply ../../.claude/patches/harmony-AGENTS.md.patch
# inspect, then commit and push from inside the submodule
```

### Why a patch, not a PR

Claude Code's GitHub MCP in this environment is restricted to
`sillsdev/languageforge-lexbox` only — it can't open PRs in
`sillsdev/harmony`. The patch is the simplest way to hand off the
content while keeping the human in the loop on the actual harmony
commit.

### After it's merged in harmony

1. Bump the lexbox submodule pointer to a SHA that includes
   `AGENTS.md`.
2. The `harmony-sentinel` agent's "fallback to reduced check" branch
   becomes unreachable; full review runs against the canonical
   standards.
