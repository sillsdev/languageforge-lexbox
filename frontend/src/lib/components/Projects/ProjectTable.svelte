<script lang="ts">
  import t from '$lib/i18n';
  import { getProjectTypeI18nKey, ProjectTypeIcon } from '$lib/components/ProjectType';
  import TrashIcon from '$lib/icons/TrashIcon.svelte';
  import FormatDate from '$lib/components/FormatDate.svelte';
  import type { ProjectItem } from '$lib/components/Projects';
  import { ProjectMigrationStatus } from '$lib/gql/generated/graphql';
  import type { IconString } from '$lib/icons';

  export let projects: ProjectItem[];

  const allColumns = ['name', 'code', 'users', 'lastChange', 'migrated', 'type', 'actions'] as const;
  type ProjectTableColumn = typeof allColumns extends Readonly<Array<infer T>> ? T : never;
  export let columns: Readonly<ProjectTableColumn[]> = allColumns;

  const migrationStatusToIcon = {
    [ProjectMigrationStatus.Migrated]: 'i-mdi-checkbox-marked-circle-outline',
    [ProjectMigrationStatus.Migrating]: 'loading loading-spinner loading-xs',
    [ProjectMigrationStatus.Unknown]: 'i-mdi-help-circle-outline',
    [ProjectMigrationStatus.PrivateRedmine]: 'i-mdi-checkbox-blank-circle-outline',
    [ProjectMigrationStatus.PublicRedmine]: 'i-mdi-checkbox-blank-circle-outline',
  } satisfies Record<ProjectMigrationStatus, IconString>;

  function migrationStatusIcon(migrationStatus?: ProjectMigrationStatus): IconString {
    migrationStatus = migrationStatus ?? ProjectMigrationStatus.Unknown;
    return migrationStatusToIcon[migrationStatus] ?? migrationStatusToIcon[ProjectMigrationStatus.Unknown];
  }

  function isColumnVisible(column: ProjectTableColumn): boolean {
    return columns.includes(column);
  }
</script>

<div class="overflow-x-auto">
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
          <th>{$t('project.table.users')}</th>
        {/if}
        {#if isColumnVisible('lastChange')}
          <th>
            {$t('project.table.last_change')}
          </th>
        {/if}
        {#if isColumnVisible('migrated')}
          <th>{$t('project.table.migrated')}</th>
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
            <td>{project.userCount}</td>
          {/if}
          {#if isColumnVisible('lastChange')}
            <td>
              {#if project.deletedDate}
                <span class="text-error">
                  <FormatDate date={project.deletedDate} />
                </span>
              {:else}
                <FormatDate date={project.lastCommit} />
              {/if}
            </td>
          {/if}
          {#if isColumnVisible('migrated')}
            <td><span class={migrationStatusIcon(project.migrationStatus)} /></td>
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
