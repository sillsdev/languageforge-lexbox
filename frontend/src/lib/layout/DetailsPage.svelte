<script lang="ts">
  import type { Snippet } from 'svelte';
  import BadgeList from '$lib/components/Badges/BadgeList.svelte';
  import t from '$lib/i18n';
  import HeaderPage from './HeaderPage.svelte';

  interface Props {
    titleText: string;
    wide?: boolean;
    setBreadcrumb?: boolean;
    banner?: Snippet;
    actions?: Snippet;
    title?: Snippet;
    badges?: Snippet;
    headerContent?: Snippet;
    details?: Snippet;
    children?: Snippet;
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
    children,
  }: Props = $props();

  // TODO: Can probably simplify this and get rid of the fooRender varaibles, I suspect. 2025-05 RM
  const bannerRender = $derived(banner);
  const actionsRender = $derived(actions);
  const titleRender = $derived(title);
  const headerContentRender = $derived(headerContent);
</script>

<HeaderPage {wide} {titleText} {setBreadcrumb}>
  {#snippet banner()}
    {@render bannerRender?.()}
  {/snippet}
  {#snippet actions()}
    {@render actionsRender?.()}
  {/snippet}
  {#snippet title()}
    {@render titleRender?.()}
  {/snippet}
  {#snippet headerContent()}
    {#if badges}
      <BadgeList>
        {@render badges?.()}
      </BadgeList>
    {/if}
    {@render headerContentRender?.()}
  {/snippet}
  {#if details}
    <div class="my-4 space-y-2 details">
      <p class="text-2xl mb-4">{$t('project_page.summary')}</p>
      {@render details?.()}
    </div>
  {/if}
  {@render children?.()}
</HeaderPage>
