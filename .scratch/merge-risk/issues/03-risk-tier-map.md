# Risk tier map for lexbox

Type: grilling
Status: resolved
Blocked by:

## Question

Which paths and change shapes in lexbox are high, medium, low, or no risk, and in what checked-in format does the skill read that map?

## Answer

No tier map and no strict rules. Risk is judged by the agent from the change itself, not from which files it touches. What matters is consequences that are painful to repair: data corruption or loss, breaking app updates (old clients, serialized data, sync/protocol compatibility), and long-term pain a new release can't easily undo. Everything else is low stakes, since a follow-up release fixes it.

- Paths are at most a hint for where to look harder; they set no floor.
- Diff-shape signals (serialized shapes, public contracts, deleted logic, concurrency, schema) are prompts for the agent's judgment, not rules, because rules give false positives.
- Critical code can still get a "merge" verdict when the change itself is low-risk or tests clearly cover the edge cases. The report should note nuances like that only when they truly help the developer.
- Supersedes the prior-art idea of a path-based floor that evidence can only raise.
