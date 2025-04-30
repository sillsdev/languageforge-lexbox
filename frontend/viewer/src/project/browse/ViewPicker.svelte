<script lang="ts">
  import { Icon } from '$lib/components/ui/icon';
  import * as RadioGroup from '$lib/components/ui/radio-group';
  import { t } from 'svelte-i18n-lingui';
  import { views } from '$lib/views/view-data';
  import { useCurrentView, useViewSettings } from '$lib/views/view-service';
  import Label from '$lib/components/ui/label/label.svelte';
  import Switch from '$lib/components/ui/switch/switch.svelte';
  import ResponsivePopup from '$lib/components/responsive-popup/responsive-popup.svelte';
  let {dictionaryPreview = $bindable('show')}: {
    dictionaryPreview?: 'show' | 'hide' | 'sticky'
  } = $props();
  const currentView = useCurrentView();
  const viewSettings = useViewSettings();
  function getCurrentView() {
    return $currentView.id;
  }
  function setCurrentView(id: string) {
    currentView.set(views.find((v) => v.id === id) ?? views[0]);
  }
</script>
<ResponsivePopup title={$t`View Configuration`}>
  {#snippet trigger()}
    <Icon icon="i-mdi-layers" />
  {/snippet}
  <div class="space-y-6">
    <RadioGroup.Root bind:value={getCurrentView, setCurrentView}>
      <h3 class="font-normal">{$t`Field Labels`}</h3>
      {#each views as view}
        <div class="flex items-center space-x-2">
          <RadioGroup.Item value={view.id} id={view.id} />
          <Label for={view.id}>
            {view.label}
          </Label>
        </div>
      {/each}
    </RadioGroup.Root>
    <div class="space-y-2">
      <h3 class="font-normal mb-2">{$t`View Settings`}</h3>
      <div class="flex items-center space-x-2">
        <Switch
          id="showEmptyFields"
          bind:checked={
            () => $viewSettings.showEmptyFields,
            (value) => ($viewSettings = { ...$viewSettings, showEmptyFields: value })
          }
        />
        <Label for="showEmptyFields">{$t`Show Empty Fields`}</Label>
      </div>
      <RadioGroup.Root bind:value={dictionaryPreview}>
        <h3 class="font-normal">{$t`Dictionary Preview`}</h3>
        <div class="flex items-center space-x-2">
          <RadioGroup.Item value="show" id="show"/>
          <Label for="show">
            {$t`Show`}
          </Label>
        </div>
        <div class="flex items-center space-x-2">
          <RadioGroup.Item value="hide" id="hide"/>
          <Label for="hide">
            {$t`Hide`}
          </Label>
        </div>
        <div class="flex items-center space-x-2">
          <RadioGroup.Item value="sticky" id="sticky"/>
          <Label for="sticky">
            {$t`Pinned`}
          </Label>
        </div>
      </RadioGroup.Root>
    </div>
  </div>
</ResponsivePopup>
