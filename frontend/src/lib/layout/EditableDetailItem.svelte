<script lang="ts">
  import EditableText from '$lib/components/EditableText.svelte';
  import type { ErrorMessage } from '$lib/forms';

  export let title: string;
  export let value: string | null | undefined;

  export let disabled = false;
  export let saveHandler: (newValue: string) => Promise<ErrorMessage>;
  export let placeholder: string | undefined = undefined;
  export let multiline = false;
</script>

<div>
  <div class="text-lg">
    {title}:
  </div>
  <span class="text-secondary">
    <EditableText {value} {disabled} {saveHandler} {placeholder} {multiline} />
  </span>
</div>
