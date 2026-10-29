# Relationship to /polish

Type: grilling
Status: resolved
Blocked by: 02

## Question

Given how `/polish` actually performs, should the new skill call it, reuse only its agents and dispatch matrix, absorb and replace it, or ignore it?

## Answer

A separate skill. It shares `_shared/apply-rules.md` and the targeted test-command table (moved from `/polish` Phase 5 into `_shared/` so both read it), and reuses test-auditor, ripple-detector and the domain sentinels, picked by what the change does. It does not use the path-based dispatch matrix or `/polish`'s severity scale. `/polish` stays as is; its two bugs (dead `||` fallback, shallow-clone merge-base) are a separate fix outside this map.
