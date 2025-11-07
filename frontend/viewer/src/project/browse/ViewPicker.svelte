<script lang="ts">
  import * as RadioGroup from '$lib/components/ui/radio-group';
  import { t } from 'svelte-i18n-lingui';
  import { views } from '$lib/views/view-data';
  import { useCurrentView } from '$lib/views/view-service';
  import Switch from '$lib/components/ui/switch/switch.svelte';
  import ResponsivePopup from '$lib/components/responsive-popup/responsive-popup.svelte';
  import {Button} from '$lib/components/ui/button';
  import DevContent from '$lib/layout/DevContent.svelte';
  import * as Popover from '$lib/components/ui/popover';
  import Markdown from 'svelte-exmarkdown';
  import NewTabLinKMarkdown from '$lib/markdown/NewTabLinKMarkdown.svelte';
  import {delay} from '$lib/utils/time';

  let {
    dictionaryPreview = $bindable('show'),
    readonly = $bindable(false),
  }: {
    dictionaryPreview?: 'show' | 'hide' | 'sticky'
    readonly?: boolean
  } = $props();
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
<ResponsivePopup title={$t`View Configuration`}>
  {#snippet trigger({props})}
    <Button {...props} size="icon" variant="ghost" icon="i-mdi-layers" />
  {/snippet}
  <div class="space-y-2 md:space-y-4">
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
    <RadioGroup.Root bind:value={dictionaryPreview}>
      <h3 class="font-normal max-md:mb-1">{$t`Dictionary Preview`}</h3>
      <RadioGroup.Item value="show" label={$t`Show`} />
      <RadioGroup.Item value="hide" label={$t`Hide`}/>
      <RadioGroup.Item value="sticky" label={$t`Pinned`}/>
    </RadioGroup.Root>
    <DevContent>
      <div class="space-y-2">
        <h3 class="font-normal">Dev Options</h3>
        <div class="flex items-center space-x-2">
          <Switch bind:checked={readonly} label="Readonly" />
        </div>
      </div>
    </DevContent>
  </div>
</ResponsivePopup>
