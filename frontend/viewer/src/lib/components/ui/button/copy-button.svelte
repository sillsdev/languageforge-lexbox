<script lang="ts">
  import {Button, type ButtonProps} from '$lib/components/ui/button';
  import {AppNotification} from '$lib/notifications/notifications';
  import {t} from 'svelte-i18n-lingui';
  import type {Snippet} from 'svelte';
  import {onDestroy} from 'svelte';
  import {mergeProps} from 'bits-ui';
  import {cn} from '$lib/utils';

  type Props = ButtonProps & {
    text?: string;
    notify?: boolean;
    children?: Snippet;
  };

  const {text, notify = true, children, ...rest}: Props = $props();

  let copied = $state(false);
  let resetTimer: ReturnType<typeof setTimeout> | undefined;

  async function handleClick(_event: MouseEvent) {
    try {
      if (!text) {
        if (notify)
          AppNotification.display($t`Nothing to copy`, {
            type: 'warning',
            timeout: 'min',
          });
        return;
      }
      await navigator.clipboard.writeText(text);
      if (notify)
        AppNotification.display($t`Copied to clipboard`, {
          type: 'success',
          timeout: 'min',
        });
      copied = true;
      clearTimeout(resetTimer);
      resetTimer = setTimeout(reset, 1500);
    } catch (error) {
      const detail = error instanceof Error ? error.message : String(error);
      if (notify) AppNotification.error($t`Failed to copy to clipboard`, detail);
    }
  }

  function reset() {
    copied = false;
    clearTimeout(resetTimer);
    resetTimer = undefined;
  }

  onDestroy(() => {
    clearTimeout(resetTimer);
  });
</script>

<!--
  todo: once we've updated to tailwind 4 maybe wrap:
  https://www.shadcn-svelte-extras.com/components/copy-button
-->
<Button
  disabled={copied}
  onclick={handleClick}
  icon={copied ? 'i-mdi-check' : 'i-mdi-content-copy'}
  {...rest}
  iconProps={mergeProps({class: cn(copied && 'text-primary')}, rest.iconProps)}
>
  {@render children?.()}
</Button>
