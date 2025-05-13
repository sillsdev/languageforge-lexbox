<script lang="ts">
  import type { Snippet } from 'svelte';
  import { page } from '$app/state';
  import type { FeatureFlag } from '$lib/gql/types';
  import { hasFeatureFlag } from '$lib/user';

  interface Props {
    flag: FeatureFlag | keyof typeof FeatureFlag;
    children?: Snippet;
  }

  let { flag, children }: Props = $props();
</script>

<!-- eslint-disable-next-line @typescript-eslint/no-unsafe-argument -->
{#if hasFeatureFlag(page.data.user, flag)}
  {@render children?.()}
{/if}
