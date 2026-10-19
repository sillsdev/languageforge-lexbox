<script lang="ts">
  import * as RadioGroup from '$lib/components/ui/radio-group';
  import {t} from 'svelte-i18n-lingui';
  import ResponsivePopup from '$lib/components/responsive-popup/responsive-popup.svelte';
  import {Button} from '$lib/components/ui/button';
  import ViewPicker from './ViewPicker.svelte';

  let open = $state(false);
  let {
    dictionaryPreview = $bindable('show'),
  }: {
    dictionaryPreview?: 'show' | 'hide' | 'sticky'
  } = $props();
</script>
<ResponsivePopup bind:open>
  {#snippet trigger({props})}
    <Button {...props} size="icon" variant="ghost" icon="i-mdi-layers" />
  {/snippet}
  <div class="space-y-2 md:space-y-4">
    <h3 class="font-normal max-md:mb-1">{$t`Dictionary Preview`}</h3>
    <RadioGroup.Root bind:value={dictionaryPreview}>
      <RadioGroup.Option value="show">{$t`Show`}</RadioGroup.Option>
      <RadioGroup.Option value="hide">{$t`Hide`}</RadioGroup.Option>
      <RadioGroup.Option value="sticky">{$t`Pinned`}</RadioGroup.Option>
    </RadioGroup.Root>
    <ViewPicker onClose={() => open = false} />
  </div>
</ResponsivePopup>
