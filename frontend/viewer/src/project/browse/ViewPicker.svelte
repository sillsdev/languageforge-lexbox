<script lang="ts">
  import * as Popover from '$lib/components/ui/popover';
  import * as RadioGroup from '$lib/components/ui/radio-group';
  import NewTabLinkMarkdown from '$lib/markdown/NewTabLinkMarkdown.svelte';
  import {delay} from '$lib/utils/time';
  import {useViewService} from '$lib/views/view-service.svelte';
  import ManageCustomViewsButton from '$lib/views/custom/ManageCustomViewsButton.svelte';
  import Markdown from 'svelte-exmarkdown';
  import {t} from 'svelte-i18n-lingui';
  import {Label} from '$lib/components/ui/label';
  import {isCustomView} from '$lib/views/view-data';
  import {ViewBase} from '$lib/dotnet-types';

  let {onClose}: {onClose?: () => void} = $props();
  const viewService = useViewService();

  function getCurrentView() {
    return viewService.currentView.id;
  }
  function setCurrentView(id: string) {
    viewService.selectView(id);
  }

  let popoverContent = $state<HTMLElement | null>(null);
  // for some reason the popover is scrolled down when it opens
  async function onPopoverOpenChange(isOpen: boolean) {
    if (isOpen) {
      await delay(0);
      if (popoverContent) popoverContent.scrollTop = 0;
    }
  }
</script>

<Popover.Root onOpenChange={onPopoverOpenChange}>
  <Popover.InfoTrigger class="text-start w-fit max-md:mb-1">
    <h3 class="inline font-normal">{$t`View`}</h3>
  </Popover.InfoTrigger>
  <Popover.Content bind:ref={popoverContent} class="max-h-[40vh] overflow-y-auto text-sm flex flex-col gap-2">
    <div>
      <Markdown md={$t`The *FieldWorks Lite* view is designed for non-linguists and differs from the *FieldWorks Classic* view in the following ways:`} />
      <ul class="text-sm pl-4 list-disc mt-0.5 [&_strong]:font-semibold">
        <li><Markdown md={$t`**Simpler terminology** (e.g. *Word* instead of *Lexeme form*, *Meaning* instead of *Sense*)`} /></li>
        <li><Markdown md={$t`**Fewer fields** (e.g. hides *Complex form types*, *Literal meaning*)`} /></li>
        <li><Markdown md={$t`**Fewer morpheme types** (only *Root*, *Bound Root*, *Stem*, *Bound Stem*, *Particle*, *Phrase*, and *Discontiguous Phrase*)`} /></li>
      </ul>
    </div>
    <NewTabLinkMarkdown md={$t`The *FieldWorks Classic* view, on the other hand, is designed for users who are familiar with *[FieldWorks Language Explorer](https://software.sil.org/fieldworks/)*.`} />
  </Popover.Content>
</Popover.Root>

<RadioGroup.Root bind:value={getCurrentView, setCurrentView}>
  {#each viewService.views as view (view.id)}
    <Label class="flex items-center gap-4 md:gap-2 max-md:py-3">
      <RadioGroup.Item value={view.id} />
      <span>
        {view.name}
        {#if isCustomView(view)}
          <span class="text-muted-foreground">({view.base === ViewBase.FieldWorks ? 'Classic' : 'Lite'})</span>
        {/if}
      </span>
    </Label>
  {/each}
</RadioGroup.Root>

<div class="not-empty:mt-1">
  <ManageCustomViewsButton {onClose} />
</div>
