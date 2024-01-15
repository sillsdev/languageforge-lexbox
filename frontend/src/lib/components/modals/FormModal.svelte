<script context="module" lang="ts">
  import Modal, { DialogResponse } from '$lib/components/modals/Modal.svelte';
  import type { LexFormErrors, LexFormState } from '$lib/forms/superforms';
  import type { AnyZodObject, ZodObject, z } from 'zod';
  import type { ZodValidation } from 'sveltekit-superforms';
  import type { ErrorMessage } from '$lib/forms';

  export type FormModalResult<S extends AnyZodObject> = {
    response: DialogResponse;
    formState: LexFormState<S>;
  };

  export type FormSubmitCallback<Schema extends ZodValidation<AnyZodObject>> = (state: LexFormState<Schema>) => Promise<ErrorMessage | Partial<LexFormErrors<Schema>>>;
</script>

<script lang="ts">
  import { Form, FormError, lexSuperForm, SubmitButton } from '$lib/forms';
  import type { Readable } from 'svelte/store';

  type Schema = $$Generic<ZodObject>;
  type FormType = z.infer<Schema>;
  type SubmitCallback = FormSubmitCallback<Schema>;

  export let schema: Schema;

  const superForm = lexSuperForm(schema, () => modal.submitModal());
  const { form: _form, errors, reset, message, enhance, formState } = superForm;
  let modal: Modal;


  export async function open(
    value: FormType | undefined,  //eslint-disable-line @typescript-eslint/no-redundant-type-constituents
    onSubmit: SubmitCallback
  ): Promise<FormModalResult<Schema>>;
  export async function open(onSubmit: SubmitCallback): Promise<FormModalResult<Schema>>;
  export async function open(
    valueOrOnSubmit: Partial<FormType> | SubmitCallback | undefined,  //eslint-disable-line @typescript-eslint/no-redundant-type-constituents
    _onSubmit?: SubmitCallback
  ): Promise<FormModalResult<Schema>> {
    const onSubmit = _onSubmit ?? (valueOrOnSubmit as SubmitCallback);
    const value = _onSubmit ? (valueOrOnSubmit as Partial<FormType>) : undefined;

    reset();

    if (value) _form.set(value, { taint: false });

    const response = await openModal(onSubmit);
    const _formState = $formState; // we need to read the form state before the modal closes or it will be reset
    modal.close();
    return { response, formState: _formState };
  }

  export function close(): void {
    modal.close();
  }

  export function form(): Readable<Readonly<FormType>> {
    return superForm.form;
  }

  async function openModal(onSubmit: SubmitCallback): Promise<DialogResponse> {
    const result = await modal.openModal();
    if (result == DialogResponse.Cancel) return result;

    const error = await onSubmit($formState);
    if (error) {
      if (typeof error === 'string') $message = error;
      if (typeof error === 'object') $errors = {...$errors, ...error};
      // again go back to the top and await a response from the modal.
      return await openModal(onSubmit);
    }

    return result;
  }
</script>

<Modal bind:this={modal} on:close={() => reset()} bottom>
  <Form id="modalForm" {enhance}>
    <p><slot name="title" /></p>
    <slot errors={$errors} />
  </Form>
  <FormError error={$message} right />
  <svelte:fragment slot="extraActions">
    <slot name="extraActions" />
  </svelte:fragment>
  <svelte:fragment slot="actions" let:submitting>
    <SubmitButton form="modalForm" loading={submitting}>
      <slot name="submitText" />
    </SubmitButton>
  </svelte:fragment>
</Modal>
