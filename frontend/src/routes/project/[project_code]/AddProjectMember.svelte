<script lang="ts">
	import { BadgeButton } from '$lib/components/Badges';
	import { FormModal } from '$lib/components/modals';
	import Input from '$lib/forms/Input.svelte';
	import UserRoleSelect from '$lib/forms/UserRoleSelect.svelte';
	import { ProjectRole } from '$lib/gql/graphql';
	import t from '$lib/i18n';
	import { z } from 'zod';
	import { _addProjectMember } from './+page';

	export let projectId: string;

	const schema = {
		email: z.string().email($t('project_page.add_user.email_required')),
		role: z.enum([ProjectRole.Editor, ProjectRole.Manager]).default(ProjectRole.Editor),
	};
	let formModal: FormModal<typeof schema>;
	$: form = formModal?.form();

	async function openModal(): Promise<void> {
		await formModal.open(async (form) => {
			const result = await _addProjectMember({ projectId, userEmail: $form.email, role: $form.role });
			return result.error?.message;
		});
	}
</script>

<BadgeButton icon="i-mdi-account-plus-outline" on:click={openModal}>
	{$t('project_page.add_user.add_button')}
</BadgeButton>

<FormModal bind:this={formModal} {schema} let:errors>
	<span slot="title">{$t('project_page.add_user.modal_title')}</span>
	<Input
		id="email"
		type="email"
		label={$t('admin_dashboard.column_email')}
		bind:value={$form.email}
		required
		error={errors.email}
		autofocus
	/>
	<UserRoleSelect bind:value={$form.role} error={errors.role} />
	<span slot="submitText">{$t('project_page.add_user.submit_button')}</span>
</FormModal>
