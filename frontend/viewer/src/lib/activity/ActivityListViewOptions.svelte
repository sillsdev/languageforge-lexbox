<script lang="ts" module>
  export type ActivityListViewMode = 'simple' | 'detailed';
</script>

<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import ResponsivePopup from '$lib/components/responsive-popup/responsive-popup.svelte';
  import {Button} from '$lib/components/ui/button';
  import * as Tabs from '$lib/components/ui/tabs';
  import {Icon} from '$lib/components/ui/icon';

  let {mode = $bindable('simple')}: {mode?: ActivityListViewMode} = $props();

  let open = $state(false);
</script>

<ResponsivePopup bind:open>
  {#snippet trigger({props})}
    <Button {...props} size="icon-xs" variant="ghost" icon="i-mdi-layers" title={$t`List mode`} />
  {/snippet}
  <div class="grid gap-2">
    <h3>{$t`List mode`}</h3>
    <Tabs.Root bind:value={mode} class="text-center">
      <Tabs.List class="w-full" onkeydown={(e) => {if (e.key === 'Enter') open = false}}>
        <Tabs.Trigger value="simple" onclick={() => open = false}>
          <Icon icon="i-mdi-format-list-bulleted-square" class="mr-1" />
          {$t`Simple`}
        </Tabs.Trigger>
        <Tabs.Trigger value="detailed" onclick={() => open = false}>
          <Icon icon="i-mdi-format-list-text" class="mr-1" />
          {$t`Detailed`}
        </Tabs.Trigger>
      </Tabs.List>
    </Tabs.Root>
  </div>
</ResponsivePopup>
