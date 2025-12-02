<script  module lang="ts">
  // We define the Project type that we'll want in here, and export it so that callers can know they're passing in the right type
  export type Project = {
    id: string
    name: string
    code: string
    memberRole: ProjectRole
  };
</script>

<script lang="ts">
  import {resolve} from '$app/paths';
  import {ProjectRole} from '$lib/gql/types';
  import {writable} from 'svelte/store';
  import {onMount} from 'svelte';
  import t from '$lib/i18n';
  import FormatUserProjectRole from '../Projects/FormatUserProjectRole.svelte';
  import {projectUrl} from '$lib/util/project';

  interface Props {
    projects?: Project[];
    selectedProjects?: string[];
    hideRoleColumn?: boolean;
  }

  let { projects = [], selectedProjects = $bindable([]), hideRoleColumn = false }: Props = $props();

  let allSelected = $derived(projects && selectedProjects && selectedProjects.length === projects.length);

  function handleSelectAllClick(): void {
    if (!selectedProjects || !projects) return;
    if (allSelected) {
      selectedProjects = [];
    } else {
      selectedProjects = [...projects.map(proj => proj.id)];
    }
  }

  function isManager(proj: Project): boolean {
    return proj.memberRole === ProjectRole.Manager;
  }

  // Projects managed by the given user come pre-checked, to save time in typical uses of this component
  let projectsStore = $derived(writable(projects));
  onMount(() => projectsStore.subscribe(projects => {
    if (projects && projects.length > 0) {
      selectedProjects = [... projects.filter(isManager).map(proj => proj.id)];
    }
  }));
</script>

{#if projects?.length}
  <div class="overflow-x-auto @container scroll-shadow">
    <table class="table table-sm">
      <thead>
        <tr class="bg-base-200">
            <th class="p-0 w-4">
              <label class="px-3 py-2">
                <input type="checkbox" checked={allSelected} class="align-middle" onchange={handleSelectAllClick} />
              </label>
            </th>
            <th>
              <span class="align-middle">
                {$t('project.table.name')}
              </span>
            </th>
            {#if !hideRoleColumn}
              <th>
                <span class="align-middle">
                  {$t('project_role.label')}
                </span>
              </th>
            {/if}
        </tr>
      </thead>
      <tbody>
        {#each projects as proj (proj.id)}
          {@const isManager = proj.memberRole === ProjectRole.Manager}
          <tr>
            <td class="p-0 w-4">
              <label class="px-3 py-2">
                <input type="checkbox" bind:group={selectedProjects} value={proj.id} />
              </label>
            </td>
            <td>
              <a class="link" href={resolve(projectUrl(proj))} target="_blank">
                {proj.name}
              </a>
            </td>
            {#if !hideRoleColumn}
              <td>
                <span class:text-primary={isManager} class:font-bold={isManager} class:dark:brightness-150={isManager}>
                  <FormatUserProjectRole role={proj.memberRole} />
                </span>
              </td>
            {/if}
            </tr>
        {/each}
      </tbody>
      <tfoot>
        <tr class="h-2"></tr>
      </tfoot>
    </table>
  </div>
{/if}
