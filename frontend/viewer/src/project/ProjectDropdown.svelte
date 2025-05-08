<script lang="ts">
  import * as Command from '$lib/components/ui/command';
  import * as Popover from '$lib/components/ui/popover';
  import { Button } from '$lib/components/ui/button';
  import { Icon } from '$lib/components/ui/icon';
  import { cn } from '$lib/utils';
  import {t} from 'svelte-i18n-lingui';
  import {tick} from 'svelte';
  import flexLogo from '$lib/assets/flex-logo.png';
  import {useProjectsService} from '$lib/services/service-provider';
  import {resource} from 'runed';
  import type {IProjectModel} from '$lib/dotnet-types';

  let { projectName, onSelect } = $props<{
    projectName: string;
    onSelect: (project: IProjectModel) => void;
  }>();
  const projectsService = useProjectsService();
  const projectsResource = resource(() => projectsService, async (projectsService) => {
      const projects = await projectsService.localProjects();
      return projects.flatMap((project) => {
        if (project.fwdata && project.crdt) {
          return [
            { ...project, fwdata: false },
            { ...project, crdt: false }
          ];
        }
        return [project];
      })
      .sort((a, b) => a.name.localeCompare(b.name));
  }, {lazy: true});

  let open = $state(false);
  let triggerRef = $state<HTMLButtonElement>(null!);


  // Simulate loading delay
  function handleOpen(isOpen: boolean) {
    open = isOpen;
    if (isOpen && !projectsResource.current) {
      void projectsResource.refetch();
    }
  }

  function handleSelect(project: IProjectModel) {
    onSelect(project);
    closeAndFocusTrigger();
  }

  // We want to refocus the trigger button when the user selects
  // an item from the list so users can continue navigating the
  // rest of the form with the keyboard.
  function closeAndFocusTrigger() {
    open = false;
    void tick().then(() => {
      triggerRef?.focus();
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
        {#if projectsResource.loading}
          <Command.Loading>
            <div class="flex items-center justify-center p-4">
              <Icon icon="i-mdi-loading" class="size-4 animate-spin" />
              <span class="ml-2">{$t`Loading Dictionaries...`}</span>
            </div>
          </Command.Loading>
        {:else}
          {#each projectsResource.current ?? [] as project}
            <Command.Item
              value={project.name}
              onSelect={() => handleSelect(project)}
              class={cn('cursor-pointer', project.name === projectName && 'bg-secondary')}
            >
              {#if project.fwdata}
                <img src={flexLogo} alt={$t`FieldWorks logo`} class="h-6 shrink-0"/>
                {:else}
                <Icon icon="i-mdi-book-edit-outline"/>
              {/if}
              {project.name}
            </Command.Item>
          {/each}
        {/if}
      </Command.List>
    </Command.Root>
  </Popover.Content>
</Popover.Root>
