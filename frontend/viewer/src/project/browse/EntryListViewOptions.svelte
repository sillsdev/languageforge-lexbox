<script lang="ts" module>
  export type EntryListViewMode = 'preview' | 'simple';
</script>

<script lang="ts">
  import { t } from 'svelte-i18n-lingui';
  import Switch from '$lib/components/ui/switch/switch.svelte';
  import ResponsivePopup from '$lib/components/responsive-popup/responsive-popup.svelte';
  import {Button} from '$lib/components/ui/button';
  import DevContent from '$lib/layout/DevContent.svelte';
  import ViewPicker from './ViewPicker.svelte';
  import * as Tabs from '$lib/components/ui/tabs';
  import {Icon} from '$lib/components/ui/icon';

  let {
    entryMode = $bindable('simple'),
    readonly = $bindable(false),
  }: {
    entryMode?: EntryListViewMode
    readonly?: boolean
  } = $props();

  let open = $state(false);
</script>

<ResponsivePopup bind:open>
  {#snippet trigger({props})}
    <Button {...props} size="xs-icon" variant="ghost" icon="i-mdi-layers" />
  {/snippet}
  <div class="space-y-3 md:space-y-4">

    <div class="grid gap-2">
      <h3>{$t`List mode`}</h3>
      <Tabs.Root bind:value={entryMode} class="text-center">
        <Tabs.List onkeydown={(e) => {if (e.key === 'Enter') open = false}}>
          <Tabs.Trigger value="simple" onclick={() => open = false}>
            <Icon icon="i-mdi-format-list-bulleted-square" class="mr-1"/>
            {$t`Simple`}
          </Tabs.Trigger>
          <Tabs.Trigger value="preview" onclick={() => open = false}>
            <Icon icon="i-mdi-format-list-text" class="mr-1"/>
            {$t`Preview`}
          </Tabs.Trigger>
        </Tabs.List>
      </Tabs.Root>
    </div>

    <ViewPicker />

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
