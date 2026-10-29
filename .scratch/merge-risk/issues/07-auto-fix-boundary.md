# Which to-dos the skill does itself

Type: grilling
Status: resolved
Blocked by: 05, 06

## Question

Which to-dos is the skill allowed to do without asking (add a fast test, small fix, regenerate files), which does it only propose, and does it commit or leave changes in the working tree?

## Answer

The skill does the code and test parts of X itself, including writing tests without asking (overriding apply-rules' "ask before tests" for this skill). It still asks first for architecture moves and sync-flow changes (apply-rules list) and never does ops steps (secrets, k8s, deploys); those stay in the report. It may check out the PR branch itself (a worktree when the current tree has uncommitted work) and commit its changes; it doesn't push. After X it runs the new tests, re-checks only the affected outcomes once, and updates the verdict.
