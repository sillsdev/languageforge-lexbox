<script lang="ts">
  import type { Snippet } from 'svelte';
  interface Props {
    children?: Snippet;
  }

  let { children }: Props = $props();
</script>

<!--cloudflare replaces email addresses with a hash, however this can mess up the dom and break svelte, so sometimes we need to bypass it-->
<!--because normal html comments are trimmed in svelte we need to use @html to avoid the comment being trimmed-->
<!--eslint-disable-next-line svelte/no-at-html-tags-->
{@html '<!--email_off-->'}
{@render children?.()}
<!--eslint-disable-next-line svelte/no-at-html-tags-->
{@html '<!--/email_off-->'}
