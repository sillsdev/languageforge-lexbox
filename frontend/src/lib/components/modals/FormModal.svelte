<script lang="ts">
  import type { Readable } from 'svelte/store';

  import Modal, { DialogResponse } from '$lib/components/modals/Modal.svelte';
  import { FormError, lexSuperForm } from '$lib/forms';
  import Form from '$lib/forms/Form.svelte';
  import type { ZodObject, z } from 'zod';

  type Schema = $$Generic<ZodObject>;
  type FormType = z.infer<Schema>;
  export let schema: Schema;

  const superForm = lexSuperForm(schema, () => modal.submitModal());
  const { errors, reset, message, enhance } = superForm;
  const _form = superForm.form;
  let modal: Modal;

  export async function open(
    onSubmit: (d: FormType) => Promise<string | undefined>,
    value?: Partial<FormType>
  ): Promise<void> {
    if (value) _form.set(value, { taint: false });
    if ((await modal.openModal()) === DialogResponse.Cancel) return;
    const error = await onSubmit($_form);
    if (error) {
      $message = error;
      // again go back to the top and await a response from the modal.
      return await open(onSubmit);
    }
    modal.close();
  }
  export function close(): void {
    modal.close();
  }
  export function form(): Readable<FormType> {
    return superForm.form;
  }
</script>

<Modal bind:this={modal} on:close={() => reset()} bottom>
  <Form id="modalForm" {enhance}>
    <p><slot name="title" /></p>
    <slot errors={$errors} />
  </Form>
  <FormError error={$message} right />
  <slot name="extraActions" slot="extraActions" />
  <svelte:fragment slot="actions" let:closing>
    <button type="submit" form="modalForm" class="btn btn-primary" class:loading={closing}>
      <slot name="submitText" />
    </button>
  </svelte:fragment>
</Modal>
