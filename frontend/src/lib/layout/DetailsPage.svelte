<script lang="ts">
  import type { Snippet } from 'svelte';
  import BadgeList from '$lib/components/Badges/BadgeList.svelte';
  import t from '$lib/i18n';
  import HeaderPage, { type Props as HeaderPageProps } from './HeaderPage.svelte';

  interface Props extends HeaderPageProps {
    badges?: Snippet;
    details?: Snippet;
  }

  let {
    details,
    headerContent,
    badges,
    children,
    ...rest
  }: Props = $props();

  const headerContentRender = $derived(headerContent);
</script>

<HeaderPage {...rest}>
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
