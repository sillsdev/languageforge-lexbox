"""Quarterly numbers from public sources: GitHub releases, the FW Lite community forum, and the
PR-label FTE split. Needs `gh` (logged in) and Python 3.10+, nothing else.

    python public_sources.py FY27Q1

Prints the numbers and the releases, flags and forum topics behind them. Writes nothing.
"""
import datetime as dt
import json
import re
import subprocess
import sys
import urllib.request

REPO = "sillsdev/languageforge-lexbox"
FORUM_CATEGORY = "https://community.software.sil.org/c/fwlite/48/l/latest.json"


def quarter_range(quarter):
    m = re.fullmatch(r"FY(\d{2})Q([1-4])", quarter)
    if not m:
        sys.exit(f"Quarter must look like FY27Q1, got {quarter!r}")
    fy, q = 2000 + int(m[1]), int(m[2])
    start = dt.date(fy - 1 if q == 1 else fy, [10, 1, 4, 7][q - 1], 1)
    end = dt.date(start.year + (start.month + 2) // 12, (start.month + 2) % 12 + 1, 1)
    return start, end


def gh_json(*args):
    out = subprocess.run(["gh", *args], capture_output=True, text=True, encoding="utf-8", check=True).stdout
    # --paginate prints one JSON array per page back to back.
    decoder, items, i = json.JSONDecoder(), [], 0
    while i < len(out):
        if out[i].isspace():
            i += 1
            continue
        page, i = decoder.raw_decode(out, i)
        items.extend(page if isinstance(page, list) else [page])
    return items


def in_range(iso, start, end):
    return start.isoformat() <= iso[:10] < end.isoformat()


def section_items(body, title):
    m = re.search(rf"^### {re.escape(title)}[^\n]*\n(.*?)(?=^#|\Z)", body or "", re.M | re.S)
    return len(re.findall(r"^[*-] ", m[1], re.M)) if m else 0


def releases(start, end):
    found = [r for r in gh_json("api", "--paginate", f"repos/{REPO}/releases?per_page=100")
             if not r["draft"] and r["published_at"] and in_range(r["published_at"], start, end)]
    found.sort(key=lambda r: r["published_at"])
    fwl, lexbox, flags = [], [], []
    for r in found:
        has_assets = bool(r["assets"]) or r["tag_name"].startswith("fw-lite")
        fwl_notes, lex_notes = section_items(r["body"], "FieldWorks Lite"), section_items(r["body"], "Lexbox")
        if has_assets:
            fwl.append(r)
        if lex_notes:
            lexbox.append(r)
        if fwl_notes and not has_assets:
            flags.append(f"{r['tag_name']}: FW Lite release notes but no FW Lite assets (not counted for FW Lite)")
        if not has_assets and not lex_notes and not fwl_notes:
            flags.append(f"{r['tag_name']}: no assets and no FW Lite/Lexbox notes (not counted)")
        if r["prerelease"]:
            flags.append(f"{r['tag_name']}: marked pre-release (counted)")

    def summary(rs):
        return {"count": len(rs),
                "latest": rs[-1]["tag_name"] if rs else None,
                "latestDate": rs[-1]["published_at"][:10] if rs else None,
                "tags": [r["tag_name"] for r in rs]}

    return {"fwl": summary(fwl), "lexbox": summary(lexbox), "flags": flags}


def forum_topics(start, end):
    topics, page = {}, 0
    while page < 30:
        req = urllib.request.Request(f"{FORUM_CATEGORY}?page={page}", headers={"User-Agent": "lexbox-quarterly-analytics"})
        with urllib.request.urlopen(req, timeout=30) as resp:
            batch = json.load(resp).get("topic_list", {}).get("topics", [])
        if not batch:
            break
        for t in batch:
            topics[t["id"]] = t
        # Topics are sorted by last activity, so keep paging until a whole page predates the quarter.
        if all(t["last_posted_at"] and t["last_posted_at"][:10] < start.isoformat() for t in batch):
            break
        page += 1
    created = sorted((t for t in topics.values() if in_range(t["created_at"], start, end)),
                     key=lambda t: t["created_at"])
    return {"count": len(created), "topics": [{"created": t["created_at"][:10], "title": t["title"]} for t in created]}


FWL_PATHS = ("backend/FwLite/", "frontend/viewer/", "backend/FwHeadless/", "backend/Testing/FwHeadless",
             "platform.bible-extension/")
LEXBOX_PATHS = ("backend/LexBoxApi/", "backend/LexCore/", "backend/LexData/", "backend/SyncReverseProxy/",
                "backend/LfClassicData/", "backend/Testing/", "frontend/", "hgweb/", "deployment/", "otel/")
NOISE = re.compile(r"(\.verified\.|lock\.ya?ml$|package-lock\.json$|\.po$|generated-types/|"
                   r"/Migrations/.*(Designer|ModelSnapshot)\.cs$|schema\.graphql$|/gql/generated/|/Templates/.*\.json$)")


def fte_share(start, end):
    """FW Lite share of merged PRs, two ways. Only a sanity check for the FTE split the devs confirm."""
    last_day = end - dt.timedelta(days=1)
    prs = [p for p in gh_json("pr", "list", "-R", REPO, "--state", "merged", "--limit", "1000",
                              "--search", f"merged:{start}..{last_day} base:develop",
                              "--json", "labels,author,files")
           if "dependabot" not in p["author"]["login"]]
    by_label = {"fwl": 0.0, "lexbox": 0.0, "unlabelled": 0}
    by_files = {"fwl": 0.0, "lexbox": 0.0}
    for p in prs:
        names = {lab["name"] for lab in p["labels"]}
        f = bool(names & {"💻 FW Lite", "🔩 FW Lite Core", "📙 Platform.Bible"})
        lx = "📦 Lexbox" in names
        if f and lx:
            by_label["fwl"] += 0.5
            by_label["lexbox"] += 0.5
        elif f:
            by_label["fwl"] += 1
        elif lx:
            by_label["lexbox"] += 1
        else:
            by_label["unlabelled"] += 1
        lines = {"fwl": 0, "lexbox": 0}
        for file in p["files"]:
            if NOISE.search(file["path"]):
                continue
            n = file["additions"] + file["deletions"]
            if file["path"].startswith(FWL_PATHS):
                lines["fwl"] += n
            elif file["path"].startswith(LEXBOX_PATHS):
                lines["lexbox"] += n
        total = lines["fwl"] + lines["lexbox"]
        if total:
            by_files["fwl"] += lines["fwl"] / total
            by_files["lexbox"] += lines["lexbox"] / total

    def pct(d):
        t = d["fwl"] + d["lexbox"]
        return round(100 * d["fwl"] / t) if t else None

    return {"mergedPrs": len(prs), "fwlPercentByLabel": pct(by_label), "fwlPercentByFiles": pct(by_files),
            "byLabel": by_label}


def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    # Forum titles can hold characters the Windows console code page can't encode.
    sys.stdout.reconfigure(encoding="utf-8")
    quarter = sys.argv[1]
    start, end = quarter_range(quarter)
    r, f, fte = releases(start, end), forum_topics(start, end), fte_share(start, end)
    print(f"{quarter}: {start} .. {end} (exclusive)")
    for name, key in (("FW Lite", "fwl"), ("Lexbox", "lexbox")):
        print(f"{name} releases: {r[key]['count']}, latest {r[key]['latest']} ({r[key]['latestDate']}): "
              f"{', '.join(r[key]['tags'])}")
    for flag in r["flags"]:
        print(f"  check: {flag}")
    print(f"FW Lite forum topics: {f['count']}")
    for t in f["topics"]:
        print(f"  {t['created']} {t['title']}")
    print(f"FTE sanity check: FW Lite {fte['fwlPercentByLabel']}% by PR label, "
          f"{fte['fwlPercentByFiles']}% by files touched ({fte['mergedPrs']} PRs, {fte['byLabel']['unlabelled']} unlabelled)")


if __name__ == "__main__":
    main()
