<script lang="ts">
  import type {Snippet} from 'svelte';
  import t, {date, number} from '$lib/i18n';
  import {getProjectTypeI18nKey, ProjectTypeIcon} from '$lib/components/ProjectType';
  import TrashIcon from '$lib/icons/TrashIcon.svelte';
  import type {ProjectItemWithDraftStatus} from '$lib/components/Projects';
  import Icon from '$lib/icons/Icon.svelte';
  import {projectUrl} from '$lib/util/project';
  import {resolve} from '$app/paths';
  import {nextProjectSort, type ProjectSort, type ProjectSortColumn} from './project-sort';

  const allColumns = ['name', 'code', 'users', 'createdAt', 'lastChange', 'type', 'actions'] as const;
  type ProjectTableColumn = typeof allColumns extends Readonly<Array<infer T>> ? T : never;
  interface Props {
    projects: ProjectItemWithDraftStatus[];
    columns?: Readonly<ProjectTableColumn[]>;
    actions?: Snippet<[{ project: ProjectItemWithDraftStatus }]>;
    /** The order `projects` is already in. Headers are only clickable when `onSortChange` is set. */
    sort?: ProjectSort;
    onSortChange?: (sort: ProjectSort) => void;
  }

  const { projects, columns = allColumns, actions, sort, onSortChange }: Props = $props();

  function ariaSort(column: ProjectSortColumn): 'ascending' | 'descending' | undefined {
    if (sort?.column !== column) return undefined;
    return sort.dir === 'asc' ? 'ascending' : 'descending';
  }

  function isColumnVisible(column: ProjectTableColumn): boolean {
    return columns.includes(column);
  }
</script>

{#snippet header(column: ProjectSortColumn, label: string, className = '')}
  <th class={className} aria-sort={ariaSort(column)}>
    {#if onSortChange}
      <button
        type="button"
        class="inline-flex items-center gap-2 cursor-pointer hover:text-base-content"
        onclick={() => sort && onSortChange(nextProjectSort(sort, column))}
      >
        {label}
        <span
          class:invisible={sort?.column !== column}
          class="{sort?.dir === 'desc' ? 'i-mdi-sort-descending' : 'i-mdi-sort-ascending'} text-xl"
        ></span>
      </button>
    {:else}
      {label}
    {/if}
  </th>
{/snippet}

<div class="overflow-x-auto @container scroll-shadow">
  <table class="table table-lg">
    <thead>
      <tr class="bg-base-200">
        {#if isColumnVisible('name')}
          {@render header('name', $t('project.table.name'))}
        {/if}
        {#if isColumnVisible('code')}
          {@render header('code', $t('project.table.code'))}
        {/if}
        {#if isColumnVisible('users')}
          {@render header('users', $t('project.table.users'), 'hidden @md:table-cell')}
        {/if}
        {#if isColumnVisible('createdAt')}
          {@render header('createdAt', $t('project.table.created_at'), 'hidden @xl:table-cell')}
        {/if}
        {#if isColumnVisible('lastChange')}
          {@render header('lastChange', $t('project.table.last_change'), 'hidden @2xl:table-cell')}
        {/if}
        {#if isColumnVisible('type')}
          <th>{$t('project.table.type')}</th>
        {/if}
        {#if actions}
          <th></th>
        {/if}
      </tr>
    </thead>
    <tbody>
      {#each projects as project ((project.isDraft ? 'draft-' : '') + project.id)}
        <tr>
          {#if isColumnVisible('name')}
            <td>
              <span class="flex gap-2 items-center">
                {#if project.isDraft}
                  {#if project.createUrl}
                    <!-- Only set if on admin dashboard -->
                    <a class="link" href={resolve(project.createUrl)}>
                      {project.name}
                    </a>
                  {:else}
                    {project.name}
                  {/if}
                  <span
                    class="tooltip text-warning text-xl shrink-0 leading-0"
                    data-tip={$t('admin_dashboard.is_draft')}
                  >
                    <Icon icon="i-mdi-script" />
                  </span>
                {:else if project.deletedDate}
                  <span class="contents text-error">
                    {project.name}
                    <TrashIcon pale />
                  </span>
                {:else}
                  <a class="link" href={resolve(projectUrl(project))}>
                    {project.name}
                  </a>
                {/if}
                {#if project.isConfidential}
                  <span
                    class="tooltip text-warning text-xl shrink-0 leading-0"
                    data-tip={$t('project.confidential.confidential')}
                  >
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
          {#if actions}
            {@render actions?.({ project })}
          {/if}
        </tr>
      {/each}
    </tbody>
  </table>
</div>
