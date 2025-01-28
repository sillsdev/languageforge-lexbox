<script>
  import {fieldName} from '$lib/i18n';
  import {useCurrentView} from '$lib/services/view-service';
  import {mdiBookPlusOutline} from '@mdi/js';
  import {Button} from 'svelte-ux';

  const currentView = useCurrentView();

</script>

<Button on:click icon={mdiBookPlusOutline} variant="fill-outline" color="success" size="sm">
  <div class="hidden sm:contents">
    Create {fieldName({id: 'entry'}, $currentView.i18nKey)}
  </div>
</Button>
