<script lang="ts" context="module">
  import type { I18nKey } from '$lib/i18n';

  const tabs = ['projects', 'users'] as const;
  export type AdminTabId = typeof tabs[number];
  const DEFAULT_TAB_I18N = {
    projects: 'admin_dashboard.project_table_title',
    users: 'admin_dashboard.user_table_title',
  } as const satisfies Record<AdminTabId, I18nKey>;
</script>

<script lang="ts">
  import t from '$lib/i18n';

  export let activeTab: AdminTabId = 'projects';
</script>

<div role="tablist" class="hidden admin-tabs:flex tabs tabs-lifted tabs-lg">
  {#each tabs as tab}
    {@const isActiveTab = activeTab === tab}
    <a href={`#${tab}`} role="tab" class:tab-active={isActiveTab} class="tab grow">
      <h2 class="text-lg flex gap-4">
        {#if isActiveTab}
          <slot>
            {$t(DEFAULT_TAB_I18N[tab])}
          </slot>
        {:else}
          {$t(DEFAULT_TAB_I18N[tab])}
        {/if}
      </h2>
    </a>
  {/each}
</div>

<h2 class="admin-tabs:hidden text-2xl flex gap-4">
  <slot>
    {$t(DEFAULT_TAB_I18N[activeTab])}
  </slot>
</h2>
