# Variants in FieldWorks Lite — design & work log

Working document for adding variant support to FwLite (model → sync → UI). Written so any
dev/agent can pick up the work mid-stream. Update the **Status** section as steps land.

## Status

- [x] Exploration (complex-forms blueprint, liblcm variant model, viewer UI)
- [x] Design review with Tim (2026-07-03): per-link model confirmed, naming settled
- [ ] Step 1 — MiniLcm model, CRDT change types, both API implementations, conformance tests
- [ ] Step 2 — FwData↔CRDT project sync (orchestration, snapshot, round-trip tests)
- [ ] Step 3 — Viewer UI (fields, picker, i18n, Playwright)

Branches are stacked: `feat/variants-model` ← `feat/variants-sync` ← `feat/variants-ui`.

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
    RichMultiString? Comment;   // LexEntryRef.Summary (FLEx UI labels it "Comment") — check actual MultiString kind at impl time
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
7. **Cycle/duplicate guard mirrors `AddEntryComponentChange`** — lives in the *change class*
   (soft-deletes instead of throwing, because sync must tolerate incoming duplicates);
   validator only rejects self-reference cheaply. Don't duplicate the BFS in validators.
8. **Deleted `VariantType` cleanup**: `Variant.GetReferences()` includes its `Types` ids, and
   `RemoveReference` removes the type from the list (only endpoint ids soft-delete the link).
   Better than the complex-forms quirk where deleted types linger in `Entry.ComplexFormTypes`.
9. **Variant entries with no senses are first-class** and must be covered by tests both ways
   (headline behavioral difference; FLEx "Insert Variant" creates sense-less entries).
10. **A variant can target a sense** (`MainSenseId`); on the main side such links still
    surface under the owning entry's `Variants`.
11. **Minor-entry visibility in FWL UI** (step 3): users must be able to see at a glance that
    an entry is a variant, not a first-class entry (badge / "Variant of X" in the editor;
    list styling is a follow-up).

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
  mirroring `AddEntryComponentChange`), `SetVariantChange` (mirror
  `SetComplexFormComponentChange`; needs a hand-written generator branch in
  `ChangeSerializationTests.GeneratedChangesForType` — protected ctor),
  `AddVariantTypeChange`/`RemoveVariantTypeChange` (`EditChange<Variant>`),
  `CreateVariantType`, `JsonPatchChange<Variant>`, `JsonPatchChange<VariantType>`,
  `DeleteChange<Variant>`, `DeleteChange<VariantType>`.
  Check `ConfigRegistrationTests.AllChangesAreRegistered` exclusion list (it force-crosses
  generic changes with all object types).

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
- `CreateEntryOptions.IncludeComplexFormsAndComponents` also gates variant links (doc
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
   disable self/duplicates but allow cross-type overlap with components), variant badge for
   minor entries (decision 11), demo data seeded with a variant pair + real (not stub)
   `InMemoryDemoApi` support for the paths the UI uses, i18n extraction **with context
   comments**, Playwright test.

## Open questions / follow-ups (not blockers)

- Dictionary preview (`DictionaryEntry.svelte`) shows neither complex forms nor variants;
  "variant of X" there is a follow-up.
- `LexEntryInflType` extras (GlossPrepend/Append, InflFeats, Slots), type hierarchy
  round-trip — future work.
- Entry-list styling of minor entries (beyond the editor badge) — follow-up.
- FLEx "Insert Variant" convenience (create variant entry + link in one step) in the
  new-entry dialog — follow-up UX; v1 links existing entries.
- `HideMinorEntry` is modeled as bool; LCM docs hint the int may become per-publication bit
  flags someday — revisit if FLEx ever uses values other than 0/1.
