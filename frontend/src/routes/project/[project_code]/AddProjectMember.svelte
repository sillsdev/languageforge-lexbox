<script lang="ts">
	import { BadgeButton } from '$lib/components/Badges';
	import Modal, { DialogResponse } from '$lib/components/modals/Modal.svelte';
	import { lexSuperForm } from '$lib/forms';
	import Form from '$lib/forms/Form.svelte';
	import Input from '$lib/forms/Input.svelte';
	import UserRoleSelect from '$lib/forms/UserRoleSelect.svelte';
	import { ProjectRole } from '$lib/gql/graphql';
	import t from '$lib/i18n';
	import { z } from 'zod';
	import { _addProjectMember } from './+page';

	const formSchema = z.object({
		email: z.string().email($t('project_page.add_user.email_required')),
		role: z.enum([ProjectRole.Editor, ProjectRole.Manager]).default(ProjectRole.Editor),
	});

	export let projectId: string;
	let modal: Modal;
	let { form, errors, reset, message, enhance } = lexSuperForm(formSchema, () =>
		modal.submitModal(),
	);
	async function openModal(): Promise<void> {
		if ((await modal.openModal()) === DialogResponse.Cancel) return;
		const result = await _addProjectMember({ projectId, userEmail: $form.email, role: $form.role });
		if (result.error) {
			$message = result.error.message;
			// again go back tqo the top and await a response from the modal.
			return await openModal();
		}
		modal.close();
	}
</script>

<BadgeButton icon="i-mdi-account-plus-outline" type="badge-success" on:click={openModal}>
	{$t('project_page.add_user.add_button')}
</BadgeButton>

<Modal bind:this={modal} on:close={() => reset()} bottom>
	<Form id="addProjectMember" {enhance}>
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
		<UserRoleSelect bind:value={$form.role} error={$errors.role} />
	</Form>
	<svelte:fragment slot="actions" let:closing>
		{#if $message}
			<label class="label">
				<span class="label-text-alt text-lg text-error mb-2">{$message}</span>
			</label>
		{/if}
		<button type="submit" form="addProjectMember" class="btn btn-primary" class:loading={closing}>
			{$t('project_page.add_user.submit_button')}
		</button>
	</svelte:fragment>
</Modal>
