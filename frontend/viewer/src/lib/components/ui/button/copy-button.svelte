<script lang="ts">
  import {Button, type ButtonProps} from '$lib/components/ui/button';
  import { AppNotification } from '$lib/notifications/notifications';
  import { t } from 'svelte-i18n-lingui';
  import type {Snippet} from 'svelte';

  type Props = {
    text?: string;
    notify?: boolean;
    children?: Snippet;
    // plus any other Button props via rest
  };

  // Svelte 5 props and children
  const { text, notify = true, children = undefined, ...rest }: Props & ButtonProps = $props();

  async function handleClick(_event: MouseEvent) {
    try {
      if (!text) {
        if (notify) AppNotification.display($t`Nothing to copy`, 'warning');
        return;
      }
      await navigator.clipboard.writeText(text);
      if (notify) AppNotification.display($t`Copied to clipboard`, 'success');
    } catch (error) {
      if (notify) AppNotification.display($t`Failed to copy to clipboard`, 'error');
      // In Svelte 5 we avoid createEventDispatcher; consumers can rely on notifications
      // or we could later emit a CustomEvent from a DOM element if needed.
    }
  }
</script>

<!--
  A wrapper around our shared Button that copies `text` to the clipboard when clicked.
  All other props are passed through to the underlying Button via Svelte 5 $props rest.
-->
<Button onclick={handleClick} icon="i-mdi-content-copy" {...rest}>
  {@render children?.()}
</Button>
