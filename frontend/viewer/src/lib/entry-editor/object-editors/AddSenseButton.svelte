<script lang="ts">
  import {Button, type ButtonProps} from '$lib/components/ui/button';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {pt} from '$lib/views/view-text';
  import {t} from 'svelte-i18n-lingui';

  const { ...restProps }: Pick<ButtonProps, 'onclick'> = $props();

  const viewService = useViewService();
</script>

<Button {...restProps} icon="i-mdi-plus" size="xs" title={pt($t`Add Sense`, $t`Add Meaning`, viewService.currentView)}>
  {pt($t`Add Sense`, $t`Add Meaning`, viewService.currentView)}
</Button>
