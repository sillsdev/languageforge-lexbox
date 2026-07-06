# Variants in FieldWorks Lite — design & work log

Working document for adding variant support to FwLite (model → sync → UI). Written so any
dev/agent can pick up the work mid-stream. Update the **Status** section as steps land.

## Status

- [x] Exploration (complex-forms blueprint, liblcm variant model, viewer UI)
- [x] Design review with Tim (2026-07-03): per-link model confirmed, naming settled
- [x] Step 1+2 — model, changes, both APIs, sync orchestration + round-trip suite
  (all backend suites green: MiniLcm.Tests, LcmCrdt.Tests, FwDataMiniLcmBridge.Tests,
  FwLiteProjectSync.Tests incl. Sena3 live db regen; the WS-font failure on Windows is
  the known local-only false-fail)
- [x] Step 3 — Viewer UI (fields, per-link type menu, demo seed, i18n, Playwright green)
- [x] Review iterations — see "Review & verification log" below; ALL automated review is
  done and green
- [ ] **Next: human review of [#2410](https://github.com/sillsdev/languageforge-lexbox/pull/2410)**
  (the only open PR; pay special attention to commit `0d03aed28` — it changes shipped
  `AddEntryComponentChange` replay semantics, see decision 7 + rollout notes), then Tim's
  merge sequence: reopen [#2408](https://github.com/sillsdev/languageforge-lexbox/pull/2408)
  and merge the steps one by one.

## Review & verification log

The user-mandated workflow was: per step, polish → open a non-draft PR (so CodeRabbit
reviews) → handle all CodeRabbit feedback → make Devin + CI green → close the PR
(NOT merged) → next iteration opens a cumulative PR. PR closures are workflow, not
abandonment.

- **Iteration 1 — backend, [#2408](https://github.com/sillsdev/languageforge-lexbox/pull/2408)
  (now closed on purpose)**: /polish agent suite; CodeRabbit round (2 accepted: sense-ownership
  guard on variant create + `NotBeEmptyVariantEntryReference`; 3 rejected with posted replies);
  Devin clean; CI 27/27 at `55d4485cd`. [#2409](https://github.com/sillsdev/languageforge-lexbox/pull/2409)
  (stacked UI draft) closed as superseded.
- **Iteration 2 — cumulative backend+UI, [#2410](https://github.com/sillsdev/languageforge-lexbox/pull/2410)
  (OPEN, base develop)**: scoped /polish clean; CodeRabbit round (doc fixes accepted, picker
  cycle-guard suggestion rejected as complex-form-picker precedent); Devin clean ×3; CI green.
- **liblcm deep review (4 parallel agents, every claim cited against `D:\code\liblcm`
  source)** → commit `0d03aed28`, the highest-risk commit in the stack. Found and fixed:
  (a) CRDT rejected sense-targeted variant graphs FLEx accepts → sync would delete legal
  links (decision 7's `AllComponents` no-recurse rule); (b) `AddEntryComponentChange` missed
  mixed cycles liblcm rejects → mid-sync wedge; (c) per-link edits on FLEx shared
  multi-target refs silently edited sibling links → split-before-mutate; plus read
  hardening + `HideMinorEntry` bitfield guard.
- **Merge-hardening (`aff08b562`)**: targeted mutation testing (3/3 mutants killed; the
  surviving would-be mutant exposed the missing `HideMinorEntry` bitfield test, now added);
  `CreateLinks_MatchTheLcmCycleRuleOnRandomGraphs` — a seeded random-graph differential test
  in `VariantTestsBase` asserting both implementations accept/reject exactly per an inline
  port of LCM's `AllComponents` oracle (the FwData side runs real liblcm, so this is
  empirical conformance, not source-reading); maintainer-simulation review → truthful PR
  framing, rollout-note completeness, this log.
- **Verification commands used** (all green at `aff08b562`): FwData + CRDT variant/component
  suites filtered, `FwLiteProjectSync.Tests --filter Sena3` (16/16), viewer
  check/lint/vitest/Playwright.

**Boundary change vs the original plan**: steps 1 and 2 merged — the moment the FwData
bridge reads variants, importing a variant-containing project crashes unless variant types
are imported before entries, so a "model-only" PR can't be green against real projects.
Final split: one backend PR (model through sync) + one UI PR stacked on it.

### Step 2/3 decision addenda

- **Deterministic variant-list order on both sides** (`Variant.VariantOfOrder`/`VariantsOrder`,
  composite-key based, culture-free). ComplexForms sort alphabetically because FieldWorks
  does; variants have no such convention, and guid order keeps snapshot comparisons and
  sync stable across cultures. UI can sort for display later if wanted.
- **`CrdtMiniLcmApi.BulkCreateEntries`** (import fast path) emits `AddVariantChange` with the
  same only-if-other-endpoint-already-created trick as components.
- **Sena3 verified live db/snapshot regenerated**: sena-3 really contains variants
  (*inde*→*ande*, *yenda*→*enda*, custom bilingual "Pronunciation Variant" type), which now
  round-trip — the diff was inspected and is purely additive.
- **Blank-project template regenerated**: new CRDT projects seed the 7 standard FLEx variant
  types (well-known guids).
- **UI**: `Variant of` (picker: entries-and-senses) and `Variants` (picker: only-entries)
  fields, both shown by default in both views; per-link variant types via a "Variant type"
  checkbox submenu on each link badge; new links default to *Unspecified Variant* (FLEx
  behavior); all edits flow through `updateEntry` (before/after diff → EntrySync), so the
  in-memory demo works without dedicated endpoints. `useVariantTypes()` is **eager** —
  the type list must be loaded before the first pick applies the default.
- **helpIds**: `Variant_of_field.htm` verified to exist (fetched); there's no dedicated
  FLEx topic for the Variants back-reference field, so it reuses the same topic.

## The LCM model (authority: liblcm)

A variant relationship is a `LexEntryRef` **owned by the variant (minor) entry**
(`LexEntry.EntryRefsOS`), with:

| LexEntryRef field | Variant usage | FLEx UI label |
|---|---|---|
| `RefType` | `LexEntryRefTags.krtVariant` (= 0; complex form = 1) | — |
| `ComponentLexemesRS` | The main entry **or sense** this is a variant of. Sequence — multiple targets allowed by the model; FLEx UI uses one per ref. | "Variant of" |
| `VariantEntryTypesRS` | Types from `LexDb.VariantEntryTypesOA` (per ref!) | "Variant Type" |
| `HideMinorEntry` | 0 = show as minor entry (default on create) | "Show Minor Entry" (inverted) |
| `Summary` | Per-ref note | "Comment" |
| `PrimaryLexemesRS` | **Unused** for variants (complex-forms only) | — |

Key liblcm business rules (`SIL.LCModel/DomainImpl/OverridesLing_Lex.cs`):

- `LexEntry.MakeVariantOf` creates the ref with `HideMinorEntry = 0` and defaults the type
  to *Unspecified Variant* when none is given.
- A `LexEntryRef` is deleted when its `ComponentLexemesRS` becomes empty (merge/cleanup paths).
- An entry can own **multiple** variant refs (e.g. variant of two entries with different
  types); back-refs (`VariantFormEntryBackRefs`, also on senses) are computed.
- Variant entries **may have senses** (no model restriction). An entry created *as* a variant
  has none; an entry converted to a variant keeps its senses (FLEx course A7: *pagaye* vs
  *placoter*).

The variant-types list is **hierarchical**: *Irregularly Inflected Form* (a `LexEntryInflType`)
has child types *Plural* and *Past*. `LexEntryInflType` extends `LexEntryType` with
`GlossPrepend/GlossAppend/InflFeatsOA/SlotsRC` (not modeled in v1). Well-known GUIDs:
Unspecified `3942addb…`, Dialectal `024b62c9…`, Free `4343b1ef…`, Spelling `0c4663b3…`,
Irregularly Inflected Form `01d4fbc1…`, Plural `a32f1d1c…`, Past `837ebe72…`.

## Design: one link = one LexEntryRef (per-link types)

**FLEx compatibility is the top priority** (Tim, design review): we don't have to model every
field, but what we model must round-trip losslessly. Variant Type / Show Minor Entry /
Comment are **per relationship** in FLEx (they live on the `LexEntryRef`), so the MiniLcm
link carries them — unlike complex forms, which flatten types to entry level.

```csharp
public record Variant : IObjectWithId<Variant>
{
    Guid Id;                    // synthetic in CRDT; unset when read from FwData (sync keys on the composite below)
    Guid VariantEntryId;        // the variant (minor) entry — owns the LexEntryRef in FW
    string? VariantHeadword;    // derived cache, like ComplexFormComponent headwords
    Guid MainEntryId;           // the entry this is a variant of (LCM: ComponentLexemes)
    Guid? MainSenseId;          //   …or a specific sense of it
    string? MainHeadword;       // derived cache
    List<VariantType> Types;    // LexEntryRef.VariantEntryTypesRS
    bool HideMinorEntry;        // LexEntryRef.HideMinorEntry != 0 (LCM polarity kept; UI shows "Show minor entry" inverted)
    RichMultiString Comment;    // LexEntryRef.Summary (FLEx UI labels it "Comment")
    DateTimeOffset? DeletedAt;
}
```

- **`Entry.Variants: List<Variant>`** — links where this entry (or one of its senses) is the
  main. Matches the FLEx "Variants" section on a main entry.
- **`Entry.VariantOf: List<Variant>`** — links where this entry is the variant. Matches the
  FLEx "Variant of" field on a minor entry.
- **`VariantType`**: `Id`, `Name`, `DeletedAt` — same shape as `ComplexFormType`, a
  first-class CRDT object with its own sync.
- Sync key: **composite `(VariantEntryId, MainEntryId, MainSenseId)`**, never `Id`
  (FW-side links are keyed by the ref they live in; ids don't line up across sides —
  same rule as `ComplexFormComponent`).

### Decisions (and the reasoning)

1. **Per-link model, not complex-form parity flattening** (Tim confirmed). One `Variant` =
   one `LexEntryRef` with exactly one target. Reading a multi-target ref (model-legal, rare,
   not creatable in FLEx UI) yields one link per target sharing the ref's types/fields;
   writing back would split into per-link refs. Two refs to the *same* target collapse to
   one link (composite-key dedupe, first ref wins) — documented edge, mirrors complex forms.
2. **Naming leans FLEx-user-facing, not LCM-internal** (Tim: "Component = the main entry" is
   unintuitive, even though liblcm's `ComponentLexemesRS` is exactly that). Hence
   `MainEntryId`/`MainSenseId`, record name `Variant`, entry fields `Variants`/`VariantOf`.
   `Comment` (FLEx label) maps to LCM `Summary`; precedent: MiniLcm `Entry.Note` ↔ LCM
   `LexEntry.Comment`. `HideMinorEntry` keeps LCM's name *and polarity* so bridge code never
   flips signs; the UI can present "Show minor entry".
3. **`HideMinorEntry` + `Comment` are modeled and synced in v1, UI deferred** (Tim). Cheap on
   the link entity, avoids a second migration + sync fanout later, and deleting/recreating a
   link in FWL can never silently drop FLEx data.
4. **Variant types are read *flattened*** (`GetVariantTypes` must be changed — it exists today
   reading only top-level `PossibilitiesOS`): the IIF subtypes (*Plural*, *Past*) are children
   in the list and are what IIF-heavy projects assign. Creating a type missing on the other
   side creates a **plain top-level `LexEntryType`** — standard types exist everywhere
   (well-known GUIDs), so this is rare; hierarchy and `LexEntryInflType` payloads don't
   round-trip in v1.
5. **Link edits are allowed** (unlike complex forms' "delete and recreate"): type add/remove
   are dedicated CRDT changes on the link (`AddVariantTypeChange`/`RemoveVariantTypeChange`,
   `EditChange<Variant>`); `HideMinorEntry`/`Comment` sync via `JsonPatchChange<Variant>`.
   This is what makes concurrent type edits merge instead of last-writer-wins.
6. **No ordering.** Variant lists have no user-meaningful order in FLEx; both directions diff
   as sets. No `IOrderable`, no `SetOrderChange`, no Move API.
7. **Self-references, duplicates AND cycles are rejected; chains are allowed.** (This
   decision reversed mid-implementation: `MakeVariantOf` has no cycle check, but the FwData
   conformance tests proved liblcm rejects circular refs at a lower level —
   `LexEntryRef.ValidateAddObjectInternal` → `LexEntry.AllComponents`, which walks the
   **combined** complex-form + variant component graph. Allowing in the CRDT what FLEx
   rejects would wedge sync, so `AddVariantChange` mirrors the combined-graph walk.)
   The duplicate/cycle guard lives in `AddVariantChange` (soft-delete, sync-tolerant);
   self-reference is additionally rejected by `VariantValidator` in the validation wrapper
   (consistent across both implementations). Chains (a variant of a variant) remain legal,
   as in FLEx. Two liblcm subtleties both change classes mirror exactly (shared walk in
   `ComponentGraph`): `AllComponents` resolves a **sense-targeted** component to its owning
   entry but does **not recurse** into it (so some loops through sense targets are legal in
   FLEx and must stay legal here), while the *added* component's entry is always walked in
   full. `AddEntryComponentChange` was aligned to the same combined walk — its old
   complex-form-only walk accepted mixed cycles that liblcm rejects at sync time (a wedge),
   and recursed past sense targets (over-rejecting vs FLEx). That loosening only affects
   the rare pre-existing sense-cycle shape; pre-variants data is otherwise unaffected
   (no `Variant` rows to walk).
8. **Deleted `VariantType` cleanup**: `Variant.GetReferences()` includes its `Types` ids, and
   `RemoveReference` removes the type from the list (only endpoint ids soft-delete the link).
   Better than the complex-forms quirk where deleted types linger in `Entry.ComplexFormTypes`.
   Like `Entry.ComplexFormTypes`, `Variant.Types` stores denormalized copies (jsonb) that are
   matched by id; a type *rename* updates only the canonical `VariantType` row (the embedded
   `Name` copies go stale — the sync diff's `Replace` is deliberately a no-op, same as
   `ComplexFormTypesDiffApi`). That never surfaces: the viewer and FwData writes resolve
   names/refs via the canonical row, using the embedded copies only for their ids.
9. **Variant entries with no senses are first-class** and must be covered by tests both ways
   (headline behavioral difference; FLEx "Insert Variant" creates sense-less entries).
10. **A variant can target a sense** (`MainSenseId`); on the main side such links still
    surface under the owning entry's `Variants`.
11. **Minor-entry visibility in FWL UI** (step 3): users must be able to see at a glance that
    an entry is a variant, not a first-class entry (badge / "Variant of X" in the editor;
    list styling is a follow-up).
12. **FLEx ref shapes that don't fit the composite-key model** (one link per
    (VariantEntryId, MainEntryId, MainSenseId)):
    - *Shared refs* — one `LexEntryRef` with several targets shares Types/HideMinorEntry/
      Comment across them. Reads fan the ref out into one `Variant` per target; per-link
      **edits split the target out into its own single-component ref first**
      (`FindVariantRefForUpdate`, copying the shared fields) so the sibling links are never
      silently edited. FLEx's own `FindMatchingVariantEntryRef` likewise refuses to reuse
      multi-component refs.
    - *Duplicate refs* — two refs from the same variant entry to the same target collapse to
      one link on read (`DistinctBy` composite key); the extra ref stays in FwData untouched
      (never entering the snapshot, it never diffs) and `DeleteVariant` removes the target
      from all of them.
    - `HideMinorEntry` is an LCM **int reserved as a per-publication bitfield**; we model the
      current 0/non-0 semantics as a bool and only write when the bool flips, so a future
      multi-bit value isn't collapsed to 1.
    - Deleting *Irregularly Inflected Form* cascades to its owned children (Plural/Past) in
      FwData but not in the flat CRDT type list; the next `VariantTypeSync` reconciles
      (direction-dependent), so it self-heals without touching entry data.

### FwData bridge mapping

Read (`FwDataMiniLcmApi.FromLexEntry`):
- `VariantOf` ← each `entry.VariantEntryRefs` ref × each `ComponentLexemesRS` target
  (`ILexEntry`/`ILexSense`), `DistinctBy` composite key; per-link `Types`/`HideMinorEntry`/
  `Comment` from the ref.
- `Variants` ← back-refs: `entry.VariantFormEntryBackRefs` + per-sense back-refs (verify
  exact liblcm property names at impl time), mapped the same way.
- `GetVariantTypes()` ← `LexDb.VariantEntryTypesOA` **flattened** (decision 4).

Write:
- `CreateVariant` → **new** `LexEntryRef` on the variant entry (`RefType = krtVariant`),
  `HideMinorEntry`/`Summary`/types from the link, add the single target. Idempotent when the
  composite already exists.
- `DeleteVariant` → find the ref holding the target; remove the target; delete the ref when
  its components empty (liblcm's own cleanup semantics).
- `AddVariantType`/`RemoveVariantType` (link-locator + type id) → that ref's
  `VariantEntryTypesRS`.
- `SubmitUpdateVariant` (scalars) → proxy over the ref (`UpdateComplexFormTypeProxy` pattern).
- Type CRUD → possibility list (create = plain top-level `LexEntryType`).

### CRDT side

- `Variant` table (`Variants`): FK cascades — Entry×2 (`VariantEntryId`, `MainEntryId`),
  Sense (`MainSenseId`); filtered unique indexes on the composite (sense null / not null),
  mirroring `ComplexFormComponents`; `Types` jsonb; `Comment` jsonb.
- `VariantType` snapshot table (mirror `ComplexFormType`).
- linq2db headword expressions for `VariantHeadword`/`MainHeadword` (mirror the
  `ComplexFormComponent` `IsExpression` mappings in `ConfigureDbOptions`), plus `Finalize`
  sorting for deterministic list order.
- EF migration `AddVariants`.
- Changes (register in `LcmCrdtKernel.ConfigureCrdt` **and** add instances to
  `UseChangesTests.GetAllChanges()` — kernel comment mandates it):
  `AddVariantChange` (create; carries types+scalars; dedupe/cycle/deleted-ref handling
  mirroring `AddEntryComponentChange`),
  `AddVariantTypeChange`/`RemoveVariantTypeChange` (`EditChange<Variant>`),
  `CreateVariantType`, `JsonPatchChange<Variant>`, `JsonPatchChange<VariantType>`,
  `DeleteChange<Variant>`, `DeleteChange<VariantType>`.
  A `SetVariantChange` (mirror of `SetComplexFormComponentChange`) was considered and
  dropped: that change turned out to be test/wire-legacy only for complex forms; variant
  scalars go through `JsonPatchChange<Variant>` and endpoint changes are delete+recreate.

### API surface (both implementations + wrappers)

Read: `GetVariantTypes()`, `GetVariantType(Guid)`.
Write: `CreateVariantType`, `UpdateVariantType` ×2, `DeleteVariantType`,
`CreateVariant(variant)`, `DeleteVariant(variant)`, `UpdateVariant(before, after)`,
`AddVariantType(variant, typeId)`, `RemoveVariantType(variant, typeId)` — link-locator
arguments resolve by composite key on both sides.
Submit (sync, result-less): `SubmitCreateVariant`, `SubmitUpdateVariant(variant, patch)`,
`SubmitUpdateVariantType`.

Fanout sites that must stay in lockstep:
- `CrdtMiniLcmApi`, `FwDataMiniLcmApi` (manual implementations)
- `DryRunMiniLcmApi` (manual write methods; reads auto-forward)
- `MiniLcmApiWriteNormalizationWrapper` (manual write methods)
- `MiniLcmApiValidationWrapper` — **explicit** additions or the new writes silently skip
  validation (BeaKona auto-forwards; the CreateEntry gap #2362 is the cautionary tale)
- `ResumableImportApi` — cache `CreateVariantType` like `CreateComplexFormType` (step 2)
- `MiniLcmApiNotifyWrapper` (FwLiteShared) — notify on variant writes for UI refresh
- `MiniLcmJsInvokable`, `InMemoryDemoApi`, generated TS types (step 3)

### Sync design (step 2)

- `VariantTypeSync` (mirror `ComplexFormTypeSync`); runs in `SyncInternal` next to
  `ComplexFormTypeSync`, before entries.
- `VariantSync` (per-link): types diff via `Add/RemoveVariantType`; `HideMinorEntry`/`Comment`
  via `SubmitUpdateVariant` patch. Used by EntrySync diff Replace and `UpdateVariant`.
- `VariantOf`/`Variants` link diffs join **phase 2** (`SyncComplexFormsAndComponents`, both
  directions — a one-direction wiring won't reconcile deletes). Phase/option names stay
  as-is in these PRs (`…ComplexFormsAndComponents` now covers variants too; renaming is a
  separate no-behavior PR — reviewer advice, keeps the critical diff small).
- `CreateEntryOptions.IncludeEntryReferences` (renamed from `IncludeComplexFormsAndComponents` on review) gates variant links too (doc
  comment updated; entry-to-entry refs need both endpoints to exist).
- `ProjectSnapshot` gains `VariantTypes` — update **`TakeProjectSnapshot()`**
  (`MiniLcmApiExtensions`) and `ProjectSnapshot.Empty`, or it compiles-but-snapshots-empty
  (landmine #1912-class).
- `ProjectImporter`/`MiniLcmImport`: explicit `VariantType` creation loop (same slot as
  complex-form types, before entries) — import is a separate code path from sync.
- `ResumableImportApi`: cache `CreateVariantType`.
- Legacy snapshots need **no** MorphTypes-style patch: CRDT genuinely has no variants yet, so
  the first sync after upgrade imports FwData variants into the CRDT (dedicated upgrade test:
  snapshot without variants + FwData with variants → added to CRDT, nothing deleted from
  FwData).
- **Verified files must be regenerated regardless of Sena3 content**: entry shape changes
  break `ProjectSnapshotSerializationTests.LatestSena3SnapshotRoundTrips` (+ the dated
  `Snapshots/` files) and possibly `LiveSena3Sync`'s `sena-3-live.verified.sqlite` /
  `CrdtChanges == 0` assertion (Sena 3 contains variants → first sync after upgrade
  produces changes). Regenerate + eyeball, don't hand-edit.
- `EntryFakerHelper`/AutoFaker: `CanSyncRandomEntries` will start generating variant links —
  extend `PrepareToCreateEntry` to create referenced variant entries/types (mirror the
  `createComponents` flag) and extend `SyncTests.SyncExclusions` for the derived
  `VariantHeadword`/`MainHeadword` caches.

### Rollout / compatibility notes

- Same story as every previous model addition (MorphTypes, Publications): the lexbox server
  stores change JSON opaquely, but **old FwLite clients cannot deserialize a new change
  type** — they break only when someone actually uses variants in a synced project, and
  auto-update closes the window. No extra gating exists or is added (precedent).
- FwHeadless consumes the same sync libraries; it must be deployed with/before a client
  release that writes variant changes.
- **`AddEntryComponentChange` replay semantics changed** (decision 7): this affects every
  project with complex forms, not just variant users, on ANY deploy of this code. During
  version skew, a historical add shaped like a sense-targeted cycle projects as soft-deleted
  on old clients and live on new ones; the shape is rare (the old guard had to have rejected
  it, so the link was never visible) and auto-update converges the fleet, same as the
  new-change-type story above.
- **Pre-existing FwLite-only (CRDT-only) projects have no variant types**: the 7 standard
  types are seeded via the blank-project template (new projects) or FwData sync (FLEx-backed
  projects) only. On older CRDT-only projects the viewer's type submenu hides itself and new
  links are created untyped — usable but diverging from the FLEx "Unspecified Variant"
  default. A one-time seeding path for existing projects is a follow-up.

### Test plan

Mirror the *named* complex-forms cases (test-auditor sweep), not just categories:

- `MiniLcm.Tests/VariantTestsBase` (subclassed by LcmCrdt.Tests + FwDataMiniLcmBridge.Tests):
  create/delete; headwords update when referenced entries change; replaying returned object
  is idempotent; same-link-again does nothing (null and non-null sense); changing a property
  and creating again creates both; multi-layer cycle guards (1/2/3 levels) + works when a
  would-be-cycle member was deleted; **variant entry without senses; variant-of-a-sense;
  entry that is both a variant and a component; per-link types round-trip; type add/remove;
  HideMinorEntry/Comment round-trip**.
- `LcmCrdt.Tests/Changes/VariantTests` (mirror `ComplexFormTests`: add/remove type, add
  link, delete link, duplicate links are soft-deleted) + `UseChangesTests.GetAllChanges()`
  entries + `ChangeSerializationTests` generator branch + regression-data regen.
- Validator tests (self-reference, empty refs, wrong-direction ids, nested type validation).
- `FwLiteProjectSync.Tests`: EntrySync diff tests; round-trip create/edit/delete both
  directions (two named delete-cascade tests: delete main entry vs delete variant entry);
  link-target deleted between syncs (delete-vs-edit race); sense-targeted link when the
  sense moves entries; new link referencing a new sense; sense-less variant round-trip;
  legacy-snapshot upgrade; flattened-type read + create-missing-type-lands-top-level
  (FwData bridge); Sena3 + verified files regen.
- Playwright (step 3): add/remove a variant link + type via the demo project.
- Hardening additions (post-review, all in the shared bases unless noted): sense-targeted
  cycle trio (loop-through-sense-link allowed / 2-layer sense cycle rejected / sense-of-a-
  dependent-entry rejected); mixed cycle closed by a complex-form add rejected
  (`VariantTestsBase`) and its pure-CFC sense-loop mirror (`ComplexFormComponentTestsBase`);
  `VariantTestsSharedRef` (FwData-only: real 2-component ref — reads fan out, per-link type
  edits split instead of touching the sibling); `UpdateVariant_PreservesAMultiBitHideMinorEntry`
  (FwData-only); `CreateLinks_MatchTheLcmCycleRuleOnRandomGraphs` (seeded differential test
  against the LCM `AllComponents` oracle, both implementations).

## Stacked PR plan

1. **`feat/variants-model`** — models, validators, API surface, CRDT entities + changes +
   migration, FwData bridge read/write, SyncHelpers (`VariantTypeSync`, `VariantSync`,
   EntrySync diffs — they also power `UpdateEntry(before, after)`), conformance tests.
   Green standalone; project-sync orchestration doesn't move variants yet.
2. **`feat/variants-sync`** — `CrdtFwdataProjectSyncService` + `ProjectSnapshot.VariantTypes`
   + `TakeProjectSnapshot`, `ProjectImporter`/`MiniLcmImport`/`ResumableImportApi`,
   round-trip + upgrade tests, verified-file regens (incl. Sena3).
3. **`feat/variants-ui`** — `MiniLcmJsInvokable` + `MiniLcmApiNotifyWrapper` hooks,
   regenerated dotnet-types, entity-config + view-data fields (`variantOf`, `variants` —
   decide `show` defaults: mirror complexForms=true; variant-type picker inside the link
   rows), `VariantOf.svelte`/`Variants.svelte` (reuse `EntryOrSensePicker` +
   `EntryOrSenseItemList`, **both non-orderable**, `pt()` dual labels for FW-Classic view,
   disable self/duplicates but allow cross-type overlap with components), demo data seeded
   with a variant pair + real (not stub) `InMemoryDemoApi` support for the paths the UI
   uses, i18n extraction **with context comments**, Playwright test. The variant *badge*
   for minor entries (decision 11) did NOT ship in this step — see follow-ups; in-editor
   visibility is currently the always-shown "Variant of" field.

## Open questions / follow-ups (not blockers)

- Dictionary preview (`DictionaryEntry.svelte`) shows neither complex forms nor variants;
  "variant of X" there is a follow-up.
- `LexEntryInflType` extras (GlossPrepend/Append, InflFeats, Slots), type hierarchy
  round-trip — future work.
- Minor-entry badge in the editor (decision 11) and entry-list styling of minor entries —
  follow-up; today only the "Variant of" field signals variant status.
- Seed the 7 standard variant types into pre-existing CRDT-only projects (see rollout
  notes) — follow-up.
- FLEx "Insert Variant" convenience (create variant entry + link in one step) in the
  new-entry dialog — follow-up UX; v1 links existing entries.
- `HideMinorEntry` is modeled as bool; LCM docs hint the int may become per-publication bit
  flags someday — revisit if FLEx ever uses values other than 0/1.
