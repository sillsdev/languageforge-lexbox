import {describe, expect, it} from 'vitest';
import {isCompletePluginHtml} from './plugin-html';

const complete = `<!DOCTYPE html>
<html>
<head><title>x</title></head>
<body>hi</body>
</html>`;

describe('isCompletePluginHtml', () => {
  it('rejects empty and whitespace-only', () => {
    expect(isCompletePluginHtml('')).toBe(false);
    expect(isCompletePluginHtml('   \n\t  ')).toBe(false);
  });

  it('accepts a full document', () => {
    expect(isCompletePluginHtml(complete)).toBe(true);
  });

  it('is case-insensitive for doctype and closing html tag', () => {
    expect(isCompletePluginHtml('<!doctype HTML><html></HTML>')).toBe(true);
    expect(isCompletePluginHtml('<!Doctype Html>\n<html>\n</Html>\n')).toBe(true);
  });

  it('allows leading BOM and surrounding whitespace', () => {
    expect(isCompletePluginHtml(`\uFEFF  ${complete}  \n`)).toBe(true);
  });

  it('rejects HTML missing the doctype', () => {
    expect(isCompletePluginHtml('<html><body>hi</body></html>')).toBe(false);
  });

  it('rejects HTML missing the closing html tag', () => {
    expect(isCompletePluginHtml('<!DOCTYPE html><html><body>hi</body>')).toBe(false);
  });

  it('rejects truncated mid-file pastes', () => {
    expect(isCompletePluginHtml('<!DOCTYPE html>\n<html>\n<head>\n<title>Partial')).toBe(false);
  });

  it('rejects doctype that is not at the start', () => {
    expect(isCompletePluginHtml(`<!-- note -->\n${complete}`)).toBe(false);
  });
});
