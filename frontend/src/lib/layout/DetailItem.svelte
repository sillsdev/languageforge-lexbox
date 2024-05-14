<script lang="ts">
  import CopyToClipboardButton from '$lib/components/CopyToClipboardButton.svelte';
  import EditableText from '$lib/components/EditableText.svelte';
  import type { ErrorMessage } from '$lib/forms';
  import t, { type I18nKey } from '$lib/i18n';

  export let title: I18nKey;
  export let text: string | null | undefined;
  export let copyToClipboard = false;
  export let editable = false;

  // Items below only used when editable = true
  export let disabled = false;
  export let saveHandler: undefined | ((newValue: string) => Promise<ErrorMessage>) = undefined;
  export let placeholder: string | undefined = undefined;
  export let multiline = false;

  $: extraComponent = copyToClipboard || $$slots.extraButton;

</script>

<div class={extraComponent ? 'text-lg flex items-center gap-1' : 'text-lg'}>
  {$t(title)}:
    {#if editable && saveHandler}
    <div>
      <EditableText
        value={text}
        {disabled}
        {saveHandler}
        {placeholder}
        {multiline}
      />
    </div>
    {:else}
    <span class="text-secondary">{text}</span>
    {/if}
  {#if copyToClipboard}
  <CopyToClipboardButton textToCopy={text ?? ''} size="btn-sm" outline={false} />
  {/if}
  {#if $$slots.extraButton}
  <slot name="extraButton" />
  {/if}
</div>
