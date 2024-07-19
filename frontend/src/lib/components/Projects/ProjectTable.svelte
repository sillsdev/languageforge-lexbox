<script lang="ts">
  import t, { date, number } from '$lib/i18n';
  import { getProjectTypeI18nKey, ProjectTypeIcon } from '$lib/components/ProjectType';
  import TrashIcon from '$lib/icons/TrashIcon.svelte';
  import type { ProjectItemWithDraftStatus } from '$lib/components/Projects';
  import Icon from '$lib/icons/Icon.svelte';
  import Dropdown from '../Dropdown.svelte';
  import { createEventDispatcher } from 'svelte';

  export let projects: ProjectItemWithDraftStatus[];

  const allColumns = ['name', 'code', 'users', 'createdAt', 'lastChange', 'type', 'actions'] as const;
  type ProjectTableColumn = typeof allColumns extends Readonly<Array<infer T>> ? T : never;
  export let columns: Readonly<ProjectTableColumn[]> = allColumns;

  function isColumnVisible(column: ProjectTableColumn): boolean {
    return columns.includes(column);
  }

  const dispatch = createEventDispatcher<{
    removeProjectFromOrg: { projectId: string; projectName: string };
  }>();
</script>

<div class="overflow-x-auto @container scroll-shadow">
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
            <span class="i-mdi-sort-descending text-xl align-[-5px] ml-2" />
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
              <span class="flex gap-2 items-center">
                {#if project.isDraft}
                  <a class="link" href={project.createUrl}>
                    {project.name}
                  </a>
                  <span
                    class="tooltip text-warning text-xl shrink-0 leading-0"
                    data-tip={$t('admin_dashboard.is_draft')}>
                    <Icon icon="i-mdi-script" />
                  </span>
                {:else if project.deletedDate}
                  <span class="contents text-error">
                    {project.name}
                    <TrashIcon pale />
                  </span>
                {:else}
                  <a class="link" href={`/project/${project.code}`}>
                    {project.name}
                  </a>
                {/if}
                {#if project.isConfidential}
                  <span
                    class="tooltip text-warning text-xl shrink-0 leading-0"
                    data-tip={$t('project.confidential.confidential')}>
                    <Icon icon="i-mdi-shield-lock-outline" color="text-warning" />
                  </span>
                {/if}
              </span>
            </td>
          {/if}
          {#if isColumnVisible('code')}
            <td class="max-w-40 overflow-hidden text-ellipsis text-nowrap" title={project.code}>
              {project.code}
            </td>
          {/if}
          {#if isColumnVisible('users')}
            <td class="hidden @md:table-cell">
              {$number(project.isDraft ? undefined : project.userCount)}
            </td>
          {/if}
          {#if isColumnVisible('createdAt')}
            <td class="hidden @xl:table-cell">
              {$date(project.createdDate)}
            </td>
          {/if}
          {#if isColumnVisible('lastChange')}
            <td class="hidden @2xl:table-cell">
              {#if !project.isDraft && project.deletedDate}
                <span class="text-error">
                  {$date(project.deletedDate)}
                </span>
              {:else if !project.isDraft}
                {$date(project.lastCommit)}
              {:else}
                {$t('project.awaiting_approval')}
              {/if}
            </td>
          {/if}
          {#if isColumnVisible('type')}
            <td>
              <span class="tooltip align-bottom shrink-0 leading-0" data-tip={$t(getProjectTypeI18nKey(project.type))}>
                <ProjectTypeIcon type={project.type} />
              </span>
            </td>
          {/if}
          <td class="p-0">
            <Dropdown>
              <button class="btn btn-ghost btn-square">
                <span class="i-mdi-dots-vertical text-lg" />
              </button>
              <ul slot="content" class="menu">
                <li>
                  <button class="text-error" on:click={() => dispatch('removeProjectFromOrg', {projectId: project.id, projectName: project.name})}>
                    <TrashIcon />
                    {'Remove'}
                  </button>
                </li>
              </ul>
            </Dropdown>
          </td>
          {#if $$slots.actions}
            <slot name="actions" {project} />
          {/if}
        </tr>
      {/each}
    </tbody>
  </table>
</div>
