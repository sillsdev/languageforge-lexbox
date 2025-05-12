<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {useCurrentView} from '$lib/views/view-service';
  import {pt} from '$lib/views/view-text';
  import {t} from 'svelte-i18n-lingui';

  const {
    onclick,
  } = $props<{ onclick: () => void }>();

  const currentView = useCurrentView();
</script>

<Button {onclick} variant="default" size="extended-fab" class="font-semibold" icon="i-mdi-plus-thick">
  {pt($t`Sense`, $t`Meaning`, $currentView)}
</Button>
