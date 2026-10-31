const DOCTYPE = '<!doctype html>';
const CLOSE_HTML = '</html>';

/**
 * Whether plugin HTML looks like a complete document (`<!DOCTYPE html>` … `</html>`).
 * Empty/whitespace-only returns false.
 *
 * Scans only the edges (no trim/replace of the full string) so multi-MB pastes stay cheap.
 */
export function isCompletePluginHtml(html: string): boolean {
  let start = 0;
  let end = html.length;
  if (end > 0 && html.charCodeAt(0) === 0xfeff) start = 1;
  while (start < end && isHtmlWhitespace(html.charCodeAt(start))) start++;
  if (start === end) return false;
  while (end > start && isHtmlWhitespace(html.charCodeAt(end - 1))) end--;

  return (
    end - start >= DOCTYPE.length + CLOSE_HTML.length &&
    equalsIgnoreCaseAt(html, start, DOCTYPE) &&
    equalsIgnoreCaseAt(html, end - CLOSE_HTML.length, CLOSE_HTML)
  );
}

/** Same whitespace set as `String.prototype.trim` (ES2019+). */
function isHtmlWhitespace(code: number): boolean {
  return (
    code === 0x09 || // tab
    code === 0x0a || // lf
    code === 0x0b || // vt
    code === 0x0c || // ff
    code === 0x0d || // cr
    code === 0x20 || // space
    code === 0xa0 || // nbsp
    code === 0x1680 ||
    (code >= 0x2000 && code <= 0x200a) ||
    code === 0x2028 || // line separator
    code === 0x2029 || // paragraph separator
    code === 0x202f ||
    code === 0x205f ||
    code === 0x3000 ||
    code === 0xfeff
  );
}

function equalsIgnoreCaseAt(haystack: string, index: number, asciiLowerNeedle: string): boolean {
  for (let i = 0; i < asciiLowerNeedle.length; i++) {
    const c = haystack.charCodeAt(index + i);
    // ASCII letters only in our needles; fold A-Z → a-z
    const lower = c >= 65 && c <= 90 ? c + 32 : c;
    if (lower !== asciiLowerNeedle.charCodeAt(i)) return false;
  }
  return true;
}

const DOCTYPE_PATTERN = /<!doctype html/i;

/**
 * Pulls the plugin document out of an AI chat reply: from the first `<!DOCTYPE html` to the last
 * `</html>`, dropping any prose or code fences around it. Returns the input unchanged when it holds
 * no such document, so the caller's completeness check reports the problem.
 */
export function extractPluginHtml(reply: string): string {
  const start = reply.search(DOCTYPE_PATTERN);
  const end = reply.toLowerCase().lastIndexOf(CLOSE_HTML);
  if (start < 0 || end < start) return reply;
  return reply.slice(start, end + CLOSE_HTML.length);
}

/** The document's `<title>`, used to name a pasted plugin. */
export function pluginHtmlTitle(html: string): string | undefined {
  return new DOMParser().parseFromString(html, 'text/html').title.trim() || undefined;
}
