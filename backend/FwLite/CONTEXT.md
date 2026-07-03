# FwLite

## Collation

How strings in a writing system are compared when ordering dictionary content. Imported from FLEx per writing system; stored on `WritingSystem` without LDML at runtime.

**Collation**:
The rules that govern string comparison for a writing system (ICU tailoring or .NET locale alias). Distinct from list position and from the user action of sorting entries.
_Avoid_: Sort rules (when meaning collation specifically), custom sorting (as a model term)

**Writing system order**:
Where a writing system appears in the project's vernacular or analysis list (`WritingSystem.Order`). Not how strings alphabetize.
_Avoid_: Confusing with collation

**Entry sort**:
The user-facing action of ordering the entry list (e.g. by headword in a chosen writing system). Uses the selected writing system's collation for headword comparison.
_Avoid_: Collation (when meaning the user action)

**Collation scope (v1)**:
Collation governs headword entry sort (`SortField.Headword`) and the alphabetical tie-break when search results fall through to headword order (`SortField.SearchRelevance`). It does not govern search matching (whether a query matches text) or FTS ranking.
_Avoid_: Applying collation to `ContainsDiacriticMatch`, prefix/contains predicates, or FTS rank

**Imported collation**:
Collation metadata synced from FLEx onto `WritingSystem`. Two optional fields: compiled ICU rules, or a .NET locale alias for "same as another language." No LDML at FwLite runtime.
_Avoid_: Storing LDML, simple-rule source text, or import metadata

**Legacy collation fallback**:
When neither field is set, headword comparison uses the pre-existing FwLite behavior (`CultureInfo` from `WsId`, case-insensitive with lowercase-before-uppercase tie-break). FLEx "Default Ordering" is imported as this fallback too — FwLite does not replicate FLEx's empty-rule ICU default until we choose to.
_Avoid_: Treating `null` and `""` as different states for `IcuCollationRules`

**Collation compare (imported)**:
When `IcuCollationRules` or `SystemCollationLocale` is set, use ICU4N (`RuleBasedCollator` / locale `Collator`) `Compare` with no legacy case tie-break layered on top. Matches FLEx for custom and other-language modes. FwData/FLEx bridge code still uses icu.net separately.
_Avoid_: Applying case-insensitive or lowercase-first logic on top of imported ICU4N collation

**Collation import (from FLEx)**:
Populated when mapping `CoreWritingSystemDefinition` → `WritingSystem` in the FwData bridge. Custom modes: store non-empty compiled ICU rules only. Other-language: store .NET locale tag only. Default ordering: leave both fields null (legacy fallback). Import-only — no write-back to fwdata LDML.
_Avoid_: Parsing LDML in FwLite; persisting empty `IcuCollationRules` for default ordering

**Collation write-back**:
Explicitly a no-op. The fwdata `UpdateWritingSystemProxy` overrides collation fields and does not write them to LDML.
_Avoid_: Assuming bidirectional collation sync

## Commenting

Collaborative discussion attached to dictionary data (entries, senses, example sentences). Comments sync via Harmony; read status is local-only per device (not synced, not per Lexbox user account).

## Language

**Comment thread**:
A conversation anchored to one dictionary object. Has its own identity separate from the object it discusses. Can be open or closed.
_Avoid_: Thread (alone, ambiguous), conversation

**User comment**:
A single message within a comment thread, authored by a project participant.
_Avoid_: Comment (alone, when the type name matters), note

**Subject**:
The dictionary object a comment thread is about — an entry, sense, or example sentence.
_Avoid_: Entity, parent, anchor, target

**SubjectId**:
The GUID of the subject object.
_Avoid_: EntityId, ParentId

**SubjectType**:
Which kind of dictionary object the subject is: Entry, Sense, or ExampleSentence.
_Avoid_: ParentEntityType, EntityType

**Open thread**:
A thread that accepts new replies (enforced at the API layer).
_Avoid_: Active, unresolved

**Closed thread**:
A thread whose `Status` is `Closed`. Local replies are rejected by the API. Synced replies remain visible and unread; the thread does not auto-reopen — users reopen explicitly if they want to continue.
_Avoid_: Resolved (reserved — epic #1765 mentions "resolving" but close ≠ resolve in v1), optimistic close, derived close state

**Synced reply on closed thread**:
A comment that arrives via sync while the thread is closed. It remains visible, counts as unread, and does not change thread status.

**Subject cardinality**:
A subject may have many comment threads. There is no uniqueness constraint on `(SubjectId, SubjectType)`.
_Avoid_: One thread per entry/sense/example (not a v1 rule)

**Subject deletion**:
When a subject is soft-deleted, its comment threads are soft-deleted via Harmony reference cascade, and their comments are soft-deleted in turn. Threads do not survive as orphans.
_Future_: If subject merge were supported, comments would move with the subject; out of scope for v1.
_Avoid_: Orphan threads on deleted subjects

**PreviousCommentId**:
The comment the author had seen when writing this reply. Used to detect parallel forks under sync — not the sole source of display order.
_Avoid_: Parent comment, reply-to (when meaning the fork chain)

**Fork warning**:
Shown when a comment's `PreviousCommentId` does not match the comment immediately before it in `CreatedAt` display order. Indicates the author likely did not see intervening comment(s).
_Avoid_: Conflict indicator, merge warning

**Deleted-predecessor notice**:
When `PreviousCommentId` points to a soft-deleted comment, show an informational notice — not a fork warning.

**CRDT-only (v1)**:
Comments exist only in the Harmony/CRDT store. They are not mirrored to FwData or round-tripped through `FwLiteProjectSync`. If FieldWorks gains native comment support later, syncing comments in becomes a separate effort.
_Avoid_: Assuming FwData parity, LF import as sync

**Read status**:
Per-device, non-synced seen-comment records keyed by `CommentId`. Account switching on the same OS user is out of scope; project data is already per OS user.
_Avoid_: Per-user read sync, cross-device read continuity

**Open thread (read side effect)**:
Opening a thread marks all comments currently in that thread as read on this device.
_Avoid_: Auto-read on sync, auto-read on close

**In-thread arrival (nice to have)**:
A comment that arrives via sync while the thread is already open may stay unread until the user explicitly acknowledges it (e.g. popup). Not required for v1.

**Delete comment**:
Authors may delete their own comments. Managers may delete any comment.
_Avoid_: Editors deleting others' comments

**Delete thread**:
Managers may delete a thread (cascades to comments via Harmony).
_Avoid_: Any editor deleting threads

**Commenter role**:
For commenting permissions, Commenter is equivalent to Editor — can create threads, reply, and edit/delete own comments. Does not grant manager moderation powers.

**Edited comment**:
When `UpdatedAt > CreatedAt`, show an edited badge. Hover reveals the updated date/time. Prior text versions are not shown in v1; Harmony commit history can power a history view later.
_Avoid_: Inline diff, version chain on the model (v1)

**Subject scope (v1)**:
Comment threads attach to whole objects only — Entry, Sense, or ExampleSentence. Field-level anchoring (e.g. a specific gloss or writing system) is out of scope; reviewers can name the field in prose.
_Future_: Field-level anchoring if LF import or workflow requires it.
_Avoid_: Field comments, property-level threads (v1)

**Comment text**:
Plain text only in v1 (no Markdown or rich formatting). Display as-is; URL auto-linking in the UI is optional. Maximum **2,000 characters** per comment; empty/whitespace rejected at API. Limit may be raised later.
_Future_: Markdown subset if formatting is needed.
_Avoid_: Rich text, HTML, validating length in change classes (API/UI only)

**Activity view (deferred)**:
Presentation (per-comment, per-thread, or per-subject list; sort by authored date vs sync date) is a UI concern. The v1 model supports all three via queries over `CommentThread`, `UserComment`, and local read status — no extra Harmony fields required. Initial UI will likely use the simplest shape (flat unread comments by `CreatedAt`).
_Future_: Sync-date sort may need a local per-device `SyncedAt` on receipt, similar to read status — not in Harmony model.
