<script lang="ts">
	import type { Readable } from 'svelte/store';

	import Modal, { DialogResponse } from '$lib/components/modals/Modal.svelte';
	import { FormError, lexSuperForm } from '$lib/forms';
	import Form from '$lib/forms/Form.svelte';
	import { z, type ZodRawShape } from 'zod';

	type T = $$Generic<ZodRawShape>;
	export let schema: T;

	let formSchema = z.object(schema);
	const superForm = lexSuperForm(formSchema, () => modal.submitModal());
	const { errors, reset, message, enhance } = superForm;
	const _form = superForm.form;

	type FormType = z.infer<typeof formSchema>;

	let modal: Modal;

	export async function open(
		onSubmit: (d: FormType) => Promise<string | undefined>,
	): Promise<void> {
		if ((await modal.openModal()) === DialogResponse.Cancel) return;
		const error = await onSubmit($_form);
		if (error) {
			$message = error;
			// again go back to the top and await a response from the modal.
			return await open(onSubmit);
		}
		modal.close();
	}

	export function form(): Readable<z.infer<typeof formSchema>> {
		return superForm.form;
	}
</script>

<Modal bind:this={modal} on:close={() => reset()} bottom>
	<Form id="modalForm" {enhance}>
		<p><slot name="title" /></p>
		<slot errors={$errors} />
	</Form>
	<svelte:fragment slot="actions" let:closing>
		{#if $message}
			<FormError>{$message}</FormError>
		{/if}
		<button type="submit" form="modalForm" class="btn btn-primary" class:loading={closing}>
			<slot name="submitText" />
		</button>
	</svelte:fragment>
</Modal>
