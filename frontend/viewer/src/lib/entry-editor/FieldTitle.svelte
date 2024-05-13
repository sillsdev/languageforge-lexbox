<script lang="ts">
  import { mdiEyeOffOutline } from '@mdi/js';
  import { Icon, Tooltip } from 'svelte-ux';
  import type { FieldConfig } from '../types';
  import { fieldName } from '../i18n';
  import { getContext } from 'svelte';
  import type { Readable } from 'svelte/store';
  import type { views } from '../config-data';
  import FieldHelpIcon from './FieldHelpIcon.svelte';

  export let field: FieldConfig;

  const activeView = getContext<Readable<typeof views[number]['value']>>('activeView');

  $: extraField = 'extra' in field && field.extra;
</script>

<div>
  <span class="inline-flex items-center relative">
    {#if extraField}
      <Tooltip title="Extra / hidden field" delay={0} placement="top" offset={4}>
        <Icon classes={{root: 'absolute -left-[1.3em] opacity-50'}} data={mdiEyeOffOutline} size="1em" />
      </Tooltip>
    {/if}
    <span class="name" title={`${field.id}: ${fieldName(field)}`}>{fieldName(field, $activeView?.i18n)}</span>
  </span>
  <FieldHelpIcon fieldConfig={field} />
</div>
