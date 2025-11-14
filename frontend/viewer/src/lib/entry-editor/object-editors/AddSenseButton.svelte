<script lang="ts">
  import {Button, type ButtonProps} from '$lib/components/ui/button';
  import {useCurrentView} from '$lib/views/view-service';
  import {pt} from '$lib/views/view-text';
  import {t} from 'svelte-i18n-lingui';

  const { ...restProps }: Pick<ButtonProps, 'onclick'> = $props();

  const currentView = useCurrentView();
</script>

<Button {...restProps} icon="i-mdi-plus" size="xs" title={pt($t`Add Sense`, $t`Add Meaning`, $currentView)}>
  {pt($t`Add Sense`, $t`Add Meaning`, $currentView)}
</Button>
