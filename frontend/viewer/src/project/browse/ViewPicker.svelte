<script lang="ts">
  import { Icon } from '$lib/components/ui/icon';
  import * as Popover from '$lib/components/ui/popover';
  import * as RadioGroup from '$lib/components/ui/radio-group';
  import { buttonVariants } from '$lib/components/ui/button';
  import { t } from 'svelte-i18n-lingui';
  import {views} from '$lib/views/view-data';
  import {useCurrentView} from '$lib/views/view-service';
  const currentView = useCurrentView();
  function getCurrentView() {
    return $currentView.id;
  }
  function setCurrentView(id: string) {
    currentView.set(views.find(v => v.id === id) ?? views[0]);
  }
</script>

<Popover.Root>
  <Popover.Trigger class={buttonVariants({ variant: 'ghost', size: 'sm', class: 'float-right' })}>
    <Icon icon="i-mdi-layers" class="size-5" />
  </Popover.Trigger>
  <Popover.Content class="w-64">
    <div class="space-y-3">
      <h4 class="font-medium">{$t`Filter Options`}</h4>
      <RadioGroup.Root bind:value={getCurrentView, setCurrentView}>
        {#each views as view}
          <div class="flex items-center space-x-2">
            <RadioGroup.Item value={view.id} id={view.id} />
            <label for={view.id} class="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70">
              {view.label}
            </label>
          </div>
        {/each}
      </RadioGroup.Root>
    </div>
  </Popover.Content>
</Popover.Root>
