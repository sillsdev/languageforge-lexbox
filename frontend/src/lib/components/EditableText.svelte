<script lang="ts">
  import { Form, lexSuperForm, randomFieldId } from '$lib/forms';
  import { ZodString, z } from 'zod';
  import IconButton from './IconButton.svelte';

  export let value: string | undefined | null = undefined;
  export let disabled = false;
  export let saveHandler: (newValue: string) => Promise<unknown>;
  export let placeholder: string | undefined = undefined;
  export let multiline = false;
  export let validation: ZodString | undefined = undefined;
  export const id = randomFieldId();

  let initialValue: string | undefined | null;
  let editing = false;
  let saving = false;

  let formElem: Form;

  const formSchema = z.object(validation ? { [id]: validation } : {});
  let { form, errors, reset, enhance } = lexSuperForm(
    formSchema,
    async () => {
      //callback only called when validation is successful
      await save();
    },
    { taintedMessage: false }
  );
  $: error = $errors[id]?.join(', ');

  function startEditing(): void {
    if (disabled) {
      return;
    }

    initialValue = value;
    form.set({ [id]: value ?? '' });
    editing = true;
  }

  async function save(): Promise<void> {
    const newValue = $form[id];
    if (newValue === initialValue) {
      editing = false;
      return;
    }

    saving = true;
    editing = false;
    try {
      await saveHandler(newValue);
      value = newValue;
    } finally {
      saving = false;
    }
  }

  function cancel(): void {
    editing = false;
    reset();
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
    formElem.requestSubmit();
  }
</script>

<span>
  {#if editing || saving}
    <span class="inline-flex items-end not-prose space-x-2 relative" class:w-full={multiline}>
      <!-- svelte-ignore a11y-autofocus -->
      <span
        class="tooltip-error tooltip-open tooltip-bottom"
        class:grow={multiline}
        class:tooltip={error}
        data-tip={error}
      >
        <Form bind:this={formElem} {enhance}>
          {#if multiline}
            <textarea
              {id}
              on:keydown={onKeydown}
              class:textarea-error={error}
              autofocus
              bind:value={$form[id]}
              readonly={saving}
              class="textarea textarea-bordered mt-1 h-48"
            />
          {:else}
            <input
              {id}
              on:keydown={onKeydown}
              class:input-error={error}
              autofocus
              bind:value={$form[id]}
              readonly={saving}
              class="input input-bordered mt-1 mb-0"
            />
          {/if}
        </Form>
      </span>

      <IconButton on:click={submit} loading={saving} icon="i-mdi-check-bold" />
      <IconButton on:click={cancel} disabled={saving} icon="i-mdi-close-thick" />
    </span>
  {:else}
    <span
      class:hover:bg-gray-800={!disabled}
      class="content-wrapper inline-flex items-center cursor-text rounded-lg py-2 px-3 -mx-3"
      on:click={startEditing}
      on:keypress={startEditing}
    >
      {#if value}
        <span class="mr-2 whitespace-pre-wrap text-primary">{value}</span>
      {:else}
        <span class="mr-2 opacity-75">{placeholder}</span>
      {/if}
      {#if !disabled}
        <span class="i-mdi-pencil-outline text-lg edit-icon mb-1 self-end" />
      {/if}
    </span>
  {/if}
</span>

<style>
  input,
  textarea {
    font-size: inherit;
  }

  textarea {
    min-width: 40vw;
  }

  .edit-icon {
    flex: 1 0 1.125rem;
  }
</style>
