<script lang="ts">
  import t, { date } from '$lib/i18n';
  import { getProjectTypeI18nKey, ProjectTypeIcon } from '$lib/components/ProjectType';
  import TrashIcon from '$lib/icons/TrashIcon.svelte';
  import type { ProjectItemWithDraftStatus } from '$lib/components/Projects';
  import { ProjectMigrationStatus } from '$lib/gql/generated/graphql';
  import type { IconString } from '$lib/icons';
  import Icon from '$lib/icons/Icon.svelte';

  export let projects: ProjectItemWithDraftStatus[];

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
              {:else if project.isDraft}
                <span class="flex gap-2 items-center">
                  <a class="link" href={`/project/${project.code}`}>
                    {project.name}
                  </a>
                  <span
                    class="tooltip text-warning text-xl shrink-0 leading-0"
                    data-tip={$t('admin_dashboard.is_draft')}>
                    <Icon icon="i-mdi-script" />
                  </span>
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
                  {$date(project.deletedDate)}
                </span>
              {:else}
                {$date(project.lastCommit)}
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
