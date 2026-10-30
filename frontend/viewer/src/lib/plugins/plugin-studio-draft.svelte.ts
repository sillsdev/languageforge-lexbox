import {isCompletePluginHtml} from './plugin-html';
import {parsePluginManifest, type PluginManifest} from './plugin-manifest';

/**
 * What the studio is doing: trying an example that isn't in the project yet, building a new
 * plugin, or editing one the project already has.
 */
export type StudioMode = 'example' | 'new' | 'edit';

export interface StudioBaseline {
  name: string;
  description: string;
  html: string;
}

/**
 * The plugin being worked on in the studio, compared against what it started from (the example,
 * an empty draft, or the saved plugin). Nothing here is persisted: leaving the studio drops it.
 */
export class PluginStudioDraft {
  baseline = $state.raw<StudioBaseline>({name: '', description: '', html: ''});
  name = $state('');
  description = $state('');
  // Raw: multi-MB strings replaced wholesale, never mutated in place.
  html = $state.raw('');
  /** Versions replaced by a paste or upload, newest last, so the last one can be undone. */
  previousVersions = $state.raw<string[]>([]);

  constructor(readonly mode: StudioMode, baseline?: StudioBaseline) {
    if (baseline) this.reset(baseline);
  }

  /** Starts over from a new baseline, e.g. once the plugin's file has loaded or after a save. */
  reset(baseline: StudioBaseline): void {
    this.baseline = baseline;
    this.name = baseline.name;
    this.description = baseline.description;
    this.html = baseline.html;
    this.previousVersions = [];
  }

  /** Swaps in a new version of the HTML, keeping the one it replaces for {@link undoVersion}. */
  applyVersion(html: string): void {
    if (html === this.html) return;
    this.previousVersions = [...this.previousVersions, this.html];
    this.html = html;
  }

  undoVersion(): void {
    const previous = this.previousVersions.at(-1);
    if (previous === undefined) return;
    this.previousVersions = this.previousVersions.slice(0, -1);
    this.html = previous;
  }

  /**
   * What a new version is reviewed against: what the studio started from (the saved plugin or the
   * example), or for a new plugin the version before it.
   */
  get reviewBaseHtml(): string {
    return this.baseline.html || this.previousVersions.findLast(html => !!html) || '';
  }

  readonly htmlChanged = $derived(this.html !== this.baseline.html);

  /** Anything that leaving the studio would lose. */
  readonly dirty = $derived(
    this.htmlChanged
    || this.name.trim() !== this.baseline.name.trim()
    || this.description.trim() !== this.baseline.description.trim());

  /** An example the user changed before adding is saved as its own "… (custom)" plugin. */
  get customized(): boolean {
    return this.mode === 'example' && this.htmlChanged;
  }

  readonly complete = $derived(isCompletePluginHtml(this.html));

  /** Parsed only from a complete document, so a half-pasted file shows no permissions. */
  readonly manifest: PluginManifest | undefined = $derived(this.complete ? parsePluginManifest(this.html) : undefined);

  get canSave(): boolean {
    return !!this.name.trim() && this.complete && (this.mode !== 'edit' || this.dirty);
  }
}
