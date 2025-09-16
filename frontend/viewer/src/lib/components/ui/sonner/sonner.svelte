<script lang="ts">
  import {mode} from 'mode-watcher';
  import {Toaster as Sonner, type ToasterProps as SonnerProps} from 'svelte-sonner';
  import {buttonVariants} from '../button';
  import {cn} from '$lib/utils';
  import Loading from '$lib/components/Loading.svelte';
  import {Icon} from '../icon';

  let {
    class: className,
    ...restProps
  }: SonnerProps = $props();
</script>

<!--
The toast stretches across viewports that are <=600px

The button classes are messy, because of styling precedence issues.
The recommended approaches are (1) use default styling or (2) use unstyled: true and do everything manually (so all or nothing).
I prefer this for now.

There's currently a bug where all toasts are rendered with the height of the latest toast, so
[&[data-expanded="true"]]:max-h-max prevents small toasts from being bigger than they should
[&[data-expanded="true"]]:!h-max prevents big toasts from being smaller than they should.
(Maybe this will fix it: https://github.com/wobsoriano/svelte-sonner/pull/167)

My fix results in another bug that I'm ignoring for now: sonner still thinks big toasts are small, so they can cover others toasts.
I.e. If there's small, big, small, then sonner thinks the big one is small and puts the last small one behind it (but only sometimes 😅)
-->
<Sonner
  theme={mode.current}
  closeButton
  class={cn('toaster group', className)}
  richColors
  toastOptions={{
    classes: {
      toast: 'gap-3 group toast [&[data-expanded="true"]]:!h-max [&[data-expanded="true"]]:max-h-max group-[.toaster]:!bg-background group-[.toaster]:!border-border group-[.toaster]:shadow-lg',
      description: 'group-[.toast]:!text-muted-foreground max-h-[30vh] overflow-y-auto whitespace-break-spaces touch-pan-y', /* pan-y means the browser should handle y-scrolling (and NOT x-scrolling, which is for swiping away) */
      actionButton: buttonVariants({size: 'sm', variant: 'default', class: 'group-[.toast]:!bg-primary group-[.toast]:!text-primary-foreground !h-9 min-h-9 !px-3 group-[.toast[data-type="error"]]:i-mdi-content-copy [&.copied]:!i-mdi-check group-[.toast[data-promise="true"][data-type="loading"]]:hidden'}),
      cancelButton: buttonVariants({size: 'sm', variant: 'secondary', class: 'group-[.toast]:!bg-muted group-[.toast]:!text-muted-foreground !h-9 min-h-9 !px-3'}),
    },
  }}
  {...restProps}
>
  {#snippet loadingIcon()}
    <Loading />
  {/snippet}
  {#snippet closeIcon()}
    <Icon icon="i-mdi-close" class="size-4" />
  {/snippet}
  {#snippet infoIcon()}
    <Icon icon="i-mdi-information-variant-circle" class="size-5" />
  {/snippet}
  {#snippet successIcon()}
    <Icon icon="i-mdi-check-circle" class="size-5" />
  {/snippet}
  {#snippet errorIcon()}
    <Icon icon="i-mdi-alert-octagram" class="size-5" />
  {/snippet}
  {#snippet warningIcon()}
    <Icon icon="i-mdi-alert" class="size-5" />
  {/snippet}
</Sonner>
