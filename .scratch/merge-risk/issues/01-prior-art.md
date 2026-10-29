# Prior art for PR risk-vs-coverage triage

Type: research
Status: resolved
Blocked by:

## Question

What do existing public skills, tools, and write-ups do to triage AI-written PRs by risk against test coverage (e.g. mattpocock/skills `code-review`, Anthropic's code-review plugin, CodeRabbit/Devin risk summaries, "risk-based review" practice)? Which ideas for scoring, verdict scales, and auto-fix boundaries should we copy or avoid?

## Answer

Findings: [research/01-prior-art.md](../research/01-prior-art.md). No public tool weighs risk against coverage into a merge verdict, so that part is ours to design. Copy: path rules set a risk floor that evidence can only raise; policy read from the base branch; stale or missing CI/bot evidence blocks; risk and test cover shown as two columns, never one score; one-way vs two-way door as a tiebreaker; check that tests can actually fail. Avoid: auto-approval, raw coverage percent as a gate, vague "spot-check" advice without naming hunks. Several sources are vendor blogs and a few claims are unverified (marked in the file).
