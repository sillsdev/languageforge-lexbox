<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {Textarea} from '$lib/components/ui/textarea';
  import {t} from 'svelte-i18n-lingui';

  let {
    value = $bindable(''),
    saving = false,
    placeholder,
    variant = 'default',
    onSubmit,
  }: {
    value?: string;
    saving?: boolean;
    placeholder?: string;
    variant?: 'default' | 'inline';
    onSubmit: (text: string) => void | Promise<void>;
  } = $props();

  let focused = $state(false);
  let textareaEl = $state<HTMLTextAreaElement | null>(null);

  const replyPlaceholder = $derived(placeholder ?? $t`Reply…`);
  const showActions = $derived(focused || value.trim().length > 0);
  const canSend = $derived(Boolean(value.trim()) && !saving);

  async function submit(): Promise<void> {
    const text = value.trim();
    if (!text) return;
    await onSubmit(text);
    value = '';
    focused = false;
    textareaEl?.blur();
  }

  function cancel(): void {
    value = '';
    focused = false;
    textareaEl?.blur();
  }

  function onFocusOut(event: FocusEvent): void {
    const next = event.relatedTarget as Node | null;
    if (event.currentTarget instanceof Node && next && event.currentTarget.contains(next)) return;
    focused = false;
  }

  function onKeyDown(e: KeyboardEvent): void {
    if (e.key === 'Enter' && (e.metaKey || e.ctrlKey)) {
      e.preventDefault();
      void submit();
    }
    if (e.key === 'Escape') {
      e.preventDefault();
      cancel();
    }
  }
</script>

{#if variant === 'inline'}
  <div class="flex items-end gap-2" onfocusin={() => (focused = true)} onfocusout={onFocusOut}>
    <Textarea
      bind:ref={textareaEl}
      bind:value
      placeholder={replyPlaceholder}
      rows={1}
      disabled={saving}
      class="min-h-9 flex-1 py-[7px] text-[13px] leading-5 md:text-[13px]"
      onkeydown={onKeyDown}
    />
    <Button
      variant="default"
      size="icon"
      class="size-9 max-h-9 max-w-9 min-h-9 min-w-9 shrink-0"
      icon="i-mdi-send"
      aria-label={$t`Reply`}
      onclick={() => void submit()}
      disabled={!canSend}
      loading={saving}
    />
  </div>
{:else}
  <div class="flex flex-col gap-1.5" onfocusin={() => (focused = true)} onfocusout={onFocusOut}>
    <Textarea
      bind:ref={textareaEl}
      bind:value
      placeholder={replyPlaceholder}
      rows={1}
      disabled={saving}
      class="min-h-8 py-1.5 text-[13px]"
      onkeydown={onKeyDown}
    />
    {#if showActions}
      <div class="flex justify-end gap-1.5">
        <Button variant="outline" size="sm" onclick={cancel} disabled={saving}>
          {$t`Cancel`}
        </Button>
        <Button size="sm" onclick={() => void submit()} disabled={!canSend} loading={saving}>
          {$t`Reply`}
        </Button>
      </div>
    {/if}
  </div>
{/if}
