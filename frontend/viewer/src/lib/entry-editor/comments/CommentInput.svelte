<script lang="ts">
  import * as InputGroup from '$lib/components/ui/input-group';

  let {
    id,
    value = $bindable(''),
    placeholder,
    rows = 1,
    disabled = false,
    loading = false,
    buttonVariant = 'ghost',
    submitDisabled,
  }: {
    id: string;
    value?: string;
    placeholder: string;
    rows?: number;
    disabled?: boolean;
    loading?: boolean;
    buttonVariant?: 'default' | 'ghost';
    submitDisabled?: boolean;
  } = $props();

  const isSubmitDisabled = $derived(submitDisabled ?? !value.trim());

  function submitOnCtrlEnter(event: KeyboardEvent): void {
    if (event.key === 'Enter' && event.ctrlKey) {
      const target = event.target as HTMLTextAreaElement | null;
      target?.form?.requestSubmit();
      event.preventDefault();
    }
  }
</script>

<InputGroup.Root>
  <InputGroup.Textarea
    {id}
    bind:value
    {placeholder}
    {rows}
    {disabled}
    onkeydown={submitOnCtrlEnter}
    class="min-h-4 py-1"
  />
  <InputGroup.Addon align="block-end" class="flex-row-reverse">
    <InputGroup.Button
      variant={buttonVariant}
      size="icon-xs"
      icon="i-mdi-send"
      class="p-0! [&>.icon-wrapper]:flex [&>.icon-wrapper]:items-center [&>.icon-wrapper]:justify-center"
      iconProps={{ class: 'size-4' }}
      type="submit"
      disabled={isSubmitDisabled}
      {loading}
    />
  </InputGroup.Addon>
</InputGroup.Root>
