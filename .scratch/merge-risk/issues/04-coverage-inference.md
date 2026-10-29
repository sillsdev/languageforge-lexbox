# How to infer test coverage for a change

Type: grilling
Status: resolved
Blocked by:

## Question

For a changed hunk, how does the skill decide whether tests cover it (test project conventions, conformance tests in `MiniLcm.Tests`, vitest/Playwright in the viewer, CI-only integration tests), and what counts as "weak" coverage?

## Answer

The agent judges coverage per change by asking "if this went wrong, would a test fail?", and only for changes that are actually scary: a failure is both plausible and painful to repair. Unlikely or low-damage changes get no test request. It runs only the tests the PR adds or changes; CI-only integration tests count only if CI ran them on the current commit. "Weak" means no test would catch the painful outcome.
