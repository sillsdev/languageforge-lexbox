<script module lang="ts">
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import ActivityItemChangePreview from '$lib/activity/ActivityItemChangePreview.svelte';
  import CollapsedEntryDiff from '$lib/activity/CollapsedEntryDiff.svelte';
  import {fwliteStoryParameters} from '../fwl-parameters';
  import {allWsEntry} from '$project/demo/demo-entry-data';
  import type {IChangeContext, IComplexFormComponent, IEntry, IObjectWithId, IPartOfSpeech, ISemanticDomain} from '$lib/dotnet-types';

  const {Story} = defineMeta({
    title: 'activity/previews',
    parameters: fwliteStoryParameters({resizable: false}),
  });

  function ctx(partial: Partial<IChangeContext>): IChangeContext {
    return {commitId: 'demo', changeIndex: 0, changeName: 'Demo change', affectedEntries: [], entityType: 'Unknown', ...partial} as IChangeContext;
  }

  const entry = allWsEntry;
  const sense = entry.senses[0];
  const example = sense.exampleSentences[0];

  const entryBefore: IEntry = {...entry, lexemeForm: {...entry.lexemeForm, seh: 'nyumba'}, citationForm: {seh: 'nyumba'}};
  const entryAfter: IEntry = {...entry, lexemeForm: {...entry.lexemeForm, seh: 'nyumba zikulu'}, citationForm: {}};
  const homographEntry: IEntry = {...entryAfter, homographNumber: 2};

  const senseBefore = {...sense, gloss: {...sense.gloss, en: 'dwelling'}};
  const senseAfter = {...sense, gloss: {...sense.gloss, en: 'house'}};

  const exampleBefore = {...example, sentence: {...example.sentence, seh: {spans: [{text: 'Nyumba ndi yaing ono', ws: 'seh'}]}}};
  const exampleAfter = example;

  // An example that added an audio recording of the sentence — the audio ws should show a player, not a URI.
  const exampleAudioAfter = {...example, sentence: {...example.sentence, 'seh-Zxxx-x-audio': {spans: [{text: 'sil-media://lexbox.org/00000000-0000-0000-0000-0000000000f1', ws: 'seh-Zxxx-x-audio'}]}}};

  const posBefore: IPartOfSpeech = {id: '00000000-0000-0000-0000-0000000000c1', name: {en: 'Noun'}, predefined: true};
  const posAfter: IPartOfSpeech = {...posBefore, name: {en: 'Common noun'}};

  const semDomBefore: ISemanticDomain = {id: '00000000-0000-0000-0000-0000000000c2', name: {en: 'Universe, creation'}, code: '1', predefined: true};
  const semDomAfter: ISemanticDomain = {...semDomBefore, name: {en: 'Universe, creation, cosmos'}};

  // A sense whose middle semantic domain (by code) is removed — verifies the removed badge keeps its sorted
  // position rather than being dumped at the end.
  const dom = (code: string, name: string): ISemanticDomain => ({id: `sd-${code}`, code, name: {en: name}, predefined: true});
  const senseDomBefore = {...sense, semanticDomains: [dom('1.1', 'Sky'), dom('5.2', 'Food'), dom('8.3', 'Colour')]};
  const senseDomAfter = {...sense, semanticDomains: [dom('1.1', 'Sky'), dom('8.3', 'Colour')]};

  const complexFormId = '00000000-0000-0000-0000-0000000000a1';
  const addedComponentId = '00000000-0000-0000-0000-0000000000aa';
  const cfc = {
    id: '00000000-0000-0000-0000-0000000000bb',
    complexFormEntryId: complexFormId,
    complexFormHeadword: 'nyumba yaikulu',
    componentEntryId: addedComponentId,
    componentHeadword: 'mwala',
    order: 1,
  } as unknown as IComplexFormComponent;
  const siblingCfc = {
    id: '00000000-0000-0000-0000-0000000000cc',
    complexFormEntryId: complexFormId,
    componentEntryId: '00000000-0000-0000-0000-0000000000dd',
    componentHeadword: 'thanthwe',
    order: 2,
  } as unknown as IComplexFormComponent;
  const complexFormEntry: IEntry = {...entry, id: complexFormId, lexemeForm: {seh: 'nyumba yaikulu'}, citationForm: {}, senses: [], components: [cfc, siblingCfc], complexForms: []};
  const componentEntry: IEntry = {...entry, id: addedComponentId, lexemeForm: {seh: 'mwala'}, citationForm: {}, senses: [], components: [], complexForms: [cfc]};

  const cases: {label: string; context: IChangeContext}[] = [
    {label: 'Entry — edited', context: ctx({entityType: 'Entry', previousSnapshot: entryBefore, snapshot: entryAfter, affectedEntries: [entryAfter]})},
    {label: 'Entry — homograph (subscript in header)', context: ctx({entityType: 'Entry', previousSnapshot: entryBefore, snapshot: homographEntry, affectedEntries: [homographEntry]})},
    {label: 'Entry — created (no before)', context: ctx({entityType: 'Entry', snapshot: entry, affectedEntries: [entry]})},
    {label: 'Sense — edited', context: ctx({entityType: 'Sense', previousSnapshot: senseBefore, snapshot: senseAfter, affectedEntries: [entry]})},
    {label: 'Sense — removed middle semantic domain (stays in sorted position)', context: ctx({entityType: 'Sense', previousSnapshot: senseDomBefore, snapshot: senseDomAfter, affectedEntries: [entry]})},
    {label: 'Example — edited', context: ctx({entityType: 'ExampleSentence', previousSnapshot: exampleBefore, snapshot: exampleAfter, affectedEntries: [entry]})},
    {label: 'Example — added audio recording (playable)', context: ctx({entityType: 'ExampleSentence', previousSnapshot: example, snapshot: exampleAudioAfter, affectedEntries: [entry]})},
    {label: 'Complex form component — added', context: ctx({entityType: 'ComplexFormComponent', snapshot: cfc, affectedEntries: [complexFormEntry, componentEntry]})},
    {label: 'Complex form component — added but later removed (live state has dropped the CFC)', context: (() => {
      // Simulates: this commit added the CFC, a later commit removed it. affectedEntries carry live-state
      // (post-removal) so both entries' lists don't include the CFC — the preview must re-inject it as "added".
      const complexFormLive: IEntry = {...complexFormEntry, components: [siblingCfc]};
      const componentLive: IEntry = {...componentEntry, complexForms: []};
      return ctx({entityType: 'ComplexFormComponent', snapshot: cfc, affectedEntries: [complexFormLive, componentLive]});
    })()},
    {label: 'Complex form component — removed', context: (() => {
      // Removed: the CFC is gone from the current entries' lists; previousSnapshot carries the departed link.
      const complexFormAfter: IEntry = {...complexFormEntry, components: [siblingCfc]};
      const componentAfter: IEntry = {...componentEntry, complexForms: []};
      return ctx({entityType: 'ComplexFormComponent', previousSnapshot: cfc, affectedEntries: [complexFormAfter, componentAfter]});
    })()},
    {label: 'Complex form component — removed (deleted-snapshot present, mimics real backend)', context: (() => {
      // Harmony's DeleteChange emits BOTH snapshots — previousSnapshot = the live CFC, snapshot = the same CFC with
      // deletedAt set. The frontend must treat the deleted snapshot as absent, or it wrongly reads as a reorder.
      const complexFormAfter: IEntry = {...complexFormEntry, components: [siblingCfc]};
      const componentAfter: IEntry = {...componentEntry, complexForms: []};
      const deletedCfc = {...cfc, deletedAt: '2026-06-30T12:00:00Z'} as unknown as IComplexFormComponent;
      return ctx({entityType: 'ComplexFormComponent', previousSnapshot: cfc, snapshot: deletedCfc, affectedEntries: [complexFormAfter, componentAfter]});
    })()},
    {label: 'Complex form component — swapped (endpoint change)', context: (() => {
      // Update: same cfc.id but the componentEntryId (and headword) changed.
      const swappedCfc = {...cfc, componentEntryId: '00000000-0000-0000-0000-0000000000ee', componentHeadword: 'chithaphwi'} as unknown as IComplexFormComponent;
      const complexFormAfter: IEntry = {...complexFormEntry, components: [swappedCfc, siblingCfc]};
      const newComponentEntry: IEntry = {...componentEntry, id: swappedCfc.componentEntryId, lexemeForm: {seh: 'chithaphwi'}, complexForms: [swappedCfc]};
      return ctx({entityType: 'ComplexFormComponent', previousSnapshot: cfc, snapshot: swappedCfc, affectedEntries: [complexFormAfter, newComponentEntry]});
    })()},
    {label: 'Complex form component — reordered', context: (() => {
      // Reorder: same cfc.id, same endpoints, only .order changed. mwala moves from pos 1 → pos 3.
      const reorderedCfc = {...cfc, order: 3} as unknown as IComplexFormComponent;
      const thirdCfc = {
        id: '00000000-0000-0000-0000-0000000000ff',
        complexFormEntryId: complexFormId,
        componentEntryId: '00000000-0000-0000-0000-0000000000ee',
        componentHeadword: 'chithaphwi',
        order: 2,
      } as unknown as IComplexFormComponent;
      const complexFormReordered: IEntry = {...complexFormEntry, components: [siblingCfc, thirdCfc, reorderedCfc]};
      return ctx({entityType: 'ComplexFormComponent', previousSnapshot: cfc, snapshot: reorderedCfc, affectedEntries: [complexFormReordered]});
    })()},
    {label: 'Part of speech — name edited', context: ctx({entityType: 'PartOfSpeech', previousSnapshot: posBefore, snapshot: posAfter})},
    {label: 'Semantic domain — name edited (keeps code)', context: ctx({entityType: 'SemanticDomain', previousSnapshot: semDomBefore, snapshot: semDomAfter})},
    {label: 'Remote resource (audio player)', context: ctx({entityType: 'RemoteResource', snapshot: {id: '00000000-0000-0000-0000-0000000000f0'} as unknown as IObjectWithId})},
    {label: 'No preview available', context: ctx({entityType: 'Unknown'})},
  ];
</script>

<Story name="Change previews">
  {#snippet template()}
    <div class="space-y-6 max-w-3xl p-2">
      {#each cases as testCase (testCase.label)}
        <div>
          <div class="font-bold text-sm mb-1 text-muted-foreground">{testCase.label}</div>
          <div class="border rounded p-3">
            <ActivityItemChangePreview context={testCase.context} />
          </div>
        </div>
      {/each}
    </div>
  {/snippet}
</Story>

<Story name="Created entry (collapsed)">
  {#snippet template()}
    <div class="max-w-3xl border rounded p-3">
      <CollapsedEntryDiff entry={structuredClone(allWsEntry)} />
    </div>
  {/snippet}
</Story>

<Story name="Collapsed entry in activity grid (repro)">
  {#snippet template()}
    <div style="height: 640px;" class="border overflow-hidden">
      <!-- Mirror ActivityView's outer grid exactly (post-fix: `auto 1fr` rows) -->
      <div
        class="h-full m-4 grid gap-x-6 gap-y-1 overflow-hidden"
        style="grid-template-rows: auto 1fr; grid-template-columns: minmax(8rem,25%) minmax(0,2fr)">
        <div class="flex flex-wrap items-center gap-2 bg-muted/40 rounded p-2">Filter bar</div>
        <div class="overflow-hidden row-start-2 relative bg-muted/20 rounded p-2 space-y-2 text-sm">
          <div class="p-2 bg-muted rounded">Commit A · snappy — CreateEntryChange, CreateSenseChange</div>
          <div class="p-2 bg-muted rounded">Commit B · Set Word (seh) to fooo</div>
          <div class="p-2 bg-muted rounded">Commit C · Added semantic domain 1.1 Sky</div>
          <div class="p-2 bg-muted rounded">Commit D · Added component</div>
        </div>
        <div class="sub-grid row-span-2 col-start-2 grid gap-2 grid-rows-[auto_1fr] h-full min-w-0 min-h-0">
          <div class="text-sm">Author: Demo – (2 changes)<div class="text-xs text-muted-foreground">CreateEntryChange, CreateSenseChange</div></div>
          <div class="overflow-auto border rounded p-3 min-w-0 min-h-0">
            <CollapsedEntryDiff entry={structuredClone(allWsEntry)} />
          </div>
        </div>
      </div>
    </div>
  {/snippet}
</Story>
