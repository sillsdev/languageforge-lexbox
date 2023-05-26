<script lang="ts">
	import { FormModal } from '$lib/components/modals';
	import UserRoleSelect from '$lib/forms/UserRoleSelect.svelte';
	import { ProjectRole } from '$lib/gql/graphql';
	import t from '$lib/i18n';
	import { z } from 'zod';
	import { _changeProjectMemberRole } from './+page';

	export let projectId: string;

	const schema = {
		role: z.enum([ProjectRole.Editor, ProjectRole.Manager]).default(ProjectRole.Editor),
	};
	let formModal: FormModal<typeof schema>;
	$: form = formModal?.form();

	let name: string;
	export async function open(member: { userId: string; name: string }): Promise<void> {
		name = member.name;
		await formModal.open(async (form) => {
			const result = await _changeProjectMemberRole({
				projectId,
				userId: member.userId,
				role: form.role,
			});
			return result.error?.message;
		});
	}
</script>

<FormModal bind:this={formModal} {schema} let:errors>
	<span slot="title">{$t('project_page.change_role_modal.title', { name })}</span>
	<UserRoleSelect bind:value={$form.role} error={errors.role} />
	<span slot="submitText">{$t('project_page.change_role')}</span>
</FormModal>
