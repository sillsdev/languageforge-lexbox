<script module lang="ts">
  import type {Snippet} from 'svelte';
  import Modal, {DialogResponse} from '$lib/components/modals/Modal.svelte';
  import type {LexFormErrors, LexFormState} from '$lib/forms/superforms';
  import type {Infer, ValidationErrors} from 'sveltekit-superforms';
  import type {ZodValidation} from 'sveltekit-superforms/adapters';
  import type {ErrorMessage} from '$lib/forms';

  export type FormModalResult<S extends ZodValidation> = {
    response: DialogResponse;
    formState: LexFormState<Infer<S, 'zod'>>;
  };

  export type FormSubmitReturn<Schema extends ZodValidation> =
    | ErrorMessage
    | Partial<LexFormErrors<Infer<Schema, 'zod'>>>;
  export type FormSubmitCallback<Schema extends ZodValidation> = (
    state: LexFormState<Infer<Schema, 'zod'>>,
  ) => Promise<FormSubmitReturn<Schema>>;
</script>

<script lang="ts">
  import type {SubmitVariant} from '$lib/forms/SubmitButton.svelte';
  import {Button, Form, FormError, lexSuperForm, SubmitButton} from '$lib/forms';
  import type {FormEnhance} from '$lib/forms/types';
  import type {Readable} from 'svelte/store';

  type Schema = $$Generic<ZodValidation>;
  type FormType = Infer<Schema, 'zod'>;
  type SubmitCallback = FormSubmitCallback<Schema>;

  interface Props {
    schema: Schema;
    submitVariant?: SubmitVariant;
    hideActions?: boolean;
    showDoneState?: boolean;
    title?: Snippet;
    children?: Snippet<[{ errors: ValidationErrors<FormType> }]>;
    extraActions?: Snippet;
    submitText?: Snippet;
    doneText?: Snippet;
  }

  const {
    schema,
    submitVariant = 'btn-primary',
    hideActions = false,
    showDoneState = false,
    title,
    children,
    extraActions,
    submitText,
    doneText,
  }: Props = $props();

  const superForm = lexSuperForm(schema, () => modal?.submitModal() ?? Promise.resolve(undefined), {
    resetForm: false,
    taintedMessage: true,
  });
  const { form: _form, errors, reset, message, enhance, formState, tainted } = superForm;
  const formEnhance = enhance as FormEnhance;
  let modal: Modal | undefined = $state();
  let done = $state(false);

  export async function open(
    value: Partial<FormType> | undefined,
    onSubmit: SubmitCallback,
  ): Promise<FormModalResult<Schema>>;
  export async function open(onSubmit: SubmitCallback): Promise<FormModalResult<Schema>>;
  export async function open(
    valueOrOnSubmit: Partial<FormType> | SubmitCallback | undefined,
    _onSubmit?: SubmitCallback,
  ): Promise<FormModalResult<Schema>> {
    done = false;
    const onSubmit = _onSubmit ?? (valueOrOnSubmit as SubmitCallback);
    const value = _onSubmit ? (valueOrOnSubmit) : undefined;

    reset();

    //need to use update otherwise some fields might be undefined since value can be a partial
    if (value) _form.update((f) => ({ ...f, ...value }), { taint: false });

    const response = await openModal(onSubmit);
    const _formState = $formState; // we need to read the form state before the modal closes or it will be reset
    if (response !== DialogResponse.Submit || !showDoneState) modal?.close();
    return { response, formState: _formState };
  }

  export function form(): Readable<FormType> {
    return superForm.form;
  }

  export function close(): void {
    modal?.close();
  }

  async function openModal(onSubmit: SubmitCallback): Promise<DialogResponse> {
    const result = await modal!.openModal();
    if (result == DialogResponse.Cancel) return result;

    const error = await onSubmit($formState);
    if (error) {
      if (typeof error === 'string') $message = error;
      if (typeof error === 'object') $errors = { ...$errors, ...error };
      // again go back to the top and await a response from the modal.
      return await openModal(onSubmit);
    }

    done = true;

    return result;
  }

  const extraActionsRender = $derived(extraActions);
</script>

<Modal bind:this={modal} onClose={() => reset()} bottom closeOnClickOutside={!$tainted} {hideActions}>
  <Form id="modalForm" enhance={formEnhance}>
    <p class="mb-4 text-lg font-bold">{@render title?.()}</p>
    {@render children?.({ errors: $errors })}
  </Form>
  <FormError error={$message} right />
  {#snippet extraActions()}
    {@render extraActionsRender?.()}
  {/snippet}
  {#snippet actions({ submitting, close })}
    {#if !done}
      <SubmitButton form="modalForm" variant={submitVariant} loading={submitting}>
        {@render submitText?.()}
      </SubmitButton>
    {:else}
      <Button variant="btn-primary" onclick={close}>
        {@render doneText?.()}
      </Button>
    {/if}
  {/snippet}
</Modal>
