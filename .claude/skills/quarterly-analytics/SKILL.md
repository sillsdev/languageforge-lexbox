---
name: quarterly-analytics
description: Collect the quarterly product analytics for FieldWorks Lite and Lexbox and enter them in the FieldWorks Lite and Lexbox rows of the LangTech Analytics Dashboard Google Sheet. Gathers GitHub releases, forum topics, Mixpanel users/MAU/countries/installs and support tickets itself, hands the user one command for the prod-only numbers (aws-prod DB + hg pod), asks only about FTE and anomalies, then writes the sheet after a final OK.
when_to_use: User says "quarterly analytics", "collect the analytics", "fill the analytics sheet", "LangTech analytics", "/quarterly-analytics FY27Q1", or a scheduled quarterly reminder kicks it off.
argument-hint: "[FYyyQn, default: the last finished quarter]"
allowed-tools: Bash(python .claude/skills/quarterly-analytics/scripts/public_sources.py:*) Read Glob Grep
---

# Quarterly product analytics: FieldWorks Lite + Lexbox

Fills the **FieldWorks Lite** and **Lexbox** rows of one quarter's tab (e.g. `FY27Q1`) in the Google Sheet "LangTech Analytics Dashboard". The shared Google Doc "Product Analytics (Lexbox & FW-Lite)" describes the same methods for humans; keep it in step when a method here changes.

SIL fiscal years start Oct 1 and are named after the calendar year they end in: FY27Q1 = Oct-Dec 2026, Q2 = Jan-Mar, Q3 = Apr-Jun, Q4 = Jul-Sep. The last finished quarter on, say, 2026-10-14 is FY26Q4.

## Ground rules

- **Privacy.** The scripts print counts and write nothing to disk, so project codes never leave prod. Support-thread subjects and anything else user-identifying stay in this conversation: don't save them to files, commit them or post them anywhere (repo AGENTS.md privacy rules).
- **Prod is the user's to run.** Never run kubectl or psql against aws-prod yourself. Show `scripts/prod-collect.ps1` as a click-to-run command and read the counts it prints.
- **Ask little, with AskUserQuestion, lightest question first.** Ask about FTE every quarter and anything the standing rules below don't settle (anomalies, flags). Don't ask the user to read files.
- **Nothing goes into the sheet before the user's final OK.** Write only the two product rows, never another product's cells.

## Steps

1. **Quarter and preflight.** Use the argument, or the last finished quarter. Check which connectors are attached: Google Drive + Sheets (required), Gmail, Mixpanel.
   - **No Mixpanel connector:** ask whether to browse Mixpanel with Claude in Chrome, or use Mixpanel's Query API with a service account. For the API, the user sets `MIXPANEL_SA_USER` and `MIXPANEL_SA_SECRET` in their environment. Pass them only as `curl -u "$MIXPANEL_SA_USER:$MIXPANEL_SA_SECRET" ...`, print only aggregate results, and never echo, log or inspect those variables.
   - **No Gmail connector:** page the `lexbox_support` Google Group in Chrome instead. Its list is sorted by last activity, so check each thread's first-message date.
2. **Read the sheet.** Find "LangTech Analytics Dashboard" with Drive search. Read rows 1-2 (headers) and the FieldWorks Lite and Lexbox rows of this quarter's tab, plus the previous quarter's tab for comparison and the FTE history. Map columns by header text, because the columns change between quarters. If the quarter's tab doesn't exist yet, stop and say so: the sheet owner creates it.
3. **Collect, in parallel where you can:**
   - `python .claude/skills/quarterly-analytics/scripts/public_sources.py <quarter>` covers releases, forum topics and the FTE sanity check (about 20 s).
   - Give the user the prod command, in its own `powershell` block so they can run it from the Terminal panel. psql asks for the prod DB password once; the hg scan takes a few minutes:
     `pwsh .claude/skills/quarterly-analytics/scripts/prod-collect.ps1 -Quarter <quarter>`
     When they say it's done, read the counts it prints at the end from the Terminal panel, or ask them to paste them. `-Step db` or `-Step hg` re-runs one half. A re-run of the hg half refuses to start while an earlier scan is still running in the pod.
   - Mixpanel and support tickets, as described below.
4. **Check.** Compare every value with last quarter. Flag changes over about 50%, missing values, `hg_failures` above 0, and any `check:` lines from `public_sources.py`.
5. **Ask.** FTE first (see the rules), then any anomalies. Then show the final two rows, the sheet's URL and tab, and ask for the OK to write them and sign off as the Owner listed on the "Product List" tab.
6. **Write and verify.** Write with the Sheets connector (`update_values`, numbers as numbers, `null` for cells to leave alone), then read the rows back.
7. **Offer the doc update.** Offer to update the doc's per-quarter bullets ("FY27Q1: ...") as suggestions.

## Standing rules (agreed 2026-10-14; ask before changing one)

| Column | FieldWorks Lite | Lexbox |
|---|---|---|
| Latest Release Version / Date, Number of Releases | Releases published in the quarter that have FW Lite assets (`public_sources.py`) | Releases published in the quarter whose notes have Lexbox items |
| Supported FTE, Paid FTE | Ask every quarter, pre-filled from last quarter's tab. It's the team total split across the two rows, never the total on both. History is in earlier tabs and the doc's "Supported FTE" section. Show the PR-label share from `public_sources.py` as a sanity check. | Same question |
| Downloads | Not collected from FY27Q1 on (dropped 2026-10-14, though the sheet config still marks it required). Leave blank. | n/a |
| Installs | Mixpanel: users whose first-ever `app_launched` falls in the quarter. See the FY27Q1 caveat. | n/a |
| Active Projects | `fwl_active_projects`: projects with a CRDT commit in the quarter, deleted ones included | `lexbox_active_projects`: hg repos with a commit dated in the quarter. Every folder is included, deleted and reset backups too, so a project reset during the quarter counts twice (the doc's method). |
| New Projects Started | `fwl_new_projects`: first-ever CRDT commit in the quarter | `lexbox_created_projects`: projects created in the quarter, deleted ones included |
| Active Users | Mixpanel unique users of `app_launched` in the quarter | Mixpanel unique users of any `lexbox` event in the quarter |
| Avg MAU, MAU month 1-3 | Mixpanel unique users per calendar month; Avg = mean, 1 decimal | Same |
| Number of User Countries | Mixpanel distinct `mp_country_code` over the quarter | Same (Lexbox events carry the client IP) |
| Number of Languages Impacted | `fwl_languages_impacted`: active projects whose code doesn't contain test, train, demo, dev or copy (in `prod-queries.sql` and `hg-active-repos.sh`). No manual judgment. | `lexbox_languages_impacted`, same filter |
| New Support Tickets | Threads *started* in the quarter in the `lexbox_support` group, sorted into FW Lite or Lexbox by content. Count borderline support requests. Exclude FW Lite beta join requests, alerts (prod alerts, Honeycomb triggers), spam, FYI forwards and messages with no request in them. | Same |
| New Community Topics | Topics created in the quarter in the FW Lite forum category (`public_sources.py`) | 0, there is no forum |
| Analytics contain test projects/users (Product List tab) | Yes | Yes |

## Mixpanel

Everything is in the Mixpanel project named "Lexbox" (`Get-Projects` gives its id):
- **FW Lite:** `app_launched`, once per app start (desktop and Android). Tracking started with v2026-09-01. Don't filter it on `product`: that property only arrived around v2026-09-28, and older installs still send `app_launched` without it.
- **Lexbox:** server-side `send_receive`, `fw_lite_sync`, `login_completed` and `account_created`, keyed by user id, all with `product` = `lexbox`. Query `$all_events` filtered on `product` = `lexbox`. Tracking started 2026-09-28, so FY27Q1 is its first full quarter and there's nothing earlier to compare with.

Queries go through the connector's `Run-Query` (`insights`):
- **Quarter totals** (Active Users, countries): `chartType` `bar` over the quarter's absolute date range, which counts each user once. Countries are the `aggregate-property` measurement, `unique_values` of `mp_country_code`.
- **MAU:** `chartType` `line` with `unit` `month`. Never add months up.
- **Installs:** the query schema has no "first time ever" filter, so subtract: unique users from 2026-01-01 to the quarter's last day, minus unique users from 2026-01-01 to the day before the quarter.
- To split by event, break down on `$event_name` ("Event Name" returns `undefined`).

Use only `Run-Query` and the Get/List tools; never create, edit, delete, merge or dismiss anything in Mixpanel.

Users who opted out of analytics never show up, and the team's own devices do.

**FY27Q1 caveat (delete this paragraph once FY27Q2 is done).** Installs overcount in FY27Q1. Existing devices that first sent `app_launched` after updating to an analytics build (Sep 2026 or later) look like new installs. The effect fades in FY27Q2 and is negligible after that. In FY27Q1 add a short note to the FW Lite Notes cell; in FY27Q2 add one only if Installs look high against Active Users.

## Support tickets via Gmail

Search `list:lexbox_support@groups.sil.org after:YYYY/MM/DD before:YYYY/MM/DD -subject:FIRING -subject:RESOLVED -subject:Triggered` (Gmail's `before:` is exclusive, and the dates are Pacific time, so double-check threads that start right at a boundary). Page through all results, then check each candidate's first message's date and topic. Things Gmail does differently from the group itself:
- **Subject changes split threads.** A follow-up about a new problem from the same person can land in its own Gmail thread, while the group shows one conversation. Count it once.
- **Only mail the user received is there.** If the earliest message is a reply ("Re: …"), the thread started earlier; take the date from the quoted original.
- **Spam is still there.** Marketing mail ("Congrats on launching…") still arrives; skip it.

Use only search and read; never the connector's draft, send, label, trash or filter tools.

## Prod scripts and load

`prod-collect.ps1` port-forwards the DB and kills the forward's whole process tree afterwards. It runs `prod-queries.sql` in a read-only transaction with statement and lock timeouts, then pipes `hg-active-repos.sh` into the hg pod. Both bring back only counts. The scan:
- runs one `hg log` at a time, at `nice 19`;
- skips repos whose changelog hasn't changed in the month before the quarter;
- takes a lock in the pod, so two scans never overlap.

Never parallelize it: a parallel scan has taken a node down before. Both scripts are measured against the methods the doc had before them and must stay at or below their load.
