<script lang="ts">
  import {cn, type WithoutChildrenOrChild} from '$lib/utils.js';
  import DialogPortal from './dialog-portal.svelte';
  import {Dialog as DialogPrimitive, mergeProps} from 'bits-ui';
  import type {Snippet} from 'svelte';
  import * as Dialog from './index.js';
  import {XButton} from '../button';
  import {useDialogSharedRoot} from '../dialog-shared/dialog-shared-root.svelte';
  import type {ComponentProps} from 'svelte';

  let {
    ref = $bindable(null),
    class: className,
    portalProps,
    children,
    hideClose = false,
    ...restProps
  }: WithoutChildrenOrChild<DialogPrimitive.ContentProps> & {
    portalProps?: WithoutChildrenOrChild<ComponentProps<typeof DialogPortal>>;
    children: Snippet;
    hideClose?: boolean;
  } = $props();

  // This is duplicated in alert-dialog-content.svelte
  const state = useDialogSharedRoot();
</script>

<DialogPortal {...portalProps}>
  <Dialog.Overlay {...state.overlayProps} />
  <!--
    overflow-y-auto lives on the INNER wrapper rather than the Content element
    because Chrome clips top/bottom borders to zero width when overflow:auto and
    border-radius are set on the same element. In portrait the dialog drops to
    full-width with no rounded corners (sm: doesn't apply) so the bug never
    triggers; in landscape sm:rounded-lg kicks in and the borders disappear.
    Moving overflow to a wrapper that has no border-radius avoids the clip.
    Padding + grid gap also move to the wrapper so DialogHeader/Footer keep
    their `gap-4` spacing. The close button stays a sibling of the wrapper --
    it's absolutely positioned relative to the (fixed) outer, unchanged.
  -->
  <DialogPrimitive.Content
    bind:ref
    data-slot="dialog-content"
    {...mergeProps(state.contentProps, restProps)}
    class={cn(
      'bg-background data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95 fixed top-safe-center left-safe-center z-50 flex flex-col translate-x-[-50%] translate-y-[-50%] border shadow-lg duration-200 sm:rounded-lg',
      'max-sm:min-h-safe-screen min-w-full max-w-full max-h-safe-screen sm:min-w-[min(calc(100%-32px),50rem)] sm:max-w-[calc(100%-32px)]',
      state.contentProps.class,
      className,
    )}
  >
    <div class="grid gap-4 p-6 overflow-y-auto flex-1 min-h-0">
      {@render children?.()}
    </div>
    {#if !hideClose}
      <DialogPrimitive.Close class="absolute inset-e-4 top-4">
        {#snippet child({props})}
          <XButton {...props} />
        {/snippet}
      </DialogPrimitive.Close>
    {/if}
  </DialogPrimitive.Content>
</DialogPortal>
