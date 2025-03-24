<script lang="ts">
  import { buttonVariants } from '$lib/components/ui/button';
  import * as Collapsible from '$lib/components/ui/collapsible';
  import * as Sidebar from '$lib/components/ui/sidebar';
  import { Icon } from '$lib/components/ui/icon';
  import { Input } from '$lib/components/ui/input';
  import { t } from 'svelte-i18n-lingui';
  import {Switch} from '$lib/components/ui/switch';
  import {Label} from '$lib/components/ui/label';

  let {
    search = $bindable(),
    gridifyFilter = $bindable(undefined),
  }: {
    search: string;
    gridifyFilter: string | undefined;
  } = $props();
  let missingExamples = $state(false);
  let missingSenses = $state(false);
  let missingPartOfSpeech = $state(false);
  let missingSemanticDomains = $state(false);
  $effect(() => {
    let newFilter: string[] = [];
    if (missingExamples) {
      newFilter.push('Senses.ExampleSentences=null')
    }
    if (missingSenses) {
      newFilter.push('Senses=null')
    }
    if (missingPartOfSpeech) {
      newFilter.push('Senses.PartOfSpeechId=null')
    }
    if (missingSemanticDomains) {
      newFilter.push('Senses.SemanticDomains=null')
    }
    gridifyFilter = newFilter.join(', ');
  });
</script>

<Collapsible.Root>
  <div class="relative">
    <Sidebar.Trigger icon="i-mdi-menu" class="absolute top-1/2 -translate-y-1/2 left-3 size-5" />
    <Input bind:value={search} placeholder={$t`Filter`} autofocus class="pl-10 pr-10" />
    <Collapsible.Trigger
      class={buttonVariants({ variant: 'ghost', size: 'sm', class: 'absolute top-1/2 -translate-y-1/2 right-1' })}>
      <Icon icon={gridifyFilter ? 'i-mdi-filter' : 'i-mdi-filter-outline'} class="size-5" />
    </Collapsible.Trigger>
  </div>
  <Collapsible.Content class="my-4 space-y-2">
    <div class="flex items-center gap-2">
      <Switch id="missingExamples" bind:checked={missingExamples} />
      <Label for="missingExamples">{$t`Missing Examples`}</Label>
    </div>
    <div class="flex items-center gap-2">
      <Switch id="missingSenses" bind:checked={missingSenses} />
      <Label for="missingSenses">{$t`Missing Senses`}</Label>
    </div>
    <div class="flex items-center gap-2">
      <Switch id="missingPartOfSpeech" bind:checked={missingPartOfSpeech} />
      <Label for="missingPartOfSpeech">{$t`Missing Part of Speech`}</Label>
    </div>
    <div class="flex items-center gap-2">
      <Switch id="missingSemanticDomains" bind:checked={missingSemanticDomains} />
      <Label for="missingSemanticDomains">{$t`Missing Semantic Domains`}</Label>
    </div>
  </Collapsible.Content>
</Collapsible.Root>
