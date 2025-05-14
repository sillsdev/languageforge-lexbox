<script lang="ts">
  import EditableText from '$lib/components/EditableText.svelte';
  import type { ErrorMessage } from '$lib/forms';

  interface Props {
    title: string;
    value: string | null | undefined;
    disabled?: boolean;
    saveHandler: (newValue: string) => Promise<ErrorMessage>;
    placeholder?: string;
    multiline?: boolean;
  }

  const {
    title,
    value,
    disabled = false,
    saveHandler,
    placeholder,
    multiline = false
  }: Props = $props();
</script>

<div>
  <div class="text-lg">
    {title}:
  </div>
  <span class="text-secondary">
    <EditableText {value} {disabled} {saveHandler} {placeholder} {multiline} />
  </span>
</div>
