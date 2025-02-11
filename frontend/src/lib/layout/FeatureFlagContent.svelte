<script lang="ts">
  import {page} from '$app/stores'
  import type {FeatureFlag} from '$lib/gql/types';
  import {hasFeatureFlag} from '$lib/user';

  export let flag: FeatureFlag;
</script>
<!-- eslint-disable-next-line @typescript-eslint/no-unsafe-argument -->
{#if hasFeatureFlag($page.data.user, flag)}
    <slot />
{/if}
