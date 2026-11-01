<script lang="ts">
  import {t} from 'svelte-i18n-lingui';

  let {state}: {state: 'added' | 'removed' | 'moved'} = $props();

  const label = $derived(state === 'added' ? $t`Added` : state === 'moved' ? $t`Moved` : $t`Removed`);
</script>

<!-- The diff colors and strikethrough don't reach screen readers. For a whole created or deleted entity the badge says it once. -->
<span class="sr-only in-[.diff-gone]:hidden in-[.diff-new]:hidden">{`${label}: `}</span>
