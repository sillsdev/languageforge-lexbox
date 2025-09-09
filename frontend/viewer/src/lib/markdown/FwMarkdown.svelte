<script lang="ts">
  import Markdown, {type ComponentsMap} from 'svelte-exmarkdown';
  import LinkRenderer from './LinkRenderer.svelte';

  export let md: string;
  const renderer: ComponentsMap = { a: LinkRenderer };
</script>

<Markdown {md} plugins={[{ renderer }]} />
