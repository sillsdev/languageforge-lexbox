<script lang="ts">
  import * as Popover from '$lib/components/ui/popover';
  import * as RadioGroup from '$lib/components/ui/radio-group';
  import NewTabLinKMarkdown from '$lib/markdown/NewTabLinKMarkdown.svelte';
  import {delay} from '$lib/utils/time';
  import {views} from '$lib/views/view-data';
  import {useCurrentView} from '$lib/views/view-service';
  import Markdown from 'svelte-exmarkdown';
  import {t} from 'svelte-i18n-lingui';

  const currentView = useCurrentView();
  function getCurrentView() {
    return $currentView.id;
  }
  function setCurrentView(id: string) {
    currentView.set(views.find((v) => v.id === id) ?? views[0]);
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
<RadioGroup.Root bind:value={getCurrentView, setCurrentView}>
  <Popover.Root onOpenChange={onPopoverOpenChange}>
    <Popover.InfoTrigger class="text-start w-fit">
      <h3 class="inline font-normal max-md:mb-1">{$t`View`}</h3>
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
      <NewTabLinKMarkdown md={$t`The *FieldWorks Classic* view, on the other hand, is designed for users who are familiar with *[FieldWorks Language Explorer](https://software.sil.org/fieldworks/)*.`} />
    </Popover.Content>
  </Popover.Root>
  {#each views as view (view.id)}
    <RadioGroup.Item value={view.id} label={view.label} />
  {/each}
</RadioGroup.Root>
