<script lang="ts">
  import * as ResponsiveMenu from '$lib/components/responsive-menu';
  import {t} from 'svelte-i18n-lingui';

  type Props = {
    /** Bindable so callers can open the menu programmatically (e.g. from a long-press on the picture). */
    open?: boolean;
    disabled?: boolean;
    /** Extra classes for the three-dots trigger (e.g. to make it legible over an image). */
    triggerClass?: string;
    onEdit: () => void;
    onDownload: () => void;
    onDelete: () => void;
  };
  let {open = $bindable(false), disabled = false, triggerClass, onEdit, onDownload, onDelete}: Props = $props();
</script>

<!-- A three-dots menu of picture actions, reused by the picture field and the fullscreen viewer.
     Renders as a dropdown on desktop and a drawer on mobile (via ResponsiveMenu). -->
<ResponsiveMenu.Root bind:open>
  <ResponsiveMenu.Trigger class={triggerClass} {disabled} aria-label={$t`Picture actions`} />
  <ResponsiveMenu.Content>
    <ResponsiveMenu.Item icon="i-mdi-pencil" onSelect={onEdit}>{$t`Edit`}</ResponsiveMenu.Item>
    <ResponsiveMenu.Item icon="i-mdi-download" onSelect={onDownload}>{$t`Download`}</ResponsiveMenu.Item>
    <ResponsiveMenu.Item icon="i-mdi-delete" onSelect={onDelete}>{$t`Delete`}</ResponsiveMenu.Item>
  </ResponsiveMenu.Content>
</ResponsiveMenu.Root>
