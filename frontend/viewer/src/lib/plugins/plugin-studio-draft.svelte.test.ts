import {describe, expect, it} from 'vitest';
import {PluginStudioDraft} from './plugin-studio-draft.svelte';

function html(body: string, head = '') {
  return `<!DOCTYPE html><html><head>${head}</head><body>${body}</body></html>`;
}

describe('PluginStudioDraft', () => {
  it('an untouched example can be added and is not customized', () => {
    const draft = new PluginStudioDraft('example', {name: 'Stats', description: 'Counts', html: html('a')});
    expect(draft.dirty).toBe(false);
    expect(draft.customized).toBe(false);
    expect(draft.canSave).toBe(true);
  });

  it('an example with new HTML is customized and has something to lose', () => {
    const draft = new PluginStudioDraft('example', {name: 'Stats', description: 'Counts', html: html('a')});
    draft.html = html('b');
    expect(draft.customized).toBe(true);
    expect(draft.dirty).toBe(true);
  });

  it('a new plugin needs a name and a complete document', () => {
    const draft = new PluginStudioDraft('new');
    expect(draft.canSave).toBe(false);
    draft.html = '<!DOCTYPE html><html><body>half';
    draft.name = 'Quiz';
    expect(draft.manifest).toBeUndefined();
    expect(draft.canSave).toBe(false);
    draft.html = html('quiz', '<meta name="fwlite-plugin-permissions" content="edit">');
    expect(draft.manifest?.permissions).toEqual(['edit']);
    expect(draft.canSave).toBe(true);
  });

  it('an edit can be saved only once something changed, and reset clears it', () => {
    const baseline = {name: 'Gaps', description: '', html: html('a')};
    const draft = new PluginStudioDraft('edit', baseline);
    expect(draft.canSave).toBe(false);
    draft.description = 'Finds gaps';
    expect(draft.canSave).toBe(true);
    draft.reset(baseline);
    expect(draft.dirty).toBe(false);
  });

  it('keeps replaced versions so the last one can be undone, and reviews against the right base', () => {
    const draft = new PluginStudioDraft('new');
    draft.applyVersion(html('v1'));
    expect(draft.reviewBaseHtml).toBe('');
    draft.applyVersion(html('v2'));
    expect(draft.reviewBaseHtml).toBe(html('v1'));
    draft.undoVersion();
    expect(draft.html).toBe(html('v1'));

    const edit = new PluginStudioDraft('edit', {name: 'Gaps', description: '', html: html('saved')});
    edit.applyVersion(html('v1'));
    edit.applyVersion(html('v2'));
    expect(edit.reviewBaseHtml).toBe(html('saved'));
  });
});
