# FW Lite — Example Forum Release Notes (Reference)

Real release notes posted to the SIL community forum (Discourse), in the format generated for this project: `##` version headers, `####` section headers with emoji, `*` bullets. Kept as a reference for format, tone, and filtering.

These are historical posts, preserved as posted. v2026-04-08 and v2026-04-09 lead with Bug Fixes — deliberate uses of SKILL.md's priority exception (the fix was the headline; the improvements were filler). v2025-12-12's 🧰 Maintenance section is a one-off not in the current contract. On any other conflict, SKILL.md wins.

---

## Version: v2026-06-12
#### 🚀 Improvements
* Login now opens in an in-app browser tab on Android (more reliable on low-memory devices, and it no longer leaves a stray browser page open after you sign in)
* Text you enter is now consistently Unicode-normalized (NFD) when saved, so data-entry and search results match (previously this normalization only happened after syncing with FieldWorks)

#### 🐛 Bug Fixes
* Fixed 2 bugs preventing downloading projects
* Fixed getting stuck on the Tasks page when it was the last page open
* Fixed being unexpectedly signed out after a temporary network error
* Fixed “Last sync” sometimes showing stale or missing information
* Various other small bug fixes

## Version: v2026-05-28
#### ✨ New Features
* Added morpheme type markers to headwords (e.g. dashes “-” on affixes) (this will cause a one-time delay the first time each project is opened)
* Respect FieldWorks homograph numbers when sorting entries (not displayed yet)

#### 🚀 Improvements
* View mode (simple/preview) and dictionary preview pin are now remembered per project
* When reviewing entries touched during a task, the edit dialog now shows all fields regardless of the current custom view

#### 🐛 Bug Fixes
* Fixed focus being lost when tabbing out of a field
* Fixed complex form components accumulating spurious ordering changes on each sync
* Fixed app sometimes freezing when opening an entry
* Fixed “Last sync” time sometimes showing in the future
* Fixed Troubleshoot dialog not opening on broken projects
* Fixed a crash on Android
* Fixed a crash caused by media files with similar filenames
* Various other small bug fixes

## Version: v2026-04-28
#### 🐛 Bug Fixes
* Fix values sometimes being copied to other entries
* Fix app freezing during sync

## Version: v2026-04-09
#### 🐛 Bug Fixes
* Fixed custom views on Android

#### 🚀 Improvements
* Minor UI improvements

## Version: v2026-04-08
#### 🐛 Bug Fixes
* Fixed opening projects on Android

#### 🚀 Improvements
* Minor UI improvements

## Version: v2026-04-07
#### ✨ New Features
* Custom views: Create your own custom view with only the fields and writing-systems you want to see.
* Pick up where you left off: The app now remembers what project, view, task etc. you were working on when you closed it and will take you right back the next time you open it.

#### 🚀 Improvements
* Allow creating an entry with no gloss or definition (i.e. removed validation that was causing friction)
* Improved contrast in the dictionary preview
* Updated app theme

## Version: v2026-03-02
#### 🚀 Improvements
* Made auto-syncing more reliable.
* Various UI improvements and visual updates.

#### 🐛 Bug Fixes
* Fix: “Go to Word” links on mobile didn’t open the selected word/entry.

## Version: v2026-02-11
#### ✨ New Features
* Filter words/entries by a specific publication

#### 🚀 Improvements
* Improve search result order (words that start with search text come first)
* Stop auto-capitalizing search on mobile

#### 🐛 Bug Fixes
* Fix: parts of speech list usually empty

## Version: v2026-02-05
#### ✨ New Features
* Add Updates dialog to check for and trigger updates

#### 🚀 Improvements
* Words/Entries are now loaded as you scroll, so:
    * It’s much faster
    * You can now scroll through all your entries (not just the first 5 thousand)
    * If you change/clear your filter you’ll still be scrolled to the selected word/entry
* Add missing translations (especially in Swahili and Malay)
* Some basic UI redesign

#### 🐛 Bug Fixes
* New history/activity records for senses/meanings now correctly show the current part of speech

## Version: v2025-12-12
#### 🧰 Maintenance
* Updated a large number of packages

## Version: v2025-12-03
#### ✨ New Features
* Added part of speech to new word dialog
* Prefill Part of speech and Semantic domain in new word dialog based on current filter (this enabled a basic form of collecting words by semantic domain and is the only time Semantic domains are shown in the dialog)

#### 🚀 Improvements
* Improve “Best match” sorting, so best headword matches are always shown first
* Added some missing translations in major languages

#### 🐛 Bug Fixes
* Stop mixing Reversal Index Parts of Speech into Vernacular parts of speech (Previously we imported both sets of parts of speech and treated them all as vernacular. This lead to duplicate and invalid parts of speech in FieldWorks Lite. The next time you sync FieldWorks and FieldWorks Lite, Reversal Index Parts of speech will be deleted from FieldWorks Lite.)
* Fixed some bugs when multiple users edit the same word simultaneously.
