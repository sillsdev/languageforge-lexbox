<script lang="ts">
  import { mdiHelpCircleOutline } from '@mdi/js';
  import { Icon } from 'svelte-ux';

  export let helpId: string;

  const rootHref = 'https://downloads.languagetechnology.org/fieldworks/Documentation/en/index.htm#t=';

  $: href = `${rootHref}${encodeURIComponent(helpId)}`;
</script>

{#if href}
  <sup>
    <a class="link-text text-info" tabindex="-1" target="_blank" {href}>
      <Icon data={mdiHelpCircleOutline} size="1em" />
    </a>
  </sup>
{/if}
