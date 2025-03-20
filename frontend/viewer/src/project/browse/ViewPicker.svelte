<script lang="ts">
  import { Icon } from '$lib/components/ui/icon';
  import * as Popover from '$lib/components/ui/popover';
  import * as RadioGroup from '$lib/components/ui/radio-group';
  import * as Drawer from '$lib/components/ui/drawer';
  import { buttonVariants, Button } from '$lib/components/ui/button';
  import { t } from 'svelte-i18n-lingui';
  import { views } from '$lib/views/view-data';
  import { useCurrentView, useViewSettings } from '$lib/views/view-service';
  import Label from '$lib/components/ui/label/label.svelte';
  import { IsMobile } from '$lib/hooks/is-mobile.svelte';
  import Switch from '$lib/components/ui/switch/switch.svelte';
  const currentView = useCurrentView();
  const viewSettings = useViewSettings();
  const isMobile = new IsMobile();
  function getCurrentView() {
    return $currentView.id;
  }
  function setCurrentView(id: string) {
    currentView.set(views.find((v) => v.id === id) ?? views[0]);
  }
  let open = $state(false);
</script>

{#snippet content()}
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
    <div>
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
    </div>
  </div>
{/snippet}

{#if !isMobile.current}
  <Popover.Root bind:open>
    <Popover.Trigger class={buttonVariants({ variant: 'ghost', size: 'sm', class: 'float-right' })}>
      <Icon icon="i-mdi-layers" class="size-5" />
    </Popover.Trigger>
    <Popover.Content class="w-64 sm:mr-4">
      <div class="space-y-3">
        <h3 class="font-medium">{$t`View Configuration`}</h3>
        {@render content()}
      </div>
    </Popover.Content>
  </Popover.Root>
{:else}
  <Drawer.Root bind:open>
    <Drawer.Trigger class={buttonVariants({ variant: 'ghost', size: 'sm', class: 'float-right' })}>
      <Icon icon="i-mdi-layers" class="size-5" />
    </Drawer.Trigger>
    <Drawer.Content>
      <div class="mx-auto w-full max-w-sm">
        <Drawer.Header>
          <Drawer.Title>{$t`View Configuration`}</Drawer.Title>
        </Drawer.Header>
        {@render content()}
        <Drawer.Footer>
          <Drawer.Close class={buttonVariants({ variant: 'outline' })}>{$t`Close`}</Drawer.Close>
        </Drawer.Footer>
      </div>
    </Drawer.Content>
  </Drawer.Root>
{/if}
