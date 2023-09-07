<script lang="ts">
  import type { LoadProjectsQuery } from '$lib/gql/types';
  import t from '$lib/i18n';
  import { Badge } from './Badges';
  import { ProjectTypeIcon } from './ProjectType';

  export let projects: LoadProjectsQuery['myProjects'];

</script>

<div class="grid grid-cols-2 sm:grid-cols-3 auto-rows-fr gap-2 md:gap-4">
  {#each projects as project}
    <a class="card bg-base-200 shadow-base-300" href={`/project/${project.code}`}>
      <div class="card-body">
        <h2 class="card-title overflow-hidden text-ellipsis" title={project.name}>
          <span class="text-primary inline-flex gap-2 items-center">
            {project.name} <ProjectTypeIcon type={project.type} />
          </span>
        </h2>

        <p>{project.code}</p>

        {#if project.userCount > 1}
          <p>
            {$t('projectlist.shared_with', { memberCount: project.userCount })}
          </p>
        {/if}

        <p class="flex items-end">
          {#if project.lastCommit}
            {$t('projectlist.last_change', {
              lastChange: new Date(project.lastCommit),
            })}
          {:else}
            <Badge type="badge-info" outline>
              {$t('projectlist.no_changes')}
            </Badge>
          {/if}
        </p>
      </div>
    </a>
  {/each}
  <a class="card border-4 border-base-200 shadow-base-300" href="/project/create">
    <div class="card-body mx-auto justify-center items-center text-primary">
      <span class="i-mdi-plus text-4xl" />
      <span class="text-xl text-center">{$t('project.create.title')}</span>
    </div>
  </a>
</div>

<style lang="postcss">
  .card {
    @apply
      shadow-lg
      transition
      duration-200
      hover:bg-neutral
      hover:text-neutral-content
      hover:border-neutral
      hover:shadow-xl;
  }
</style>
