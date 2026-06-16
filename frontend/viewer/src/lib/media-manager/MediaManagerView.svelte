<script lang="ts">
  import {useProjectContext} from "$project/project-context.svelte";



const projectContext = useProjectContext();
let mediaFilesService = $derived(projectContext.mediaFilesService);

</script>
<div>
    <h1>Media Manager</h1>
    {#await mediaFilesService?.allResources()}
        <div>Loading...</div>
    {:then resources}
        {#each resources as resource}
            <div>
                <h2>{resource.id}</h2>
            </div>
        {/each}
    {/await}
</div>