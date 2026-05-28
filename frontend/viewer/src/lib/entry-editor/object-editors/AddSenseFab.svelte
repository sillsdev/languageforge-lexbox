<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {pt} from '$lib/views/view-text';
  import {t} from 'svelte-i18n-lingui';

  const {
    onclick,
  } = $props<{ onclick: () => void }>();

  const viewService = useViewService();
</script>

<Button {onclick} variant="default" size="extended-fab" class="font-semibold" icon="i-mdi-plus-thick">
  {pt($t`Sense`, $t`Meaning`, viewService.currentView)}
</Button>
