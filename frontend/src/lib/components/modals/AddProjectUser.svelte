<script lang="ts">
	import Form from '$lib/forms/Form.svelte';
	import Input from '$lib/forms/Input.svelte';
	import t from '$lib/i18n';
	import Modal, { DialogResponse } from './Modal.svelte';
	import { addProjectUser } from '$lib/services/project.service';
	import { ProjectRole } from '$lib/gql/graphql';
	import { lexSuperForm, lexSuperValidate, Select } from '$lib/forms';
	import { z } from 'zod';

	const formSchema = z.object({
		email: z.string().email($t('project_page.add_user.email_required')),
		role: z.enum([ProjectRole.Editor, ProjectRole.Manager])
	});

	export let projectId: string;
	let modal: Modal;
	let {form, errors, valid, update, reset, message } = lexSuperForm(formSchema);
	async function openModal(): Promise<void> {
		if (await modal.openModal() === DialogResponse.Cancel) return;
		//validate form, we're using a wrapper over the library method because we're doing this all client side.
		await lexSuperValidate($form, formSchema, update);
		// go back to the top and await a response from the modal again.
		if (!$valid) return await openModal();
		const result = await addProjectUser({ projectId, userEmail: $form.email, role: $form.role });
		if (result.error) {
			$message = result.error.message;
			// again go back to the top and await a response from the modal.
			return await openModal();
		}
		modal.close();
	}
</script>

<button class="badge badge-lg badge-success cursor-pointer" on:click={openModal}>
	<span class="i-mdi-plus"/> {$t('project_page.add_user.add_button')}
</button>
<Modal bind:this={modal} on:close={() => reset()} onBottom>
	<Form on:submit={() => modal.submitModal()}>
		<p>{$t('project_page.add_user.modal_title')}</p>
		<Input
			id="email"
			type="email"
			label={$t('admin_dashboard.column_email')}
			bind:value={$form.email}
			required
			error={$errors.email}
			autofocus
		/>
		<Select id="select-role" bind:value={$form.role} label={$t('project_role.label')} error={$errors.role}>
			<option value={ProjectRole.Editor}>{$t('project_role.editor_description')}</option>
			<option value={ProjectRole.Manager}>{$t('project_role.manager_description')}</option>
			<option value="test">invalid</option>
		</Select>
	</Form>
	<svelte:fragment slot="actions" let:closing>
		{#if $message}
			<label class="label">
				<span class="label-text-alt text-lg text-error mb-2">{$message}</span>
			</label>
		{/if}
	<button
		on:click={() => modal.submitModal()}
		class="btn btn-primary"
		class:loading={closing}
	>
		{$t('project_page.add_user.submit_button')}
	</button>
	</svelte:fragment>
</Modal>
