<script lang="ts">
  import t from '$lib/i18n';
  import type { Project } from '$lib/project';
  import { Badge } from './Badges';

  export let projects: Project[] = [];
</script>

<div class="grid grid-cols-2 sm:grid-cols-3 gap-2 md:gap-4">
  {#each projects as project}
    <a class="card bg-neutral hover:bg-neutral-focus transition duration-200" href={`/project/${project.code}`}>
      <div class="card-body">
        <h2 class="card-title overflow-hidden text-ellipsis" title={project.name}>
          <span class="link">
            {project.name}
          </span>
        </h2>

        <p>{project.code}</p>

        {#if project.userCount > 1}
          <p>
            {$t('projectlist.shared_with', { memberCount: project.userCount })}
          </p>
        {/if}

        <p>
          {#if project.lastCommit}
            {$t('projectlist.last_change', {
              lastChange: new Date(project.lastCommit),
            })}
          {:else}
            <Badge type="badge-info">
              {$t('projectlist.no_changes')}
            </Badge>
          {/if}
        </p>
      </div>
    </a>
  {/each}
  <a class="card hover:bg-neutral-focus bg-neutral transition duration-200" href="/project/create">
    <div class="card-body mx-auto justify-center items-center">
      <span class="i-mdi-plus text-4xl" />
      <span class="link text-xl text-center">{$t('project.create.title')}</span>
    </div>
  </a>
</div>
