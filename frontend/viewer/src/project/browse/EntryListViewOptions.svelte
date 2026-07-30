<script lang="ts" module>
  export type EntryListViewMode = 'preview' | 'simple';
</script>

<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import ResponsivePopup from '$lib/components/responsive-popup/responsive-popup.svelte';
  import {Button} from '$lib/components/ui/button';
  import ViewPicker from './ViewPicker.svelte';
  import * as Tabs from '$lib/components/ui/tabs';
  import {Icon} from '$lib/components/ui/icon';

  let {
    entryMode = $bindable('simple'),
  }: {
    entryMode?: EntryListViewMode
  } = $props();

  let open = $state(false);
</script>

<ResponsivePopup bind:open>
  {#snippet trigger({props})}
    <Button {...props} size="icon-xs" variant="ghost" icon="i-mdi-layers" />
  {/snippet}
  <div class="space-y-3 md:space-y-4">

    <div class="grid gap-2">
      <h3>{$t`List mode`}</h3>
      <Tabs.Root bind:value={entryMode} class="text-center">
        <Tabs.List class="w-full" onkeydown={(e) => {if (e.key === 'Enter') open = false}}>
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

    <ViewPicker onClose={() => open = false} />
  </div>
</ResponsivePopup>
