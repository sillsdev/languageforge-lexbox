<script lang="ts">

    import type {Project} from "$lib/project";

    export let projects: Project[] = [];
</script>

<div class="grid grid-cols-3 gap-4 justify-items-center">
    {#each projects as project}
        <div class="card w-50 bg-base-200">
            <div class="card-body">
                <h2 class="card-title">{project.name}</h2>
                <p>{project.code}</p>
            </div>
        </div>
    {/each}
</div>