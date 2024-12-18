<script lang="ts">
  import Markdown, {type ComponentsMap} from 'svelte-exmarkdown';
  import type {Component} from 'svelte';
  import NewTabLinkRenderer from './NewTabLinkRenderer.svelte';

  export let md: string;
  const renderer: ComponentsMap = { a: NewTabLinkRenderer as unknown as Component };
</script>

<Markdown {md} plugins={[{ renderer: renderer }]} />
