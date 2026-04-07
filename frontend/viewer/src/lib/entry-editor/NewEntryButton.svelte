<script lang="ts">
  import {useViewService} from '$lib/views/view-service.svelte';
  import {Button, type ButtonProps} from '$lib/components/ui/button/index.js';
  import {t} from 'svelte-i18n-lingui';
  import {pt} from '$lib/views/view-text';

  const viewService = useViewService();
  const {...restProps}: ButtonProps = $props();
</script>

<Button icon="i-mdi-book-plus-outline" variant="outline" {...restProps}>
  {pt($t`Create entry`, $t`Add new word`, viewService.currentView)}
</Button>
