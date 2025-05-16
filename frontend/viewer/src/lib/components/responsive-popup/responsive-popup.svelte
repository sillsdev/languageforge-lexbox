<script lang="ts">
  import { IsMobile } from '$lib/hooks/is-mobile.svelte';
  import * as Popover from '$lib/components/ui/popover';
  import * as Drawer from '$lib/components/ui/drawer';
  import { buttonVariants } from '$lib/components/ui/button';
  import { t } from 'svelte-i18n-lingui';
  import type {PopoverTriggerProps, WithChildren} from 'bits-ui';

  type TriggerSnippet = PopoverTriggerProps['child'];

  let { open = $bindable(false), children, title, trigger }: WithChildren<{ open?: boolean, title: string, trigger: TriggerSnippet }> = $props();
</script>

{#if !IsMobile.value}
  <Popover.Root bind:open>
    <Popover.Trigger child={trigger} />
    <Popover.Content class="w-64 sm:mr-4">
      <div class="space-y-3">
        <h3 class="font-medium">{title}</h3>
        {#if children}
          {@render children()}
        {/if}
      </div>
    </Popover.Content>
  </Popover.Root>
{:else}
  <Drawer.Root bind:open>
    <Drawer.Trigger child={trigger} />
    <Drawer.Content>
      <div class="mx-auto w-full max-w-sm p-4">
        <Drawer.Header>
          <Drawer.Title>{title}</Drawer.Title>
        </Drawer.Header>
        {#if children}
          {@render children()}
        {/if}
        <Drawer.Footer>
          <Drawer.Close class={buttonVariants({ variant: 'outline' })}>{$t`Close`}</Drawer.Close>
        </Drawer.Footer>
      </div>
    </Drawer.Content>
  </Drawer.Root>
{/if}
