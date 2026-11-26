# Product

<!-- impeccable:product-schema 1 -->

Scope: the LexBox web app in `frontend/` (excluding `frontend/viewer/`, the FW Lite editor, which has its own record in `frontend/viewer/PRODUCT.md`).

## Platform

web

## Users

- **Project managers**: create projects, invite and manage members, handle join requests, set confidentiality and purpose.
- **Organization admins**: run an organization's set of projects and members.
- **Site admins and support staff**: SIL staff who support users, administer projects and accounts, and troubleshoot sync.

Many users work in a UI language other than English.

## Product Purpose

LexBox is a hosting service for FieldWorks, FieldWorks Lite, Language Forge, OneStory Editor, and WeSay projects, provided to language communities by SIL Language Technology and the Linguistics Institute at Payap University. The web app is where people find, set up, and administer those projects and their members. Success: managers get a project and its team set up without needing support, and support staff can resolve problems quickly.

## Positioning

The shared home for SIL's language-project tools: Mercurial Send/Receive hosting for FLEx and related tools, plus the sync server (Harmony) that FieldWorks Lite projects merge through.

## Operating Context

- Login, registration, email verification, invitations, password reset.
- My Projects, project pages (members, sync status, confidentiality, description), project creation and join requests, "Where's my project?".
- Organizations (create, list, manage), user settings, and a site admin dashboard.
- Most editing happens in FLEx or FW Lite, not here; LexBox is administrative.

## Capabilities and Constraints

- UI strings are localized (currently en, es, fr, ko).
- Project confidentiality is a real privacy concern for some communities; the UI already advises aliases for identifying names.
- Usage tracking is opt-out and disclosed.

## Brand Commitments

Product name "LexBox" (some copy writes "Lexbox"). No external brand guide binds the UI.

## Evidence on Hand

- Real project codes, names, members, and orgs are private. Never use them in designs, screenshots, copy, or anything posted publicly.
- No testimonials, user research, or usage metrics are on hand; do not invent them.

## Product Principles

1. **Protect the communities' data and identities.** Confidentiality and membership are never ambiguous.
2. **Self-service first.** A manager should be able to finish setup without contacting support.
3. **Make support fast.** Admin views favor finding and fixing over browsing.
