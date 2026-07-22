<script lang="ts">
  import * as ResponsiveMenu from '$lib/components/responsive-menu';
  import {t} from 'svelte-i18n-lingui';
  import type {Snippet} from 'svelte';

  type Props = {
    /** Renders as a context menu on `children` (right-click / touch long-press) instead of a
        three-dots trigger button. */
    contextMenu?: boolean;
    children?: Snippet;
    disabled?: boolean;
    /** Extra classes for the three-dots trigger (e.g. to make it legible over an image). */
    triggerClass?: string;
    onEdit: () => void;
    onDownload: () => void;
    onDelete: () => void;
  };
  let {contextMenu = false, children, disabled = false, triggerClass, onEdit, onDownload, onDelete}: Props = $props();
</script>

<ResponsiveMenu.Root {contextMenu}>
  <ResponsiveMenu.Trigger
    class={triggerClass}
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
