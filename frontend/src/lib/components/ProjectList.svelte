<script lang="ts">
  import t from '$lib/i18n';
  import { Badge } from './Badges';
  import { getProjectTypeIcon } from './ProjectType';
  import type { ProjectItemWithDraftStatus } from './Projects';

  export let projects: ProjectItemWithDraftStatus[];
</script>

<div class="grid grid-cols-2 sm:grid-cols-3 auto-rows-fr gap-2 md:gap-4">
  {#each projects as project}
    {#if project.isDraft}
      <div class="project draft">
        <div data-sveltekit-preload-data="false" class="card aspect-square bg-base-200 shadow-base-300 group overflow-hidden">
          <div class="bg" style="background-image: url('{getProjectTypeIcon(project.type)}')" />
          <div class="card-body z-[1]">
            <h2 class="card-title overflow-hidden text-ellipsis" title={project.name}>
              <span class="text-primary inline-flex gap-2 items-center">
                {project.name}
              </span>
            </h2>
            <p>{project.code}</p>

            <Badge variant="badge-warning" outline>
              Awaiting approval
            </Badge>

            <p class="flex items-end">
              {$t('projectlist.requested', {
                lastChange: new Date(),
              })}
            </p>
          </div>
        </div>
      </div>
    {:else}
      <a class="card aspect-square bg-base-200 shadow-base-300 group overflow-hidden" href={`/project/${project.code}`}>
        <div class="bg" style="background-image: url('{getProjectTypeIcon(project.type)}')" />
        <div class="card-body z-[1]">
          <h2 class="card-title overflow-hidden text-ellipsis" title={project.name}>
            <span class="text-primary inline-flex gap-2 items-center">
              {project.name}
            </span>
          </h2>
          <p>{project.code}</p>
          {#if !project.isDraft}
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
          {/if}
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

  .project.draft .card {
    pointer-events: none;
    box-shadow: none;
  }
</style>
