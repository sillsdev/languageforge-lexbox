<script lang="ts">
  import {page} from '$app/stores'
  import type {FeatureFlag} from '$lib/gql/types';
  import {hasFeatureFlag} from '$lib/user';

  interface Props {
    flag: FeatureFlag | keyof typeof FeatureFlag;
    children?: import('svelte').Snippet;
  }

  let { flag, children }: Props = $props();
</script>
<!-- eslint-disable-next-line @typescript-eslint/no-unsafe-argument -->
{#if hasFeatureFlag($page.data.user, flag)}
    {@render children?.()}
{/if}
