<script lang="ts">
  import { Icon } from '$lib/components/ui/icon';
  import * as Popover from '$lib/components/ui/popover';
  import * as RadioGroup from '$lib/components/ui/radio-group';
  import { buttonVariants } from '$lib/components/ui/button';
  import { t } from 'svelte-i18n-lingui';
  import {views} from '$lib/views/view-data';
  import {useCurrentView} from '$lib/views/view-service';
  import Label from '$lib/components/ui/label/label.svelte';
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
  <Popover.Content class="w-64 sm:mr-4">
    <div class="space-y-3">
      <h4 class="font-medium">{$t`Filter Options`}</h4>
      <RadioGroup.Root bind:value={getCurrentView, setCurrentView}>
        {#each views as view}
          <div class="flex items-center space-x-2">
            <RadioGroup.Item value={view.id} id={view.id} />
            <Label for={view.id}>
              {view.label}
            </Label>
          </div>
        {/each}
      </RadioGroup.Root>
    </div>
  </Popover.Content>
</Popover.Root>
