<script lang="ts">
  import * as Command from '$lib/components/ui/command';
  import * as Popover from '$lib/components/ui/popover';
  import { Button } from '$lib/components/ui/button';
  import { Icon } from '$lib/components/ui/icon';
  import { cn } from '$lib/utils';
  import {t} from 'svelte-i18n-lingui';
  import {tick} from 'svelte';

  let { projectName, onSelect } = $props<{
    projectName: string;
    onSelect: (projectName: string) => void;
  }>();

  // Mock project list - this would be replaced with actual API call later
  const mockProjects = [
    { id: '1', name: 'Project Alpha' },
    { id: '2', name: 'Project Beta' },
    { id: '3', name: 'Project Gamma' },
    { id: '4', name: 'Project Delta' },
    { id: '5', name: 'Project Epsilon' },
  ];

  let open = $state(false);
  let loading = $state(false);
  let triggerRef = $state<HTMLButtonElement>(null!);

  // Simulate loading delay
  function handleOpen(isOpen: boolean) {
    open = isOpen;
    if (isOpen) {
      loading = true;
      setTimeout(() => {
        loading = false;
      }, 1000);
    }
  }

  function handleSelect(project: { id: string; name: string }) {
    onSelect(project.name);
    open = false;
    closeAndFocusTrigger();
  }

  // We want to refocus the trigger button when the user selects
  // an item from the list so users can continue navigating the
  // rest of the form with the keyboard.
  function closeAndFocusTrigger() {
    open = false;
    void tick().then(() => {
      triggerRef.focus();
    });
  }
</script>

<Popover.Root bind:open onOpenChange={handleOpen}>
  <Popover.Trigger bind:ref={triggerRef}>
    {#snippet child({ props })}
      <Button
        variant="ghost"
        role="combobox"
        aria-expanded={open}
        class="w-full justify-between overflow-hidden gap-0"
        {...props}
      >
        <div class="flex items-center gap-2 overflow-hidden">
          <Icon icon="i-mdi-book" class="size-4" />
          <span class="x-ellipsis">
            {projectName}
          </span>
        </div>
        <Icon
          icon="i-mdi-chevron-down"
          class={cn('ml-2 size-4 shrink-0 opacity-50', open && 'rotate-180')}
        />
      </Button>
    {/snippet}
  </Popover.Trigger>
  <Popover.Content class="w-full p-0 min-w-56" align="start">
    <Command.Root>
      <Command.Input placeholder={$t`Search Dictionaries`} />
      <Command.List>
        <Command.Empty>{$t`No Dictionaries found`}</Command.Empty>
        {#if loading}
          <Command.Loading>
            <div class="flex items-center justify-center p-4">
              <Icon icon="i-mdi-loading" class="size-4 animate-spin" />
              <span class="ml-2">{$t`Loading Dictionaries...`}</span>
            </div>
          </Command.Loading>
        {:else}
          {#each mockProjects as project}
            <Command.Item
              value={project.name}
              onSelect={() => handleSelect(project)}
              class="cursor-pointer"
            >
              <Icon
                icon="i-mdi-check"
                class={cn(
                  'mr-2 size-4',
                  projectName === project.name ? 'opacity-100' : 'opacity-0'
                )}
              />
              {project.name}
            </Command.Item>
          {/each}
        {/if}
      </Command.List>
    </Command.Root>
  </Popover.Content>
</Popover.Root>
