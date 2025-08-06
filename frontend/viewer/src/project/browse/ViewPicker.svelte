<script lang="ts">
  import * as RadioGroup from '$lib/components/ui/radio-group';
  import { t } from 'svelte-i18n-lingui';
  import { views } from '$lib/views/view-data';
  import { useCurrentView } from '$lib/views/view-service';
  import Switch from '$lib/components/ui/switch/switch.svelte';
  import ResponsivePopup from '$lib/components/responsive-popup/responsive-popup.svelte';
  import {Button} from '$lib/components/ui/button';
  import DevContent from '$lib/layout/DevContent.svelte';

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
</script>
<ResponsivePopup title={$t`View Configuration`}>
  {#snippet trigger({props})}
    <Button {...props} size="icon" variant="ghost" icon="i-mdi-layers" />
  {/snippet}
  <div class="space-y-2 md:space-y-4">
    <RadioGroup.Root bind:value={getCurrentView, setCurrentView}>
      <h3 class="font-normal max-md:mb-1">{$t`Field Labels`}</h3>
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
