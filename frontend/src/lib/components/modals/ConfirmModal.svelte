<script lang="ts">
  import {type IconString, Icon} from '$lib/icons';
  import Modal, {DialogResponse} from './Modal.svelte';
  import {Button, type ErrorMessage, FormError} from '$lib/forms';
  import Loader from '$lib/components/Loader.svelte';

  export let title: string;
  export let submitText: string;
  export let submitIcon: IconString | undefined = undefined;
  export let submitVariant: 'btn-primary' |  'btn-error' = 'btn-primary';

  export let cancelText: string;

  export async function open(onSubmit: () => Promise<ErrorMessage>): Promise<boolean> {
    if ((await modal.openModal()) === DialogResponse.Cancel) {
      error = undefined;
      return false;
    }

    error = await onSubmit();
    if (error) {
      return open(onSubmit);
    }
    modal.close();
    error = undefined;
    return true;
  }

  let modal: Modal;
  let error: ErrorMessage = undefined;
</script>


<Modal bind:this={modal} showCloseButton={false}>
  <h2 class="text-xl mb-2">
    {title}
  </h2>
  <slot/>
  <FormError {error} right/>
  <svelte:fragment slot="actions" let:submitting>
    <Button style={submitVariant} on:click={() => modal.submitModal()}>
      <Loader loading={submitting}/>
      {submitText}
      {#if submitIcon}
        <Icon icon={submitIcon}/>
      {/if}
    </Button>
    <Button disabled={submitting} on:click={() => modal.cancelModal()}>
      {cancelText}
    </Button>
  </svelte:fragment>
</Modal>
