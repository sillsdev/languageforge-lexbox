<script lang="ts">
  import BadgeList from '$lib/components/Badges/BadgeList.svelte';
  import t from '$lib/i18n';
  import HeaderPage from './HeaderPage.svelte';

  interface Props {
    titleText: string;
    wide?: boolean;
    setBreadcrumb?: boolean;
    banner?: import('svelte').Snippet;
    actions?: import('svelte').Snippet;
    title?: import('svelte').Snippet;
    badges?: import('svelte').Snippet;
    headerContent?: import('svelte').Snippet;
    details?: import('svelte').Snippet;
    children?: import('svelte').Snippet;
  }

  let {
    titleText,
    wide = false,
    setBreadcrumb = true,
    banner,
    actions,
    title,
    badges,
    headerContent,
    details,
    children
  }: Props = $props();

  const banner_render = $derived(banner);
  const actions_render = $derived(actions);
  const title_render = $derived(title);
  const headerContent_render = $derived(headerContent);
</script>

<HeaderPage {wide} titleText={titleText} {setBreadcrumb}>
  {#snippet banner()}
    {@render banner_render?.()}
  {/snippet}
  {#snippet actions()}
    {@render actions_render?.()}
  {/snippet}
  {#snippet title()}
    {@render title_render?.()}
  {/snippet}
  {#snippet headerContent()}
  
      {#if badges}
        <BadgeList>
          {@render badges?.()}
        </BadgeList>
      {/if}
      {@render headerContent_render?.()}
    
  {/snippet}
  {#if details}
    <div class="my-4 space-y-2 details">
      <p class="text-2xl mb-4">{$t('project_page.summary')}</p>
      {@render details?.()}
    </div>
  {/if}
  {@render children?.()}
</HeaderPage>
