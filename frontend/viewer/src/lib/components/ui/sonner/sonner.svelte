<script lang="ts">
  import {mode} from 'mode-watcher';
  import {Toaster as Sonner, type ToasterProps as SonnerProps} from 'svelte-sonner';
  import {buttonVariants} from '../button';

  let restProps: SonnerProps = $props();
</script>

<!--
The toast stretches across viewports that are <=600px

The button classes are messy, because of styling precedence issues
and unstyled: true removes a lot more than we want
-->
<Sonner
  theme={mode.current}
  closeButton
  class="toaster group w-96"
  toastOptions={{
    classes: {
      toast: 'gap-3 group toast group-[.toaster]:!bg-background group-[.toaster]:!text-foreground group-[.toaster]:!border-border group-[.toaster]:shadow-lg',
      description: 'group-[.toast]:!text-muted-foreground max-h-[30vh] overflow-y-auto whitespace-break-spaces',
      actionButton: buttonVariants({size: 'sm', variant: 'default', class: 'group-[.toast]:!bg-primary group-[.toast]:!text-primary-foreground !h-9 !px-3'}),
      cancelButton: buttonVariants({size: 'sm', variant: 'secondary', class: 'group-[.toast]:!bg-muted group-[.toast]:!text-muted-foreground !h-9 !px-3'}),
    },
  }}
  {...restProps}
/>
