<script lang="ts">
  import type { Snippet } from 'svelte';
  import { page } from '$app/state';
  import type { FeatureFlag } from '$lib/gql/types';
  import { hasFeatureFlag } from '$lib/user';

  interface Props {
    flag: FeatureFlag | keyof typeof FeatureFlag;
    children?: Snippet;
    hasFlagContent?: Snippet;
    missingFlagContent?: Snippet;
  }

  let { flag, missingFlagContent, hasFlagContent, children }: Props = $props();
</script>

<!-- eslint-disable-next-line @typescript-eslint/no-unsafe-argument -->
{#if hasFeatureFlag(page.data.user, flag)}
  {@render (hasFlagContent ?? children)?.()}
{:else}
  {@render missingFlagContent?.()}
{/if}
