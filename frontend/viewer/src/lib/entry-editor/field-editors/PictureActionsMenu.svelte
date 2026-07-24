<script lang="ts">
  import * as ResponsiveMenu from '$lib/components/responsive-menu';
  import {t} from 'svelte-i18n-lingui';
  import type {ComponentProps, Snippet} from 'svelte';

  type Props = {
    /** Render as right-click / long-press instead of a three-dots button. */
    contextMenu?: boolean;
    children?: Snippet;
    disabled?: boolean;
    /** Extra classes for the three-dots trigger, if needed. */
    triggerClass?: string;
    /** Size defaults to a 40px icon button. */
    size?: ComponentProps<typeof ResponsiveMenu.Trigger>['size'];
    onOpenChange?: (open: boolean) => void;
    onEdit: () => void;
    onDownload: () => void;
    onDelete: () => void;
  };
  let {contextMenu = false, children, disabled = false, triggerClass, size, onOpenChange, onEdit, onDownload, onDelete}: Props = $props();
</script>

<ResponsiveMenu.Root {contextMenu} {onOpenChange}>
  <ResponsiveMenu.Trigger
    class={triggerClass}
    {size}
    {disabled}
    aria-label={contextMenu ? undefined : $t`Picture actions`}
    {children}
  />
  <ResponsiveMenu.Content>
    <ResponsiveMenu.Item icon="i-mdi-pencil" onSelect={onEdit}>{$t`Edit`}</ResponsiveMenu.Item>
    <ResponsiveMenu.Item icon="i-mdi-download" onSelect={onDownload}>{$t`Download`}</ResponsiveMenu.Item>
    <ResponsiveMenu.Item icon="i-mdi-delete" onSelect={onDelete}>{$t`Delete`}</ResponsiveMenu.Item>
  </ResponsiveMenu.Content>
</ResponsiveMenu.Root>
