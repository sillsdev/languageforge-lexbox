<script lang="ts">
  import { mdiEyeOffOutline } from '@mdi/js';
  import { Icon, Tooltip } from 'svelte-ux';
  import { fieldName } from '../i18n';
  import FieldHelpIcon from './FieldHelpIcon.svelte';
  import {fieldData} from './field-data';
  import {useCurrentView} from '../services/view-service';

  export let id: string;
  export let helpId: string | undefined = undefined;
  export let name: string | undefined = undefined;
  export let extra: boolean | undefined = undefined;
  $: if (!helpId) helpId = fieldData[id]?.helpId;

  const currentView = useCurrentView();

</script>

<div>
  <span class="inline-flex items-center relative">
    {#if extra}
      <Tooltip title="Extra / hidden field" delay={0} placement="top" offset={4}>
        <Icon classes={{root: 'absolute -left-[1.3em] opacity-50'}} data={mdiEyeOffOutline} size="1em" />
      </Tooltip>
    {/if}
    <span class="name" title={`${id}: ${fieldName({name, id})}`}>{fieldName({name, id}, $currentView.i18nKey)}</span>
  </span>
  {#if helpId}
    <FieldHelpIcon helpId={helpId}/>
  {/if}
</div>
