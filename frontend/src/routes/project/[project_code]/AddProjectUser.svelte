<script lang="ts">
	import { _addProjectUser } from './+page';
	import Form from '$lib/forms/Form.svelte';
	import Input from '$lib/forms/Input.svelte';
	import t from '$lib/i18n';
	import Modal, { DialogResponse } from '$lib/components/modals/Modal.svelte';
	import { ProjectRole } from '$lib/gql/graphql';
	import { lexSuperForm, Select } from '$lib/forms';
	import { z } from 'zod';

	const formSchema = z.object({
		email: z.string().email($t('project_page.add_user.email_required')),
		role: z.enum([ProjectRole.Editor, ProjectRole.Manager]).default(ProjectRole.Editor),
	});

	export let projectId: string;
	let modal: Modal;
	let count = 1;
	let {form, errors, reset, message, enhance } = lexSuperForm(formSchema, () => modal.submitModal());
	async function openModal(): Promise<void> {
		if (await modal.openModal() === DialogResponse.Cancel) return;
		const result = await _addProjectUser({ projectId, userEmail: $form.email, role: $form.role });
		if (result.error) {
			$message = result.error.message + ' ' + count++;
			// again go back tqo the top and await a response from the modal.
			return await openModal();
		}
		modal.close();
	}
</script>

<button class="badge badge-lg badge-success cursor-pointer" on:click={openModal}>
	<span class="i-mdi-plus"/> {$t('project_page.add_user.add_button')}
</button>
<Modal bind:this={modal} on:close={() => reset()} bottom>
	<Form id="addProjectUser" {enhance}>
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
		</Select>
	</Form>
	<svelte:fragment slot="actions" let:closing>
		{#if $message}
			<label class="label">
				<span class="label-text-alt text-lg text-error mb-2">{$message}</span>
			</label>
		{/if}
		<button
			type="submit"
			form="addProjectUser"
			class="btn btn-primary"
			class:loading={closing}>
			{$t('project_page.add_user.submit_button')}
		</button>
	</svelte:fragment>
</Modal>
