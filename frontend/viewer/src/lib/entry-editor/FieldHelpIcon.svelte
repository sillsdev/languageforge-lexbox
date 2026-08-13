<script lang="ts">
  import {Icon} from '$lib/components/ui/icon';

  interface Props {
    helpId: string;
  }

  let { helpId }: Props = $props();

  const rootHref = 'https://downloads.languagetechnology.org/fieldworks/Documentation/en/index.htm#t=';

  let href = $derived(`${rootHref}${encodeURIComponent(helpId)}`);
</script>

{#if href}
  <sup>
    <!-- eslint-disable-next-line svelte/no-navigation-without-resolve -->
    <a class="link-text text-sky-500" tabindex="-1" target="_blank" {href}>
      <Icon icon="i-mdi-help-circle-outline" class="size-3 ml-1 mt-1 absolute"/>
    </a>
  </sup>
{/if}
