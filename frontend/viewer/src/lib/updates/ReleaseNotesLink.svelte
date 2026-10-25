<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import Anchor from '$lib/components/ui/anchor/anchor.svelte';
  import {Icon} from '$lib/components/ui/icon';
  import {releaseNotesUrl} from './utils';
</script>

<Anchor variant="text" class="text-sm" href={releaseNotesUrl} target="_blank" rel="noopener noreferrer">
  {$t`Release notes`}
  <Icon icon="i-mdi-open-in-new" class="size-4" />
</Anchor>
