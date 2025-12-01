<script lang="ts">
  import {Form, lexSuperForm, type ErrorMessage} from '$lib/forms';
  import {type ZodString, z} from 'zod';
  import IconButton from './IconButton.svelte';

  interface Props {
    value?: string | null;
    disabled?: boolean;
    saveHandler: (newValue: string) => Promise<ErrorMessage>;
    placeholder?: string;
    multiline?: boolean;
    validation?: ZodString;
  }

  let {
    value = $bindable(),
    disabled = false,
    saveHandler,
    placeholder,
    multiline = false,
    validation = z.string(),
  }: Props = $props();

  let initialValue: string | undefined | null;
  let editing = $state(false);
  let saving = $state(false);

  let formElem: Form | undefined = $state();

  const formSchema = z.object({ value: validation });
  let { form, errors, reset, enhance, message } = lexSuperForm(
    formSchema,
    async () => {
      //callback only called when validation is successful
      await save();
    },
    { taintedMessage: false },
  );
  let error = $derived($errors.value?.join(', ') ?? $message);

  function startEditing(): void {
    if (disabled) {
      return;
    }

    initialValue = value;
    reset();
    form.set({ value: value ?? '' }, { taint: false });
    editing = true;
  }

  async function save(): Promise<void> {
    const newValue = $form.value;
    if (newValue === initialValue) {
      editing = false;
      return;
    }

    saving = true;
    try {
      const error = await saveHandler(newValue);
      if (error) {
        $message = error;
      } else {
        value = newValue;
        editing = false;
      }
    } finally {
      saving = false;
    }
  }

  function cancel(): void {
    editing = false;
  }

  function onKeydown(event: KeyboardEvent): void {
    switch (event.key) {
      case 'Enter':
        if (multiline && event.ctrlKey) {
          event.preventDefault();
          submit();
        }
        break;
      case 'Esc': // IE/Edge specific value
      case 'Escape':
        cancel();
        break;
    }
  }

  function submit(): void {
    //triggers callback in superForm with validation
    formElem?.requestSubmit();
  }
</script>

<span>
  {#if editing || saving}
    <span
      class="inline-flex not-prose space-x-2 relative max-sm:flex-col max-w-full max-sm:w-full"
      class:w-full={multiline}
    >
      <!-- svelte-ignore a11y_autofocus -->
      <span
        class="tooltip-error tooltip-open tooltip-bottom"
        class:grow={multiline}
        class:tooltip={error}
        data-tip={error}
      >
        <Form bind:this={formElem} {enhance}>
          {#if multiline}
            <textarea
              onkeydown={onKeydown}
              class:textarea-error={error}
              autofocus
              bind:value={$form.value}
              readonly={saving}
              class="textarea textarea-bordered mt-1 h-48"
            ></textarea>
          {:else}
            <input
              onkeydown={onKeydown}
              class:input-error={error}
              autofocus
              bind:value={$form.value}
              readonly={saving}
              class="input input-bordered mb-0"
            />
          {/if}
        </Form>
      </span>

      <span class="max-sm:mt-2 flex flew-nowrap gap-2 self-end">
        <IconButton onclick={submit} loading={saving} icon="i-mdi-check-bold" />
        <IconButton onclick={cancel} disabled={saving} icon="i-mdi-close-thick" />
      </span>
    </span>
  {:else}
    <button
      class:hover:bg-base-300={!disabled}
      class="content-wrapper inline-flex items-center cursor-text rounded-lg py-1 px-1.5 -mx-1.5"
      onclick={startEditing}
      onkeypress={startEditing}
    >
      {#if value}
        <span class="mr-2 whitespace-pre-wrap">{value}</span>
      {:else}
        <span class="mr-2 opacity-75">{placeholder}</span>
      {/if}
      {#if !disabled}
        <span class="i-mdi-pencil-outline text-lg text-base-content edit-icon mb-1 self-end"></span>
      {/if}
    </button>
  {/if}
</span>

<style>
  input,
  textarea {
    font-size: inherit;
  }

  .edit-icon {
    flex: 1 0 1.125rem;
  }

  .content-wrapper {
    text-align: initial;
  }
</style>
