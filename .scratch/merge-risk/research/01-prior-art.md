# Prior art: triaging AI-written PRs by risk vs test coverage

Scope note: I found no public tool that weighs per-change risk against test coverage and returns a merge verdict. Risk tiers by path and change type are common. Coverage as a merge input is only proposed, never documented as working. Most sources are vendor docs or blogs; treat claims as reports, not evidence.

## Ideas to copy

- **Deterministic path rules set a floor; nothing can lower it.** The highest-risk path in the diff wins. The authoring agent's evidence can only raise risk. Why it fits: our CRDT sync and FwHeadless paths get a fixed "needs a real review" floor that a green CI or a clean bot review cannot override. [TradersPost risk-proportional review](https://blog.traderspost.io/article/risk-proportional-review-for-ai-agents)
- **Read the risk policy from the default branch, not the PR.** A PR cannot edit its own scoring rules. Same for Claude's `REVIEW.md`. Why: the skill's path list should live in the repo's base branch (or the skill itself), never be read from the diff under review. [Unblocked risk assessment](https://docs.getunblocked.com/code-review/risk-assessment), [Claude Code Review](https://code.claude.com/docs/en/code-review)
- **Risk policy as a short list of named policies, each with a level and plain-language criteria; highest match wins.** Why: easy to write for us ("changes to commit/snapshot handling", "EF migrations", "FwData bridge"), and easy to read in the verdict. [Unblocked](https://docs.getunblocked.com/code-review/risk-assessment)
- **Small-footprint gate for the lowest tier.** Width 1 review only if the change is about 120 lines and 10 files or fewer (or 80/6 with no deletions). Why: gives v1 a concrete size cap before "merge" is even possible. [TradersPost](https://blog.traderspost.io/article/risk-proportional-review-for-ai-agents)
- **Stale evidence is ignored; missing evidence blocks.** CI or bot findings tied to an old head SHA do not count, and a missing or failed signal blocks the low-risk verdict. Why: avoids merging on a green check from before the last push. [TradersPost](https://blog.traderspost.io/article/risk-proportional-review-for-ai-agents)
- **Hard stops for human review:** unresolved blocking findings on the current head, CI still red after several author pushes, reviewer reopening a concern. Why: maps straight onto our Devin/CodeRabbit loop. [TradersPost](https://blog.traderspost.io/article/risk-proportional-review-for-ai-agents)
- **Verify findings before reporting them.** Claude's review runs several agents, then a verification step that checks each finding against the code and drops false positives; the older plugin used a 0-100 confidence score with an 80 cutoff (unverified, see Sources). Why: our skill will read bot findings, and should re-check each one against the code instead of trusting or dismissing it. [Claude Code Review](https://code.claude.com/docs/en/code-review)
- **Separate severity from "pre-existing".** Claude marks findings Important, Nit, or Pre-existing (bug not introduced by this PR). Why: stops old bugs from blocking an unrelated PR. [severity levels](https://code.claude.com/docs/en/code-review-pull-requests.md)
- **Machine-readable result line.** Claude writes severity counts as JSON in an HTML comment on the last line of the check run, so scripts can gate on it. And "missing line = no verdict, not zero findings". Why: our skill can read bot output safely and treat a missing review as unknown. [read the result from a script](https://code.claude.com/docs/en/code-review-pull-requests.md)
- **Two separate axes, never merged into one score.** mattpocock's code-review runs Standards and Spec as parallel sub-agents and reports them side by side, one worst-issue line per axis. Why: "risk" and "test cover" should stay two visible columns, so a high-risk, well-tested change reads differently from a high-risk, untested one. [mattpocock code-review](https://github.com/mattpocock/skills) (local clone)
- **"Merge Danger" section: one-way or two-way door, plus one-word blast radius.** From mattpocock's pr skill. Why: cheap to roll back vs destructive is a good tiebreaker, and for us a sync change that writes bad commits is a one-way door. [mattpocock pr](https://github.com/mattpocock/skills) (local clone)
- **Evidence tiers: screenshots and execution output beat prose.** pr skill ranks "show the exact test that now fails and passes". Why: for v1 inferred coverage, the best inference is a named test that exercises the changed behavior. [mattpocock pr](https://github.com/mattpocock/skills)
- **TDD anti-patterns as a test-quality check.** Tautological tests (expected value recomputed the way the code does), implementation-coupled tests, tests that pass by construction. Why: "has a test" is not "is covered"; the skill should spot tests that cannot fail. [mattpocock tdd](https://github.com/mattpocock/skills)
- **Flag types besides bugs.** Devin Review has Bug (Severe/Non-severe), Flag (Investigate / Informational), and security findings (Critical/Warning). Why: "Investigate" flags are exactly the spot-check hunks case. [Devin Review docs](https://docs.devin.ai/work-with-devin/devin-review)
- **Risk signals list:** size over about 1000 lines, files with no tests, schema changes, blast radius and reversibility, security or data-adjacent code. Why: a ready checklist; "touched file has no test file" is cheap to infer. [Cortex risk-based review](https://www.cortex.io/post/risk-based-code-review)
- **Passing tests are the safety net for mechanical changes** (renames, behavior-preserving refactors), so no line-by-line read. Why: this is the main case where "merge" is defensible for us. [Cortex](https://www.cortex.io/post/risk-based-code-review)
- **Repo-local guidance files** (`REVIEW.md`, `AGENTS.md`) name critical areas and paths to ignore. Why: we already have AGENTS.md and CODING_STANDARDS.md; the skill should read them for critical areas instead of hardcoding. [Devin Review docs](https://docs.devin.ai/work-with-devin/devin-review)

## Ideas to avoid

- **Auto-approve on a low score from the same AI that wrote or reviewed the code.** CodeRabbit's own write-up warns about self-review (author agent checks its own work) and bots whose verdict is accepted with no pushback. Anthropic's review and Cortex's bot also never approve; a human signs off. Why: our verdict must be advice to Tim, not an approval. [CodeRabbit](https://coderabbit.ai/blog/we-watched-developers-approve-bugs-in-30-seconds) (vendor marketing), [Claude Code Review](https://code.claude.com/docs/en/code-review), [Cortex](https://www.cortex.io/post/risk-based-code-review)
- **Trusting a quick human or bot skim.** In CodeRabbit's conference game, developers often approved bugs under 30 seconds of pressure, and obviousness didn't predict catches. No rates given. Why: "spot-check these hunks" must name exact hunks and say what to look for. [CodeRabbit](https://coderabbit.ai/blog/we-watched-developers-approve-bugs-in-30-seconds)
- **Raw coverage percent as the gate.** Cortex notes tests have limits (a library upgrade halved performance and only a week-long load test caught it). Why: for CRDT code, line coverage says little about replay order or merge conflicts. [Cortex](https://www.cortex.io/post/risk-based-code-review)
- **An estimated "review effort in minutes" score.** CodeRabbit's walkthrough includes one (found only via a secondary source, not verified). Why: it measures reading cost, not merge safety.
- **Letting an agent lower the risk tier because "it looks fine".** [TradersPost](https://blog.traderspost.io/article/risk-proportional-review-for-ai-agents). Why: the same reason as the floor rule above.
- **"All bots" auto-response.** Devin docs warn that letting it act on all bot comments can loop forever. Why: if our skill auto-fixes bot findings, cap rounds and limit which bots it answers. [Devin autofix](https://docs.devin.ai/use-cases/gallery/devin-review-autofix)
- **Reviewers who stop looking at AI PRs.** A cited 2026 study found approval rates rising and inline comments dropping 22% as reviewers saw more AI-authored PRs (second-hand via Cortex, not checked). Why: the skill should not make "merge" feel routine; show the reason each time. [Cortex](https://www.cortex.io/post/risk-based-code-review)
- **Defaulting to correctness-only review.** Claude's managed review skips missing test coverage by default. Why: our skill has to look at test gaps itself. [Claude Code Review](https://code.claude.com/docs/en/code-review)
- **Review without running code.** Claude's managed review reads the diff and surrounding code and does not build or run it. Why: ok for a bot, but our verdict should rest on CI results or a named fast test run. [Claude Code Review](https://code.claude.com/docs/en/code-review)

## Verdict scales seen

- Unblocked: `lowest / low / medium / high / highest`; auto-approve only at low or lowest with no review issues. Label `risk: <level>`.
- TradersPost: Low / Medium / High; High always needs a human; Low is a candidate for no mandatory human review.
- Cortex: green (no review) / yellow (check in) / red (stop and review); bot gives a human-effort level, minimal to high.
- Claude Code Review: per finding Important / Nit / Pre-existing; PR level never approve or block, check run is neutral.
- Devin Review: Severe / Non-severe bugs; Investigate / Informational flags; security Critical / Warning.
- Cortex remediation: flagged PRs must be split, get tests added, or explain a migration before the label clears (a "do X then merge" shape).
- No public source I found uses "spot-check these hunks" as a formal outcome. Devin's Investigate flags are closest.

## Auto-fix boundaries seen

- Devin Auto-Fix pushes fix commits to the PR branch for bugs it found; docs state no limits on size or file type. Safeguards: admin-only enablement, "Selected only" bot allowlist, per-finding human review. [Devin autofix](https://docs.devin.ai/use-cases/gallery/devin-review-autofix)
- Claude Code Review is read-only: no commits, only comments, check run, and resolving its own earlier threads. [docs](https://code.claude.com/docs/en/code-review)
- TradersPost releaser never merges, force-pushes, or requests review; bot approval is advisory and does not satisfy code owners; unattended scope is docs, chores, dev tooling, test-only changes. [TradersPost](https://blog.traderspost.io/article/risk-proportional-review-for-ai-agents)
- Cortex bot can request changes but never approves. [Cortex](https://www.cortex.io/post/risk-based-code-review)
- mattpocock `implement`: runs `tdd` at pre-agreed seams, then `code-review`, then commits to the current branch. `tdd` says write tests only at seams confirmed with the user. Nothing about fixes inside review itself; `code-review` only reports. [mattpocock skills](https://github.com/mattpocock/skills)
- Gap: no source defines "clear small to-do" (fast test, small fix). We will have to set our own line, e.g. test-only or non-critical-path edits, never in the high-risk floor paths.

## Sources

Read directly:
- https://code.claude.com/docs/en/code-review and https://code.claude.com/docs/en/code-review-pull-requests.md (Anthropic managed Code Review; first-party)
- https://docs.getunblocked.com/code-review/risk-assessment (vendor docs)
- https://blog.traderspost.io/article/risk-proportional-review-for-ai-agents (practitioner write-up; small trading-software team, single source)
- https://www.cortex.io/post/risk-based-code-review (vendor blog; cites SmartBear/Cisco and a 2026 study I did not open)
- https://docs.devin.ai/work-with-devin/devin-review and https://docs.devin.ai/use-cases/gallery/devin-review-autofix (vendor docs)
- https://coderabbit.ai/blog/we-watched-developers-approve-bugs-in-30-seconds (vendor marketing)
- mattpocock/skills local clone: skills/engineering/{code-review,implement,tdd,pr,research}/SKILL.md

Not verified:
- The open-source `code-review` plugin in anthropics/claude-code (parallel agents, 0-100 confidence, drop below 80): raw GitHub URL returned 404 and the repo was not attached to this session, so this comes from third-party mirrors that disagree on agent count. Re-check before relying on it.
- CodeRabbit "estimated review effort" and Graphite guidance: secondary search snippets only; docs.coderabbit.ai summary page 404'd, Graphite guide not opened. Graphite had no risk-tier framework I could confirm.
- Alpesh Nakrani "risk classifying generated diffs" page (found in search, 404 on fetch), and MergeShield (trust-based auto-merge, vendor claim only): not read.
- The SmartBear "200-400 lines" figure is quoted second-hand.
