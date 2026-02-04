<script lang="ts">
  import * as Editor from '$lib/components/editor';
  import MultiWsInput from '$lib/components/field-editors/multi-ws-input.svelte';
  import MultiSelect from '$lib/components/field-editors/multi-select.svelte';
  import Select from '$lib/components/field-editors/select.svelte';
  import LcmRichTextEditor from '$lib/components/lcm-rich-text-editor/lcm-rich-text-editor.svelte';
  import { ResizablePaneGroup, ResizablePane, ResizableHandle } from '$lib/components/ui/resizable';
  import { Switch } from '$lib/components/ui/switch';
  import { Tabs, TabsList, TabsTrigger } from '$lib/components/ui/tabs';
  import {writingSystems} from '$project/demo/demo-entry-data';
  import {type IMultiString} from '$lib/dotnet-types';
  import {type IRichString} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IRichString';
  import { fieldData } from '$lib/entry-editor/field-data';
  import ViewPicker from '../../project/browse/EditorViewOptions.svelte';
  import WsInput from '$lib/components/field-editors/ws-input.svelte';
  import {vt} from '$lib/views/view-text';

  const allDomains = [
    { label: 'fruit' }, { label: 'tree' }, { label: 'stars' }, { label: 'earth' },
  ].map(domain => Object.freeze(domain));
  for (let i = 0; i < 100; i++) {
    allDomains.push({
      label: allDomains[Math.floor(Math.random() * allDomains.length)].label + '-' + i
    });
  }
  allDomains.forEach(Object.freeze);
  allDomains.sort((a, b) => a.label.localeCompare(b.label));
  const readonlyDomains = Object.freeze(allDomains);

  function randomSemanticDomainSorter() {
    return Math.random() - 0.5;
  }

  const selectedDomains = [allDomains[0], allDomains[80]];

  let semanticDomainOrder = $state<'selectionOrder' | 'optionOrder' | 'randomOrder'>('selectionOrder');
  const sortSemanticDomainValuesBy = $derived(semanticDomainOrder === 'randomOrder'
    ? randomSemanticDomainSorter
    : semanticDomainOrder);
  let editorReadonly = $state(false);

  const note: IRichString = {
    spans: [
      { text: 'en note', ws: 'en' },
      { text: 'fr note', ws: 'fr' },
    ]
  };

  const partsOfSpeech = Object.freeze([
    { label: 'Noun' },
    { label: 'Verb' },
    { label: 'Adjective' },
    { label: 'Adverb' },
  ] as const);
  partsOfSpeech.forEach(Object.freeze);
  type PartOfSpeech = typeof partsOfSpeech[number];

  const partOfSpeech: PartOfSpeech = { label: 'Adjective' };

  const vernacularWs = Object.freeze(writingSystems.vernacular);
  const analysisWs = Object.freeze(writingSystems.analysis);

  const word: IMultiString = {
    'seh': 'word in seh',
  } satisfies Partial<Record<typeof vernacularWs[number]['wsId'], string>>;

  const entry = $state({
    word,
    selectedDomains,
    note,
    partOfSpeech,
    reference: 'Shakespeare',
  });
  function onChange(field: string) {
    console.log('changed', field);
  }
</script>

<!-- eslint-disable svelte/no-useless-mustaches - This is a sandbox, we don't need translations -->
<ResizablePaneGroup direction="horizontal">
  <ResizablePane class="!overflow-visible" defaultSize={75}>
    <Editor.Root class="my-4 border px-4 py-8 relative z-0">
      <div class="absolute top-4 right-4">
        <ViewPicker />
      </div>
      <!-- See sizes here: https://github.com/tailwindlabs/tailwindcss-container-queries?tab=readme-ov-file#configuration -->
      <div class="breakpoint-marker w-[32rem] text-orange-600">@lg</div>
      <div class="breakpoint-marker w-[48rem] text-green-600">@3xl</div>
      <Editor.Grid>
        <Editor.Field.Root>
          <Editor.Field.Title name="Readonly editor" />
          <Editor.Field.Body>
            <Switch bind:checked={editorReadonly} />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name={vt('Lexeme form', 'Word')} />
          <Editor.Field.Body class="grid-cols-subgrid">
            <MultiWsInput
              readonly={editorReadonly}
              bind:value={entry.word}
              onchange={() => onChange('word')}
              writingSystems={vernacularWs} />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Reference" />
          <Editor.Field.Body>
            <WsInput
              readonly={editorReadonly}
              bind:value={entry.reference}
              onchange={() => onChange('reference')}
              writingSystem={analysisWs[0]} />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name={'Order of semantic domains'} />
          <Editor.Field.Body>
            <Tabs bind:value={semanticDomainOrder} class="mb-1">
              <TabsList>
                <TabsTrigger value="selectionOrder">Selection</TabsTrigger>
                <TabsTrigger value="optionOrder">Option</TabsTrigger>
                <TabsTrigger value="randomOrder">Random</TabsTrigger>
              </TabsList>
            </Tabs>
            <p class="text-muted-foreground text-sm">
              Only comes into effect while editing, because we don't want to make any changes implicitly.
            </p>
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title
            name="Semantic domains"
            helpId={fieldData.semanticDomains.helpId}
          />
          <Editor.Field.Body>
            <MultiSelect
              readonly={editorReadonly}
              bind:values={() => entry.selectedDomains, (newValues) => (entry.selectedDomains = newValues)}
              idSelector="label"
              labelSelector={(item) => item.label}
              sortValuesBy={sortSemanticDomainValuesBy}
              drawerTitle={'Semantic domains'}
              filterPlaceholder={'Filter semantic domains...'}
              placeholder={'🤷 nothing here'}
              emptyResultsPlaceholder={'Looked hard, found nothing'}
              options={readonlyDomains}
            ></MultiSelect>
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title
            name="Note"
            helpId={fieldData.note.helpId}
          />
          <Editor.Field.Body>
            <LcmRichTextEditor bind:value={entry.note} normalWs="en" readonly={editorReadonly} onchange={() => onChange('note')} />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title
            name={{lite: 'Part of speech', classic: 'Grammatical info'}}
            helpId={fieldData.partOfSpeechId.helpId}
          />
          <Editor.Field.Body>
            <Select
              readonly={editorReadonly}
              bind:value={entry.partOfSpeech}
              idSelector="label"
              labelSelector={(item) => item.label}
              drawerTitle={'Part of speech'}
              filterPlaceholder={'Filter parts of speech...'}
              placeholder={'🤷 nothing here'}
              emptyResultsPlaceholder={'Looked hard, found nothing'}
              options={partsOfSpeech}
            ></Select>
          </Editor.Field.Body>
        </Editor.Field.Root>
      </Editor.Grid>
    </Editor.Root>
  </ResizablePane>
  <!-- looks cool 🤷 https://github.com/huntabyte/shadcn-svelte/blob/bcbe10a4f65d244a19fb98ffb6a71d929d9603bc/sites/docs/src/lib/components/docs/block-preview.svelte#L65 -->
  <ResizableHandle
    class="after:bg-border relative w-3 bg-transparent p-0 after:absolute after:right-0 after:top-1/2 after:h-8 after:w-[6px] after:-translate-y-1/2 after:translate-x-[-1px] after:rounded-full after:transition-all after:hover:h-10"
  />
  <ResizablePane>
    <pre class="overflow-x-auto text-sm text-muted-foreground bg-background relative">{JSON.stringify(entry, null, 2)}</pre>
  </ResizablePane>
</ResizablePaneGroup>

<style lang="postcss">
  @reference "../../app.css";
  .breakpoint-marker {
    @apply absolute h-full top-0 border-r-current border-r -z-10 text-right pr-2;
  }
</style>
