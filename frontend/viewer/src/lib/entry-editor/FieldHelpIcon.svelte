<script lang="ts">
  import { mdiHelpCircleOutline } from '@mdi/js';
  import { Icon } from 'svelte-ux';
  import type { FieldConfig } from '../config-types';

  export let fieldConfig: FieldConfig;

  const rootHref = 'https://downloads.languagetechnology.org/fieldworks/Documentation/en/index.htm#t=';

  $: href = 'helpId' in fieldConfig && fieldConfig.helpId ? `${rootHref}${encodeURIComponent(fieldConfig.helpId)}` : undefined;
</script>

{#if href}
  <sup>
    <a class="link-text text-info" tabindex="-1" target="_blank" {href}>
      <Icon data={mdiHelpCircleOutline} size="1em" />
    </a>
  </sup>
{/if}
