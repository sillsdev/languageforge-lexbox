<script lang="ts">
  import {fieldName} from '../i18n';
  import {useCurrentView} from '$lib/views/view-service';
  import FieldHelpIcon from './FieldHelpIcon.svelte';
  import {fieldData} from './field-data';
  import {type View} from '$lib/views/view-data';

  export let id: string;
  export let helpId: string | undefined = undefined;
  export let name: string | undefined = undefined;
  $: if (!helpId) helpId = fieldData[id]?.helpId;

  const currentView = useCurrentView();

  $: alternateView = getAlternateView($currentView);

  function getAlternateView(view: View) {
    if ('alternateView' in view) return view.alternateView;
    return view.parentView.alternateView;
  }

</script>

<div class="field-title">
  <span class="inline-flex items-center relative">
    <span class="name" title={`${fieldName({name, id}, alternateView.i18nKey)} (${alternateView.label})`}>
      {fieldName({name, id}, $currentView.i18nKey)}
      {#if helpId}
        <FieldHelpIcon helpId={helpId}/>
      {/if}
    </span>
  </span>
</div>
