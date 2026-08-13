<script lang="ts">
  import Markdown, {type ComponentsMap} from 'svelte-exmarkdown';
  import LinkRenderer from './LinkRenderer.svelte';

  interface Props {
    md: string;
  }

  let { md }: Props = $props();
  const renderer: ComponentsMap = { a: LinkRenderer };
</script>

<Markdown {md} plugins={[{ renderer }]} />
