<script lang="ts">

    import type { Project } from "$lib/project";

    export let projects: Project[] = [];
</script>

<div class="grid grid-cols-3 gap-4">
    {#each projects as project}
        <div class="card bg-base-200">
            <div class="card-body">
                <h2 class="card-title overflow-hidden text-ellipsis" title={project.name}>{project.name}</h2>
                <p>{project.code}</p>
                {#if project.userCount > 1}
                    <p>Shared with {project.userCount} people</p>
                {/if}
                <p>Last Change: {project.lastCommit ?? 'unknown'}</p>
            </div>
        </div>
    {/each}
</div>