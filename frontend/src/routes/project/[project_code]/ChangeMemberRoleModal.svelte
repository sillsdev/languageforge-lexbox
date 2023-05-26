<script lang="ts">
	import Modal, { DialogResponse } from '$lib/components/modals/Modal.svelte';
	import { lexSuperForm } from '$lib/forms';
	import Form from '$lib/forms/Form.svelte';
	import UserRoleSelect from '$lib/forms/UserRoleSelect.svelte';
	import { ProjectRole } from '$lib/gql/graphql';
	import t from '$lib/i18n';
	import { z } from 'zod';
	import { _changeProjectMemberRole } from './+page';

	export let projectId: string;

	let _name: string;

	export async function open(member: { userId: string; name: string }): Promise<void> {
		_name = member.name;
		if ((await modal.openModal()) === DialogResponse.Cancel) return;
		const result = await _changeProjectMemberRole({
			projectId,
			userId: member.userId,
			role: $form.role,
		});
		if (result.error) {
			$message = result.error.message;
			// again go back tqo the top and await a response from the modal.
			return await open(member);
		}
		modal.close();
	}

	const formSchema = z.object({
		role: z.enum([ProjectRole.Editor, ProjectRole.Manager]).default(ProjectRole.Editor),
	});

	let modal: Modal;
	let { form, errors, reset, message, enhance } = lexSuperForm(formSchema, () =>
		modal.submitModal(),
	);
</script>

<Modal bind:this={modal} on:close={() => reset()} bottom>
	<Form id="changeMemberRole" {enhance}>
		<p>{$t('project_page.change_role_modal.title', { name: _name })}</p>
		<UserRoleSelect bind:value={$form.role} error={$errors.role} />
	</Form>
	<svelte:fragment slot="actions" let:closing>
		{#if $message}
			<label class="label" for="role">
				<span class="label-text-alt text-lg text-error mb-2">{$message}</span>
			</label>
		{/if}
		<button type="submit" form="changeMemberRole" class="btn btn-primary" class:loading={closing}>
			{$t('project_page.change_role')}
		</button>
	</svelte:fragment>
</Modal>
