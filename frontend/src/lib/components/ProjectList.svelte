<script lang="ts">
    import t from '$lib/i18n';
    import type {Project} from '$lib/project';

    export let projects: Project[] = [];
</script>
{#if projects.length > 1}
    <div class="grid grid-cols-3 gap-4">
        {#each projects as project}
            <a class="card bg-neutral hover:bg-neutral-focus" href={`/project/${project.code}`}>
                <div class="card-body">
                    <h2 class="card-title overflow-hidden text-ellipsis" title={project.name}>
                        <span class="link">
                            {project.name}
                        </span>
                    </h2>

                    <p>{project.code}</p>

                    {#if project.userCount > 1}
                        <p>{$t('projectlist.shared_with', {memberCount: project.userCount})}</p>
                    {/if}

                    <p>
                        {#if project.lastCommit}
                            {$t('projectlist.last_change', {lastChange: new Date(project.lastCommit)})}
                        {:else}
                            <div class="badge badge-info">
                                {$t('projectlist.no_changes')}
                            </div>
                        {/if}
                    </p>
                </div>
            </a>
        {/each}
        <a class="card hover:bg-neutral-focus bg-neutral relative" href="/project/create">
            <div class="card-body mx-auto justify-center items-center">
                <span class="i-mdi-plus text-4xl"/>
                <span class="link text-xl">Create Project</span>
            </div>
        </a>
    </div>
{:else}
    <span>You don't have any projects</span>
{/if}