<script lang="ts">
  import {fieldName} from '$lib/i18n';
  import {useCurrentView} from '$lib/views/view-service';
  import {Button, type ButtonProps} from '$lib/components/ui/button/index.js';
  import {t} from 'svelte-i18n-lingui';

  const currentView = useCurrentView();
  const {...restProps}: ButtonProps = $props();
</script>

<Button icon="i-mdi-book-plus-outline" variant="outline" {...restProps}>
  {$t`Create ${fieldName({id: 'entry'}, $currentView.i18nKey)}`}
</Button>
