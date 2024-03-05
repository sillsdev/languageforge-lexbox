<script lang="ts">
  import t, { date } from '$lib/i18n';
  import { getProjectTypeI18nKey, ProjectTypeIcon } from '$lib/components/ProjectType';
  import TrashIcon from '$lib/icons/TrashIcon.svelte';
  import type { ProjectItem } from '$lib/components/Projects';

  export let projects: ProjectItem[];

  const allColumns = ['name', 'code', 'users', 'createdAt', 'lastChange', 'type', 'actions'] as const;
  type ProjectTableColumn = typeof allColumns extends Readonly<Array<infer T>> ? T : never;
  export let columns: Readonly<ProjectTableColumn[]> = allColumns;

  function isColumnVisible(column: ProjectTableColumn): boolean {
    return columns.includes(column);
  }
</script>

<div class="overflow-x-auto @container">
  <table class="table table-lg">
    <thead>
      <tr class="bg-base-200">
        {#if isColumnVisible('name')}
          <th>{$t('project.table.name')}</th>
        {/if}
        {#if isColumnVisible('code')}
          <th>{$t('project.table.code')}</th>
        {/if}
        {#if isColumnVisible('users')}
          <th class="hidden @md:table-cell">{$t('project.table.users')}</th>
        {/if}
        {#if isColumnVisible('createdAt')}
          <th class="hidden @xl:table-cell">
            {$t('project.table.created_at')}
            <span class="i-mdi-sort-descending" />
          </th>
        {/if}
        {#if isColumnVisible('lastChange')}
          <th class="hidden @2xl:table-cell">
            {$t('project.table.last_change')}
          </th>
        {/if}
        {#if isColumnVisible('type')}
          <th>{$t('project.table.type')}</th>
        {/if}
        {#if $$slots.actions}
          <th />
        {/if}
      </tr>
    </thead>
    <tbody>
      {#each projects as project}
        <tr>
          {#if isColumnVisible('name')}
            <td>
              {#if project.deletedDate}
                <span class="flex gap-2 text-error items-center">
                  {project.name}
                  <TrashIcon pale />
                </span>
              {:else}
                <a class="link" href={`/project/${project.code}`}>
                  {project.name}
                </a>
              {/if}
            </td>
          {/if}
          {#if isColumnVisible('code')}
            <td>{project.code}</td>
          {/if}
          {#if isColumnVisible('users')}
            <td class="hidden @md:table-cell">
              {project.userCount}
            </td>
          {/if}
          {#if isColumnVisible('createdAt')}
            <td class="hidden @xl:table-cell">
              {$date(project.createdDate)}
            </td>
          {/if}
          {#if isColumnVisible('lastChange')}
            <td class="hidden @2xl:table-cell">
              {#if project.deletedDate}
                <span class="text-error">
                  {$date(project.deletedDate)}
                </span>
              {:else}
                {$date(project.lastCommit)}
              {/if}
            </td>
          {/if}
          {#if isColumnVisible('type')}
            <td>
              <span class="tooltip align-bottom" data-tip={$t(getProjectTypeI18nKey(project.type))}>
                <ProjectTypeIcon type={project.type} />
              </span>
            </td>
          {/if}
          {#if $$slots.actions}
            <slot name="actions" {project} />
          {/if}
        </tr>
      {/each}
    </tbody>
  </table>
</div>
