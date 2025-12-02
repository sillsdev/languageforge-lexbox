<script lang="ts">
  import Markdown, {type ComponentsMap} from 'svelte-exmarkdown';
  import NewTabLinkRenderer from './NewTabLinkRenderer.svelte';

  export let md: string;
  const renderer: ComponentsMap = { a: NewTabLinkRenderer };
</script>

<Markdown {md} plugins={[{ renderer }]} />
