<script lang="ts">
  import {type IconString, Icon} from '$lib/icons';
  import Modal, {DialogResponse} from './Modal.svelte';
  import {Button, type ErrorMessage, FormError} from '$lib/forms';
  import t from '$lib/i18n';

  export let title: string;
  export let submitText: string;
  export let submitIcon: IconString | undefined = undefined;
  export let submitVariant: 'btn-primary' |  'btn-error' = 'btn-primary';

  export let cancelText: string;
  export let hideActions: boolean = false;

  export let doneText = $t('common.close');
  export let showDoneState = false;

  let done = false;

  export async function open(onSubmit: () => Promise<ErrorMessage>): Promise<boolean> {
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

  let modal: Modal;
  let error: ErrorMessage = undefined;
</script>


<Modal bind:this={modal} showCloseButton={false} {hideActions}>
  <h2 class="text-xl mb-2">
    {title}
  </h2>
  <slot {done} {error} />
  <FormError {error} right/>
  <svelte:fragment slot="actions" let:submitting let:close>
    {#if !done}
      <Button variant={submitVariant} loading={submitting} on:click={() => modal.submitModal()}>
        {submitText}
        {#if submitIcon}
          <Icon icon={submitIcon}/>
        {/if}
      </Button>
      <Button disabled={submitting} on:click={() => modal.cancelModal()}>
        {cancelText}
      </Button>
    {:else}
      <Button variant="btn-primary" on:click={close}>
        {doneText}
      </Button>
    {/if}
  </svelte:fragment>
</Modal>
