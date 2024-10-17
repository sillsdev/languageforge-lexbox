<script  context="module" lang="ts">
  // We define the Project type that we'll want in here, and export it so that callers can know they're passing in the right type
  export type Project = {
    id: string
    name: string
    code: string
    role: ProjectRole
  };
</script>

<script lang="ts">
  import { ProjectRole } from '$lib/gql/types';
  import { writable } from 'svelte/store';
  import Badge from '../Badges/Badge.svelte';
  import { onMount } from 'svelte';

  export let projects: Project[] = [];
  export let selectedProjects: string[] = [];

  function isManager(proj: Project): boolean {
    return proj.role === ProjectRole.Manager;
  }

  // Projects managed by the given user come pre-checked, to save time in typical uses of this component
  $: projectsStore = writable(projects);
  onMount(() => projectsStore.subscribe(projects => {
    if (projects && projects.length > 0) {
      selectedProjects = [... projects.filter(isManager).map(proj => proj.code)];
    }
  }));
</script>

<ul>
  {#each projects as proj}
    <li>
      <input type="checkbox" bind:group={selectedProjects} value={proj.code} />
      {proj.name}
      {#if proj.role == ProjectRole.Manager}
      <Badge variant="badge-info">Manager</Badge>
      {/if}
    </li>
  {/each}
</ul>
