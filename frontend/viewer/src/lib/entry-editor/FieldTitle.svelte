<script lang="ts">
  import {fieldName} from '../i18n';
  import {useCurrentView} from '../services/view-service';
  import FieldHelpIcon from './FieldHelpIcon.svelte';
  import {fieldData} from './field-data';

  export let id: string;
  export let helpId: string | undefined = undefined;
  export let name: string | undefined = undefined;
  $: if (!helpId) helpId = fieldData[id]?.helpId;

  const currentView = useCurrentView();

</script>

<div>
  <span class="inline-flex items-center relative">
    <span class="name" title={`${id}: ${fieldName({name, id})}`}>
      {fieldName({name, id}, $currentView.i18nKey)}
      {#if helpId}
        <FieldHelpIcon helpId={helpId}/>
      {/if}
    </span>
  </span>
</div>
