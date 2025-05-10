<script lang="ts">
  import Markdown, {type ComponentsMap} from 'svelte-exmarkdown';
  import NewTabLinkRenderer from './NewTabLinkRenderer.svelte';

  interface Props {
    md: string;
  }

  const { md }: Props = $props();
  const renderer: ComponentsMap = { a: NewTabLinkRenderer };
</script>

<Markdown {md} plugins={[{ renderer }]} />
