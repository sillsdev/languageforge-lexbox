# Raw reviewer results (retro /polish run, dispatched from root)

## 2618 i18n-completeness
- ✨ only praise: new string "Sort writing system" extracted and propagated to all locales. No findings.

## 2618 viewer-watcher (4 important, 4 nit, 2 praise)
- ⚠️ sort object identity may reload list; ⚠️ SortConfig union loosened (invalid field/dir typechecks); ⚠️ backend ws-sort support unverified; ⚠️ stale ws id -> blank pill + wrong sort
- 💭 layered fallbacks; 💭 rows show sortedHeadword but other places show default headword (= approval-review "propagate ws elsewhere?"); 💭 no translator context; 💭 restating comments
- ✨ praised `sortedHeadword` name as matching behavior (human reviewer renamed it to bestHeadword)

## 2618 rename-detector (did a general review, not its specialty)
- ⚠️ stale ws id; ⚠️ ws not persisted; ⚠️ preview mode ignores sort ws; ⚠️ SortConfig union loosened; ⚠️ silent fallback to other ws makes rows look out of order, untested (= human "doubt the fallback")
- 💭 aria-label; 💭 test checks display not order; 💭 history comments; 💭 empty msgstr (noise)

## 2618 pr-narrative (also general review)
- ⚠️ ws pill on relevance sort + not persisted; ⚠️ stale ws id; ⚠️ preview mode mismatch; ⚠️ no PR body
- 💭 test checks display not order; 💭 comments restate code; 💭 rationale repeated 4x; 💭 sortOptions narrowing

## 2618 diff-hygiene
- 💭 x3 change-relative comments ("historical behavior", "how it works today"). Nothing else.

## 2514 diff-hygiene
- 💭 one vague comment. Nothing else.

## 2618 test-auditor (best on this PR)
- ⚠️ no test checks sort ORDER; would pass if ws never reached the query; gives a concrete cheap demo-api test
- ⚠️ one assertion can never fail (passes on develop too)
- ⚠️ sortedHeadword fallback untested; names the existing vitest file
- 💭 single-ws hide rule untested; 💭 trigger label not asserted; ✨ good Playwright test

## 2618 intent-check
- ⚠️ no linked issue; ⚠️ no test checks order
- 💭 display change beyond title, preview mode skipped (= approval "propagate ws?")
- 💭 `sortedHeadword` vs `headword(entry, ws)` names easy to mix up (≈ human rename comment)
- 💭 SortConfig loosened; 💭 ws affects relevance sort

## 2618 ripple-detector (strongest on the human "fallback" doubt)
- ⚠️ display falls back to other ws but backend sort key has no fallback -> rows look unsorted; cites EntryQueryHelpers.cs:10 and demo api (= human "doubt the fallback", with evidence)
- 💭 ws now also changes SearchRelevance ranking (backend Sorting.cs)
- checked and cleared: SortConfig consumers, watch picks up new object (contradicts viewer-watcher's reload worry)

## 2514 pr-narrative (general review)
- ⚠️ ~50 record strings changed with no test (= pre-review state had no tests; tests were added after review)
- ⚠️ removing defaults is breaking; generated wrappers unverified
- ⚠️ SubmitMoveComplexFormComponent skips normalization (unverified "check whether")
- 💭 RichString.ToString global change; 💭 Summarize format change, check consumers (near the multi-line issue but did not name it); 💭 async wrappers; 💭 no PR body
- ✨ MovePicture param rename fixes copy-paste bug

## 2514 rename-detector (general review again)
- ⚠️ AutoInterface wrappers (Validation, Notify) may silently skip Submit* validation/notify (unverified); ⚠️ ResumableImportApi same; ⚠️ check all 12 FwData Submit* exist (build would catch: noise)
- ⚠️ no test pins record text (= tests added after review)
- 💭 Position overloads; 💭 RichString.ToString global
- 💭 **patch value with newlines still breaks the one-line promise** (= the CodeRabbit multi-line defect; only a nit here)
- 💭 stale interface comment; ✨ MovePicture rename

## 2514 test-auditor (best on this PR)
- ⚠️ dry-run bug fix (picture/component deleted on other side) with no regression test; names the SyncDryRun twin to write
- ⚠️ record-wording claim untested; concrete cheap test
- ⚠️ 10 normalization Submit* forwarders untested; names the two tests
- 💭 Summarize one-line promise untested (adjacent to multi-line defect); 💭 RichString.ToString untested

## 2514 dotnet-stylist
- ⚠️ MiniLcmApiValidationWrapper auto-forwards SubmitCreateSense/SubmitCreateExampleSentence without validation (2nd agent to raise it; check generated source)
- 💭 Summarize ", " join ambiguous, non-scalar values print as type names (adjacent to multi-line defect); 💭 UpdatePublication record lacks name; 💭 async wrappers; 💭 RichString.ToString

## 2514 intent-check
- ⚠️ Validation + Notify wrappers not covered by the PR's own "no silent inheritance" goal (3rd agent on this)
- ⚠️ normalization wrapper Submit* now delete-wins instead of throwing: behavior change not in title
- 💭 "standardize" incomplete for PoS/semdom/etc records; 💭 RichString.ToString; 💭 Summarize format change; 💭 MovePicture rename out of scope

## Verified: validation-wrapper gap was real
- Flagged by rename-detector, dotnet-stylist, intent-check on #2514. Fixed later in follow-up PR #2519 "Add and enforce missing validation calls in wrapper" (commit 42f82f2d). No human reviewer on #2514 raised it.
- Notify wrapper still has no SubmitUpdatePicture override on develop today (grep). Unverified whether that matters (BeaKona forwards it without notifying).

## 2514 ripple-detector (deepest on this PR)
- ⚠️ UI path: writeNorm now forwards to notify.Submit*, which has no override, so the linked entry in a complex-form link gets no change notification (stale UI). Reasoned from code, not built.
- ⚠️ UI edits to a concurrently deleted sense/entry now silently dropped (delete-wins) instead of throwing; not in PR title
- 💭 RichString.ToString puts user text in logs
- checked and cleared: all implementers define or auto-forward all 12 Submit*
- Possible live bug: `MiniLcmApiNotifyWrapper` on develop today has no Submit* overrides at all (grep), while writeNorm forwards SubmitCreateComplexFormComponent to it. If BeaKona forwards without notifying, the linked entry goes stale in the UI. Not built or reproduced.
