<script lang="ts">
  import type {IProjectModel} from '$lib/dotnet-types';

  const {project} = $props<{ project: IProjectModel }>();

  const name = $derived(project.name);
  const code = $derived(project.code === project.name ? undefined : project.code);
</script>

{name}
{#if code}
  <span class="opacity-75 text-sm">({code})</span>
{/if}
