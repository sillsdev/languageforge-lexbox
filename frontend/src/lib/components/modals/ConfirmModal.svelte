<script lang="ts">
  import type { Snippet } from 'svelte';
  import { type IconString, Icon } from '$lib/icons';
  import Modal, { DialogResponse } from './Modal.svelte';
  import { Button, type ErrorMessage, FormError } from '$lib/forms';
  import t from '$lib/i18n';

  interface Props {
    title: string;
    submitText: string;
    submitIcon?: IconString | undefined;
    submitVariant?: 'btn-primary' | 'btn-error';
    cancelText: string;
    hideActions?: boolean;
    doneText?: string;
    showDoneState?: boolean;
    children?: Snippet<[{ done: boolean; error: ErrorMessage }]>;
  }

  const {
    title,
    submitText,
    submitIcon = undefined,
    submitVariant = 'btn-primary',
    cancelText,
    hideActions = false,
    doneText = $t('common.close'),
    showDoneState = false,
    children,
  }: Props = $props();

  let done = $state(false);

  export async function open(onSubmit: () => Promise<ErrorMessage>): Promise<boolean> {
    if (!modal) return false;
    done = false;
    if ((await modal.openModal()) === DialogResponse.Cancel) {
      error = undefined;
      return false;
    }

    error = await onSubmit();
    if (error) {
      return open(onSubmit);
    }
    done = true;
    if (!showDoneState) modal.close();
    error = undefined;
    return true;
  }

  let modal: Modal | undefined = $state();
  let error: ErrorMessage = $state(undefined);
</script>

<Modal bind:this={modal} showCloseButton={false} {hideActions}>
  <h2 class="text-xl mb-2">
    {title}
  </h2>
  {@render children?.({ done, error })}
  <FormError {error} right />
  {#snippet actions({ submitting, close })}
    {#if !done}
      <Button variant={submitVariant} loading={submitting} onclick={() => modal?.submitModal()}>
        {submitText}
        {#if submitIcon}
          <Icon icon={submitIcon} />
        {/if}
      </Button>
      <Button disabled={submitting} onclick={() => modal?.cancelModal()}>
        {cancelText}
      </Button>
    {:else}
      <Button variant="btn-primary" onclick={close}>
        {doneText}
      </Button>
    {/if}
  {/snippet}
</Modal>
