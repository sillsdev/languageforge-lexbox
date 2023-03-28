<script lang="ts">
	import Form from '$lib/forms/Form.svelte';
	import Input from '$lib/forms/Input.svelte';
	import t from '$lib/i18n';
	import Modal, { CloseReason } from './Modal.svelte';
	import { addProjectUser } from '$lib/services/project.service';
	import { ProjectRole } from '$lib/gql/graphql';

	export let projectId: string;
	let modal: Modal;
	async function openModal(): Promise<void> {
		if ((await modal.openModal()) === CloseReason.Cancel) return;
		emailError = '';
		error = '';
		//invalid try again
		if (!email) {
			emailError = $t('project_page.add_user.email_required');
			return await openModal();
		}
		const result = await addProjectUser({ projectId, userEmail: email, role });
		if (result.error) {
			error = result.error.message;
			return await openModal();
		}
		modal.close();
	}

	let email: string;
	let role: ProjectRole = ProjectRole.Editor;
	let emailError: string | undefined;
	let error: string;
</script>

<button class="badge badge-lg badge-success cursor-pointer" on:click={openModal}>
	<span class="i-mdi-plus"/> {$t('project_page.add_user.add_button')}
</button>
<Modal bind:this={modal} on:close={() => (email = '')} onBottom>
	<Form on:submit={() => modal.submitModal()}>
		<p>{$t('project_page.add_user.modal_title')}</p>
		<Input
			label={$t('admin_dashboard.column_email')}
			bind:value={email}
			required
			error={emailError}
			autofocus
		/>

		<label for="select-role" class="label">
			<span class="label-text"> Role </span>
		</label>
		<select id="select-role" bind:value={role} class="select select-bordered">
			<option value={ProjectRole.Editor}>{$t('project_role.editor_description')}</option>
			<option value={ProjectRole.Manager}>{$t('project_role.manager_description')}</option>
		</select>

		{#if error}
			<label class="label">
				<span class="label-text-alt text-error mb-2">{error}</span>
			</label>
		{/if}
	</Form>
	<button
		slot="actions"
		on:click={() => modal.submitModal()}
		let:closing
		class="btn btn-primary"
		class:loading={closing}
	>
		{$t('project_page.add_user.submit_button')}
	</button>
</Modal>
