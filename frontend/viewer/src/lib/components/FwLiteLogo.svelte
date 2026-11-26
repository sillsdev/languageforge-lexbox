<script lang="ts">
  import {Icon} from '$lib/components/ui/icon';
  import {mode} from 'mode-watcher';
  import {t} from 'svelte-i18n-lingui';
  import logo from '$logos/fieldworks-lite/fieldworks-lite-tiny.svg';
  import logoReversed from '$logos/fieldworks-lite/fieldworks-lite-tiny-reversed.svg';

  const props: {class?: string} = $props();
</script>

<Icon {...props} src={mode.current === 'dark' ? logoReversed : logo} alt={$t`FieldWorks Lite logo`} />
