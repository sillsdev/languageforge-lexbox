<script lang="ts">
  import t from '$lib/i18n';
  import Icon from '$lib/icons/Icon.svelte';
  import { Badge } from './Badges';
  import { getProjectTypeIcon } from './ProjectType';
  import type { ProjectItemWithDraftStatus } from './Projects';

  export let projects: ProjectItemWithDraftStatus[];
</script>

<div class="grid grid-cols-1 xs:grid-cols-2 sm:grid-cols-3 auto-rows-fr gap-2 md:gap-4 max-xs:justify-items-center">
  {#each projects as project}
    {#if project.isDraft}
      <div class="draft card aspect-square bg-base-200 overflow-hidden">
        <div class="bg" style="background-image: url('{getProjectTypeIcon(project.type)}')" />
        <div class="card-body z-[1] max-sm:p-6">
          <h2 class="card-title overflow-hidden text-ellipsis" title={project.name}>
            <span class="text-primary inline-flex gap-2 items-center">
              {project.name}
              <Icon icon="i-mdi-script" color="text-warning"/>
            </span>
          </h2>
          <p>{project.code}</p>
          <Badge variant="badge-warning" outline>
            <Icon icon="i-mdi-progress-clock" />
            {$t('project.awaiting_approval')}
          </Badge>
          <p class="flex items-end">
            {$t('projectlist.requested', {
              requested: new Date(project.createdDate),
            })}
          </p>
        </div>
      </div>
    {:else}
      <a class="card aspect-square bg-base-200 shadow-base-300 overflow-hidden" href={`/project/${project.code}`}>
        <div class="bg" style="background-image: url('{getProjectTypeIcon(project.type)}')" />
        <div class="card-body z-[1] max-sm:p-6">
          <h2 class="card-title overflow-hidden text-ellipsis" title={project.name}>
            <span class="text-primary inline-flex gap-2 items-center">
              {project.name}
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
                <Badge variant="badge-info" outline>
                  {$t('projectlist.no_changes')}
                </Badge>
              {/if}
            </p>
        </div>
      </a>
    {/if}
  {/each}
</div>

<style lang="postcss">
  .card {
    @apply shadow-lg
      transition
      duration-200
      hover:bg-neutral
      hover:text-neutral-content
      hover:border-neutral
      hover:shadow-xl;

    max-height: 50vh;
    max-width: 100%;

    .bg {
      @apply absolute
          w-full
          h-full
          z-0
          bg-no-repeat
          opacity-50
          transition
          duration-200;

      background-size: auto 120px;
      right: calc(-100% + 100px);
      bottom: calc(-100% + 120px);
    }

    *[style*="our-word-logo"] {
      border-top-left-radius: 4em;
    }

    &:hover .bg {
      @apply opacity-100;
    }
  }

  .draft.card {
    pointer-events: none;
    box-shadow: none;
  }
</style>
