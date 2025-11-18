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
  import ProjectTitle from '../home/ProjectTitle.svelte';
  import {useProjectContext} from '$project/project-context.svelte';

  let { onSelect } = $props<{
    onSelect: (project: IProjectModel) => void;
  }>();
  const projectContext = useProjectContext();
  const projectName = $derived(projectContext.projectName);
  const isCrdt = $derived(projectContext.projectType === 'crdt');
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
  let triggerRef = $state<HTMLButtonElement | null>(null);


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

{#snippet projectIcon(isCrdt: boolean)}
  {#if isCrdt}
    <Icon icon="i-mdi-book-edit-outline" />
  {:else}
    <Icon src={flexLogo} alt={$t`FieldWorks logo`} />
  {/if}
{/snippet}

<Popover.Root bind:open onOpenChange={handleOpen}>
  <Popover.Trigger bind:ref={triggerRef} title={projectName}>
    {#snippet child({ props })}
      <Button
        variant="ghost"
        role="combobox"
        aria-expanded={open}
        class="w-full justify-between overflow-hidden gap-0 paratext:!opacity-100"
        disabled={projectContext.inParatext}
        {...props}
      >
        <div class="flex items-center gap-2 overflow-hidden">
          {@render projectIcon(isCrdt)}
          <span class="x-ellipsis">
            {projectName}
          </span>
        </div>
        <Icon
          icon="i-mdi-chevron-down"
          class={cn('ml-2 size-4 shrink-0 opacity-50 paratext:hidden', open && 'rotate-180')}
        />
      </Button>
    {/snippet}
  </Popover.Trigger>
  <Popover.Content class="w-full p-0 min-w-56" align="start">
    <Command.Root>
      <Command.Input placeholder={$t`Search Dictionaries`} />
      <Command.List>
        {#if projectsResource.loading}
          <Command.Loading>
            <div class="flex items-center justify-center p-4">
              <Icon icon="i-mdi-loading" class="size-4 animate-spin" />
              <span class="ml-2">{$t`Loading Dictionaries...`}</span>
            </div>
          </Command.Loading>
        {:else}
          <Command.Empty>{$t`No Dictionaries found`}</Command.Empty>
          {#each projectsResource.current ?? [] as project (project)}
            <Command.Item
              value={project.name + project.crdt}
              onSelect={() => handleSelect(project)}
              class={cn('cursor-pointer', (project.name === projectName || project.code === projectName) && project.crdt === isCrdt && 'bg-secondary')}
            >
              {@render projectIcon(project.crdt)}
              <ProjectTitle {project} />
            </Command.Item>
          {/each}
        {/if}
      </Command.List>
    </Command.Root>
  </Popover.Content>
</Popover.Root>
