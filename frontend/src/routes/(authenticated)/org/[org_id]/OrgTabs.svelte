<script lang="ts" context="module">
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
  import t from '$lib/i18n';
  import { createEventDispatcher } from 'svelte';

  const dispatch = createEventDispatcher<{
    clickTab: OrgTabId
  }>();

  export let activeTab: OrgTabId = 'projects';

  function handleTabChange(tab: OrgTabId): void {
    const proceed = dispatch('clickTab', tab);
    if (proceed) {
      activeTab = tab;
    }
  }
</script>

<!-- <div role="tablist" class="hidden admin-tabs:flex tabs tabs-lifted tabs-lg"> -->
<div role="tablist" class="flex tabs tabs-lifted tabs-lg">
  <div class="tab tab-divider" />
  {#each orgTabs as tab}
    {@const isActiveTab = activeTab === tab}
    <button on:click={() => handleTabChange(tab)} role="tab" class:tab-active={isActiveTab} class="tab grow flex-1 basis-1/2">
      <h2 class="text-lg flex gap-4 items-center">
        {$t(DEFAULT_TAB_I18N[tab])}
        {#if tab === 'projects'}
          <slot name="project-badges">
          </slot>
        {:else if tab === 'members'}
          <slot name="member-badges">
          </slot>
        {/if}
      </h2>
    </button>
    <div class="tab tab-divider" />
  {/each}
</div>

<h2 class="hidden text-2xl flex gap-4">
  <slot>
    {$t(DEFAULT_TAB_I18N[activeTab])}
  </slot>
</h2>

<style lang="postcss">
  .tab {
    /* https://daisyui.com/docs/themes/#-5 */
    --tab-border: 0.1rem;
    /* using a tab radius leads to tiny rendering issues at random screen sizes */
    --tab-radius: 0;

    /* https://daisyui.com/components/tab/#tabs-with-custom-color */
    --tab-border-color: oklch(var(--bc));

    &:not(.tab-active):not(.tab-divider) {
      border: var(--tab-border) solid var(--tab-border-color);

      &:hover {
        @apply bg-base-200;
      }
    }

    /* .tab-divider needs .tab so it can access the tab css-variables */
    &.tab-divider {
      @apply px-2;
      border-bottom: var(--tab-border) solid var(--tab-border-color);
    }
  }
</style>
