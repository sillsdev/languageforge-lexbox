<script lang="ts">
  import { EditorGrid, EditorField } from '$lib/components/editor';
  import MultiSelect from '$lib/components/field-editors/multi-select.svelte';
  import { ResizablePaneGroup, ResizablePane, ResizableHandle } from '$lib/components/ui/resizable';
  import { Switch } from '$lib/components/ui/switch';
  import { Tabs, TabsList, TabsTrigger } from '$lib/components/ui/tabs';
  import { fieldData } from '$lib/entry-editor/field-data';
  import { t } from 'svelte-i18n-lingui';

  const allDomains = $state<{label: string}[]>([
    { label: 'fruit' }, { label: 'tree' }, { label: 'stars' }, { label: 'earth' },
  ]);
  for (let i = 0; i < 100; i++) {
    allDomains.push({
      label: allDomains[Math.floor(Math.random() * allDomains.length)].label + '-' + i
    });
  }
  allDomains.sort((a, b) => a.label.localeCompare(b.label));

  function randomSemanticDomainSorter() {
    return Math.random() - 0.5;
  }

  let selectedDomains = $state([allDomains[0], allDomains[80]]);

  let semanticDomainOrder = $state<'selectionOrder' | 'optionOrder' | 'randomOrder'>('selectionOrder');
  const sortSemanticDomainValuesBy = $derived(semanticDomainOrder === 'randomOrder'
    ? randomSemanticDomainSorter
    : semanticDomainOrder);
  let semanticDomainsReadonly = $state(false);
</script>

<ResizablePaneGroup direction="horizontal">
  <ResizablePane class="!overflow-visible" defaultSize={100}>
    <div class="@container my-4 border px-4 py-8 relative z-0">
      <div class="breakpoint-marker w-[32rem] text-orange-600">@lg</div>
      <div class="breakpoint-marker w-[48rem] text-green-600">@3xl</div>
      <EditorGrid>
        <EditorField.Root>
          <EditorField.Title liteName={$t`Order of semantic domains`} classicName={$t`Order of semantic domains`} />
          <EditorField.Body class="grid-cols-1">
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
          </EditorField.Body>
        </EditorField.Root>
        <EditorField.Root>
          <EditorField.Title liteName={$t`Readonly`} classicName={$t`Readonly`} />
          <EditorField.Body class="grid-cols-1">
            <Switch bind:checked={semanticDomainsReadonly} />
          </EditorField.Body>
        </EditorField.Root>
        <EditorField.Root>
          <EditorField.Title
            liteName={$t`Semantic domains`}
            classicName={$t`Semantic domains`}
            helpId={fieldData.semanticDomains.helpId}
          />
          <EditorField.Body>
            <div class="col-span-full">
              <MultiSelect
                readonly={semanticDomainsReadonly}
                bind:values={() => selectedDomains, (newValues) => (selectedDomains = newValues)}
                idSelector="label"
                labelSelector={(item) => item.label}
                sortValuesBy={sortSemanticDomainValuesBy}
                drawerTitle={$t`Semantic domains`}
                filterPlaceholder={$t`Filter semantic domains...`}
                placeholder={$t`🤷 nothing here`}
                emptyResultsPlaceholder={$t`Looked hard, found nothing`}
                options={allDomains}
              ></MultiSelect>
            </div>
          </EditorField.Body>
        </EditorField.Root>
      </EditorGrid>
    </div>
  </ResizablePane>
  <!-- looks cool 🤷 https://github.com/huntabyte/shadcn-svelte/blob/bcbe10a4f65d244a19fb98ffb6a71d929d9603bc/sites/docs/src/lib/components/docs/block-preview.svelte#L65 -->
  <ResizableHandle
    class="after:bg-border relative w-3 bg-transparent p-0 after:absolute after:right-0 after:top-1/2 after:h-8 after:w-[6px] after:-translate-y-1/2 after:translate-x-[-1px] after:rounded-full after:transition-all after:hover:h-10"
  />
  <ResizablePane></ResizablePane>
</ResizablePaneGroup>

<style lang="postcss">
  .breakpoint-marker {
    @apply absolute h-full top-0 border-r-current border-r -z-10 text-right pr-2;
  }
</style>
