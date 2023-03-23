<script lang="ts">
    import t from '$lib/i18n';
    import type {Project} from '$lib/project';

    export let projects: Project[] = [];
</script>

<div class="grid grid-cols-3 gap-4">
    {#each projects as project}
        <div class="card bg-base-200">
            <div class="card-body">
                <h2 class="card-title overflow-hidden text-ellipsis" title={project.name}>
                    <a class="link" href={`/project/${project.code}`}>
                        {project.name}
                    </a>
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
        </div>
    {/each}
</div>
