<script lang="ts">
  import {t} from 'svelte-i18n-lingui';

  let {state}: {state: 'added' | 'removed'} = $props();

  const label = $derived(state === 'added' ? $t`Added` : $t`Removed`);
</script>

<!-- The diff colors and strikethrough don't reach screen readers. -->
<span class="sr-only">{`${label}: `}</span>
