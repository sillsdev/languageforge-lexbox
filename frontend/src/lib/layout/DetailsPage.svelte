<script lang="ts">
  import BadgeList from '$lib/components/Badges/BadgeList.svelte';
  import t from '$lib/i18n';
  import HeaderPage from './HeaderPage.svelte';

  export let titleText: string;
  export let wide = false;
  export let setBreadcrumb = true;
</script>

<HeaderPage {wide} titleText={titleText} {setBreadcrumb}>
  <svelte:fragment slot="banner"><slot name="banner"></slot></svelte:fragment>
  <svelte:fragment slot="actions"><slot name="actions"></slot></svelte:fragment>
  <svelte:fragment slot="title"><slot name="title"></slot></svelte:fragment>
  <svelte:fragment slot="headerContent">
    {#if $$slots.badges}
      <BadgeList>
        <slot name="badges" />
      </BadgeList>
    {/if}
    <slot name="headerContent" />
  </svelte:fragment>
  {#if $$slots.details}
    <div class="my-4 space-y-2 details">
      <p class="text-2xl mb-4">{$t('project_page.summary')}</p>
      <slot name="details" />
    </div>
  {/if}
  <slot />
</HeaderPage>
