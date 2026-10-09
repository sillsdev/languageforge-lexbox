# Coding standards (judgement calls)

Rules a linter can't check, distilled from review corrections. Mechanical style lives in `.editorconfig`, eslint and prettier; area rules live in each folder's `AGENTS.md`.

## Data model and migrations

- A persisted field keeps its name and meaning until a migration path exists for every client that already wrote it. Clients that update without the migration see the field empty.
- Prefer leaving an awkward field in place over a heal step that repairs data after the fact.
- Keep union types and polymorphic models; the next .NET release is the planned path to better support, not a reason to flatten them now.

## Layering

- Interfaces live in `LexCore`; implementations live in `LexBoxApi`.
- `[JSInvokable]` goes on the interface method and on the concrete class method.

## Design pressure

- Prefer a config flag or setting over a new abstraction.
- Take the simplest step that solves the problem in front of you; add structure when a second caller needs it.
- Dev-only state lives in dev settings, not on shared production objects such as the project context.

## Serialization

- `IChange` JSON options come from `MakeLcmCrdtExternalJsonOptions()`, never a hand-built `JsonSerializerOptions`.

## UI

- Gate viewer UI on the API's `IMiniLcmFeatures` flags (`features.comments`, `features.write`, ...), so it renders only where the feature is supported.
- UI copy uses plain ASCII punctuation: commas, colons and `...` written as three dots, so translators and fonts see the same characters.

## Tests

- Write the failing regression test before the fix.
- Run the tests that cover the change, filtered; whole suites are CI's job.

## Candidate lints (follow-up, not yet rules)

- Em dash or ellipsis characters in lingui msgids.
- VSTHRD analyzer warnings in new code.
