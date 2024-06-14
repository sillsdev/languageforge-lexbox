<script lang="ts" context="module">
  import { Badge } from '$lib/components/Badges';
  import type { I18nKey } from '$lib/i18n';

  export const orgTabs = ['projects', 'members', 'settings', 'history'] as const;
  export type OrgTabId = (typeof orgTabs)[number];
  const DEFAULT_TAB_I18N: Record<OrgTabId, I18nKey> = {
    projects: 'org_page.projects_table_title',
    members: 'org_page.members_table_title',
    settings: 'org_page.settings_view_title',
    history: 'org_page.history_view_title',
  };
</script>

<script lang="ts">
  import t, { number } from '$lib/i18n';
  import { createEventDispatcher } from 'svelte';

  const dispatch = createEventDispatcher<{
    clickTab: OrgTabId
  }>();

  export let activeTab: OrgTabId = 'projects';
  export let projectCount: number;
  export let memberCount: number;

  function handleTabChange(tab: OrgTabId): void {
    const proceed = dispatch('clickTab', tab);
    if (proceed) {
      activeTab = tab;
    }
  }
</script>

<div role="tablist" class="flex tabs tabs-lifted tabs-lg">
  <div class="tab tab-divider" />
  {#each orgTabs as tab}
    {@const isActiveTab = activeTab === tab}
    <button on:click={() => handleTabChange(tab)} role="tab" class:tab-active={isActiveTab} class="tab grow flex-1 basis-1/2">
      <h2 class="text-lg flex gap-4 items-center">
        {$t(DEFAULT_TAB_I18N[tab])}
        {#if tab === 'projects'}
          <Badge>{$number(projectCount)}</Badge>
        {:else if tab === 'members'}
          <Badge>{$number(memberCount)}</Badge>
        {/if}
      </h2>
    </button>
    <div class="tab tab-divider" />
  {/each}
</div>
