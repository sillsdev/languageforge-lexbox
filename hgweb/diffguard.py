"""Keep hgweb from rendering diffs too large for the memory it has.

hgweb holds a file's whole diff in memory while rendering it, and the changeset
page diffs everything twice (once more for the diffstat). Measured on 6.3.2,
peak RSS is roughly 8x the size of the files touched (before + after), so a
changeset that rewrites a few large files pushes the single mod_wsgi daemon
past the container memory limit and takes Send/Receive down with it. Mercurial
has no built-in cap for this.

This extension adds up the sizes of the files a page would diff, straight from
the revlog index, and compares the total with ``diffguard.maxbytes`` (0
disables the check). Over the limit, the changeset page is served without the
diff (file list and per-file links still work) and the single-file pages
(diff, comparison, annotate, filelog with patches) answer 403.

Keep the limit small: a browser can't use a multi-megabyte diff page anyway,
and the headroom is what keeps concurrent pages and pushes alive.
"""

from mercurial import extensions, registrar, templateutil, util
from mercurial.hgweb import webcommands, webutil
from mercurial.hgweb.common import HTTP_FORBIDDEN, ErrorResponse

testedwith = b'6.3.2'

configtable = {}
configitem = registrar.configitem(configtable)
configitem(b'diffguard', b'maxbytes', default=b'8MB')


def _size(ctx, path):
    return ctx[path].size() if path in ctx else 0


def _toolarge(web, pairs):
    """Return an explanation if diffing the (ctx, basectx, path) pairs would
    exceed the limit, else None."""
    limit = web.repo.ui.configbytes(b'diffguard', b'maxbytes')
    if limit <= 0:
        return None
    total = sum(_size(ctx, p) + _size(basectx, p) for ctx, basectx, p in pairs)
    if total <= limit:
        return None
    return b'diff too large to display: %s of changed files, limit is %s' % (
        util.bytecount(total),
        util.bytecount(limit),
    )


def _noticeblock(message):
    """A one-line diffblock in the shape _diffsgen produces, so every style renders it."""

    def gen(context):
        lines = templateutil.mappedgenerator(
            webutil._prettyprintdifflines, args=([message], 1, b'')
        )
        yield {b'parity': 0, b'blockno': 1, b'lines': lines}

    return templateutil.mappinggenerator(gen, name=b'diffblock')


def _guardchangeset(orig, web):
    try:
        ctx = webutil.changectx(web.repo, web.req)
        basectx = webutil.basechangectx(web.repo, web.req) or ctx.p1()
        status = basectx.status(ctx)
        paths = status.modified + status.added + status.removed
        message = _toolarge(web, [(ctx, basectx, p) for p in paths])
    except Exception:
        # let the real command produce its own error page
        return orig(web)
    if message is None:
        return orig(web)
    entry = webutil.changesetentry(web, ctx)
    entry['diff'] = _noticeblock(message)
    entry['diffstat'] = templateutil.mappedgenerator(lambda context: iter(()))
    entry['diffsummary'] = lambda context, mapping: message
    return web.sendtemplate(b'changeset', **entry)


def _requestedfile(web):
    try:
        fctx = webutil.filectx(web.repo, web.req)
        return fctx.changectx(), fctx.path()
    except LookupError:
        # file removed in this changeset, same fallback as filediff
        ctx = webutil.changectx(web.repo, web.req)
        return ctx, webutil.cleanpath(web.repo, web.req.qsparams[b'file'])


def _guardfile(orig, web):
    try:
        ctx, path = _requestedfile(web)
        message = _toolarge(web, [(ctx, ctx.p1(), path)])
    except Exception:
        return orig(web)
    if message is not None:
        raise ErrorResponse(HTTP_FORBIDDEN, message)
    return orig(web)


def _guardfilelog(orig, web):
    if b'patch' not in web.req.qsparams:
        return orig(web)
    try:
        # the revisions filelog would show, with a diff each (see webcommands.filelog)
        fctx = webutil.filectx(web.repo, web.req)
        revcount = web.maxshortchanges
        if b'revcount' in web.req.qsparams:
            try:
                revcount = max(int(web.req.qsparams[b'revcount']), 1)
            except ValueError:
                pass
        count = fctx.filerev() + 1
        start = max(0, count - revcount)
        end = min(count, start + revcount)
        pairs = []
        for filerev in fctx.filelog().revs(start, end - 1):
            c = fctx.filectx(filerev).changectx()
            pairs.append((c, c.p1(), fctx.path()))
        message = _toolarge(web, pairs)
    except Exception:
        return orig(web)
    if message is not None:
        raise ErrorResponse(HTTP_FORBIDDEN, message)
    return orig(web)


def extsetup(ui):
    for name in ('changeset', 'rev'):
        extensions.wrapfunction(webcommands, name, _guardchangeset)
    for name in ('filediff', 'diff', 'comparison', 'annotate'):
        extensions.wrapfunction(webcommands, name, _guardfile)
    extensions.wrapfunction(webcommands, 'filelog', _guardfilelog)
