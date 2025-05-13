<script lang="ts" module>
  import type { Snippet } from 'svelte';
  import type { I18nKey } from '$lib/i18n';

  export const adminTabs = ['projects', 'users'] as const;
  export type AdminTabId = (typeof adminTabs)[number];
  const DEFAULT_TAB_I18N: Record<AdminTabId, I18nKey> = {
    projects: 'admin_dashboard.project_table_title',
    users: 'admin_dashboard.user_table_title',
  };
</script>

<script lang="ts">
  import t from '$lib/i18n';
  import { createEventDispatcher } from 'svelte';

  const dispatch = createEventDispatcher<{
    clickTab: AdminTabId;
  }>();

  interface Props {
    activeTab?: AdminTabId;
    children?: Snippet;
  }

  let { activeTab = 'projects', children }: Props = $props();
</script>

<div role="tablist" class="hidden admin-tabs:flex tabs tabs-lifted tabs-lg overflow-x-auto">
  <div class="tab tab-divider"></div>
  {#each adminTabs as tab}
    {@const isActiveTab = activeTab === tab}
    <button
      onclick={() => dispatch('clickTab', tab)}
      role="tab"
      class:tab-active={isActiveTab}
      class="tab grow flex-1 basis-1/2"
    >
      <h2 class="text-lg flex gap-4 items-center">
        {#if isActiveTab}
          {#if children}{@render children()}{:else}
            {$t(DEFAULT_TAB_I18N[tab])}
          {/if}
        {:else}
          {$t(DEFAULT_TAB_I18N[tab])}
        {/if}
      </h2>
    </button>
    <div class="tab tab-divider"></div>
  {/each}
</div>

<h2 class="admin-tabs:hidden text-2xl flex gap-4">
  {#if children}{@render children()}{:else}
    {$t(DEFAULT_TAB_I18N[activeTab])}
  {/if}
</h2>
