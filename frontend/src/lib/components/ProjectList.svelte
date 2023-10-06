<script lang="ts">
  import type { LoadProjectsQuery } from '$lib/gql/types';
  import t from '$lib/i18n';
  import { Badge } from './Badges';
  import { ProjectTypeIcon } from './ProjectType';

  export let projects: LoadProjectsQuery['myProjects'];
  export let showCreateButton = true;
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

        <p>
          <span class="i-mdi-account text-xl mb-[-4px]" /> {project.userCount}
        </p>

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

  {#if showCreateButton}
    <a class="card border-4 border-base-200 shadow-base-300" class:in-center-column={!projects.length} href="/project/create">
      <div class="card-body mx-auto justify-center items-center text-primary">
        <span class="i-mdi-plus text-4xl"/>
        <span class="text-xl text-center">{$t('project.create.title')}</span>
      </div>
    </a>
  {/if}
</div>

{#if !showCreateButton && !projects.length}
  <div class="text-lg text-secondary flex gap-4 items-center justify-center">
    <span class="i-mdi-creation-outline text-xl shrink-0" /> {$t('user_dashboard.no_projects')}
  </div>
{/if}

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

  .in-center-column {
    grid-column: 2;
  }
</style>
