<script lang="ts">
	import { LoadingIcon } from '$lib/icons';
	import { ZodString, z } from 'zod';
	import { _changeProjectName } from '../../routes/project/[project_code]/+page';
	import IconButton from './IconButton.svelte';
	import { Form, lexSuperForm, lexSuperValidate } from '$lib/forms';

	export let value: string | undefined | null = undefined;
	export let disabled = false;
	export let saveHandler: (newValue: string) => Promise<unknown>;
	export let placeholder: string | undefined = undefined;
	export let multiline = false;
	export let validation: ZodString | undefined = undefined;

	let initialValue: string | undefined | null;
	let editing = false;
	let saving = false;
	const fieldId = 'id' + new Date().getTime();

	let inputElem: HTMLInputElement | HTMLTextAreaElement;
	let formElem: Form;

	const formSchema = z.object(validation ? { [fieldId]: validation } : {});
	let { form, errors, reset, enhance } = lexSuperForm(
		formSchema,
		async ({ form }) => {
			//callback only called when validation is successful
			await save();
		},
		{ taintedMessage: false },
	);
	$: error = $errors[fieldId]?.join(', ');

	function startEditing() {
		if (disabled) {
			return;
		}

		initialValue = value;
		form.set({[fieldId]: value ?? ''});
		editing = true;
	}

	async function save() {
		const newValue = inputElem.value;
		if (newValue === initialValue) {
			editing = false;
			return;
		}

		saving = true;
		editing = false;
		await saveHandler(newValue);
		value = newValue;
		saving = false;
		
	}

	function cancel() {
		editing = false;
		reset();
	}

	async function onKeydown(event: KeyboardEvent) {
		switch (event.key) {
			case 'Enter':
				if (multiline && event.ctrlKey) {
					event.preventDefault();
					await submit();
				}
				break;
			case 'Esc': // IE/Edge specific value
			case 'Escape':
				cancel();
				break;
		}
	}

	async function submit() {
		//triggers callback in superForm with validation
		formElem.requestSubmit();
	}
</script>

<span class="inline-flex items-end not-prose space-x-2 relative" class:w-full={multiline}>
	{#if editing || saving}
		<!-- svelte-ignore a11y-autofocus -->
		<span class:grow={multiline}>
			<Form bind:this={formElem} {enhance}>
				{#if multiline}
					<textarea
						id={fieldId}
						on:keydown={onKeydown}
						class:textarea-error={error}
						autofocus
						bind:value={$form[fieldId]}
						bind:this={inputElem}
						readonly={saving}
						class="textarea textarea-bordered mt-1"
					/>
				{:else}
					<input
						id={fieldId}
						on:keydown={onKeydown}
						class:input-error={error}
						autofocus
						bind:value={$form[fieldId]}
						bind:this={inputElem}
						readonly={saving}
						class="input input-bordered mt-1 mb-0"
					/>
				{/if}
			</Form>
		</span>
		
		<IconButton on:click={submit} disabled={saving} icon="i-mdi-check-bold" />
		<IconButton on:click={cancel} disabled={saving} icon="i-mdi-close-thick" />

		{#if error}
			<label for={fieldId} class="label absolute -bottom-5 p-0">
				<span class="label-text-alt text-error">{error}</span>
			</label>
		{/if}
		
		{#if saving}
			<span>
				<LoadingIcon />
			</span>
		{/if}
	{:else}
		<span
			class:hover:bg-gray-800={!disabled} class="content-wrapper cursor-text flex items-center py-2 px-3 -mx-3"
			on:click={startEditing}
			on:keypress={startEditing}
		>
			{#if value}
				<span class="mr-2 whitespace-pre-wrap text-primary-content">{value}</span>
			{:else}
				<span class="mr-2 opacity-75">{placeholder}</span>
			{/if}
			{#if !disabled}
				<span class="i-mdi-pencil-outline text-lg edit-icon" class:self-end={multiline} />
			{/if}
		</span>
	{/if}
</span>

<style lang="scss">
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
