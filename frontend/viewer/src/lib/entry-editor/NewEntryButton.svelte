<script lang="ts">
  import {useCurrentView} from '$lib/views/view-service';
  import {Button, type ButtonProps} from '$lib/components/ui/button/index.js';
  import {t} from 'svelte-i18n-lingui';
  import {pt} from '$lib/views/view-text';

  const currentView = useCurrentView();
  const {...restProps}: ButtonProps = $props();
</script>

<Button icon="i-mdi-book-plus-outline" variant="outline" {...restProps}>
  {pt($t`Create entry`, $t`Add new word`, $currentView)}
</Button>
