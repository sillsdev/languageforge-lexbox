<script lang="ts" module>
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

  interface Props {
    hideSettingsTab?: boolean;
    activeTab?: OrgTabId;
    projectCount: number;
    memberCount: number;
  }

  let {
    hideSettingsTab = false,
    activeTab = $bindable('projects'),
    projectCount,
    memberCount,
  }: Props = $props();

  let visibleTabs = $derived(hideSettingsTab ? orgTabs.filter(t => t !== 'settings') : orgTabs);
</script>

<div role="tablist" class="flex tabs tabs-lifted tabs-lg overflow-x-auto">
  <div class="tab tab-divider"></div>
  {#each visibleTabs as tab}
    {@const isActiveTab = activeTab === tab}
    <button onclick={() => activeTab = tab} role="tab" class:tab-active={isActiveTab} class="tab grow flex-1 basis-1/2">
      <h2 class="text-lg flex gap-4 items-center">
        {$t(DEFAULT_TAB_I18N[tab])}
        {#if tab === 'projects'}
          <Badge>{$number(projectCount)}</Badge>
        {:else if tab === 'members'}
          <Badge>{$number(memberCount)}</Badge>
        {/if}
      </h2>
    </button>
    <div class="tab tab-divider"></div>
  {/each}
</div>
