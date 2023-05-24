<script lang="ts">
	import { Badge, BadgeList, MemberBadge } from '$lib/components/Badges';
	import EditableText from '$lib/components/EditableText.svelte';
	import FormatDate from '$lib/components/FormatDate.svelte';
	import FormatProjectType from '$lib/components/FormatProjectType.svelte';
	import FormatRetentionPolicy from '$lib/components/FormatRetentionPolicy.svelte';
	import HgLogView from '$lib/components/HgLogView.svelte';
	import DeleteModal from '$lib/components/modals/DeleteModal.svelte';
	import t from '$lib/i18n';
	import { user } from '$lib/user';
	import { z } from 'zod';
	import type { PageData } from './$types';
	import {
		_changeProjectDescription,
		_changeProjectName,
		_deleteProjectUser,
		type ProjectUser,
	} from './+page';
	import AddProjectUser from './AddProjectUser.svelte';

	export let data: PageData;

	$: project = data.project;
	$: _project = project as NonNullable<typeof project>;

	let deleteUserModal: DeleteModal;
	let userToDelete: ProjectUser | undefined;
	async function deleteProjectUser(projectUser: ProjectUser) {
		userToDelete = projectUser;
		await deleteUserModal.prompt(async () => {
			await _deleteProjectUser(project!.id, projectUser.User.id);
		});
	}

	async function updateProjectName(newName: string) {
		return _changeProjectName({ projectId: _project.id, name: newName });
	}

	async function updateProjectDescription(newDescription: string) {
		return _changeProjectDescription({ projectId: _project.id, description: newDescription });
	}

    $: userId = $user?.id;
    $: isAdmin = $user?.role == 'admin';
	$: canManage =
		isAdmin ||
		$user?.projects.find((project) => project.code == project.code)?.role == 'Manager';

	const projectNameValidation = z.string().min(1, $t('project_page.project_name_empty_error'));
</script>

<div class="space-y-4">
	{#if project}
		<div class="space-y-2">
			<div class="text-3xl flex items-center gap-3 flex-wrap">
				<span>{$t('project_page.project')}:</span>
				<EditableText
					disabled={!canManage}
					bind:value={project.name}
					validation={projectNameValidation}
					saveHandler={updateProjectName}
				/>
			</div>
			<BadgeList>
				<Badge><FormatProjectType type={project.type} /></Badge>
				<Badge><FormatRetentionPolicy policy={project.retentionPolicy} /></Badge>
			</BadgeList>
		</div>

		<div class="divider" />

		<p class="text-2xl mb-4">{$t('project_page.summary')}</p>

		<div class="space-y-2">
			<span class="text-lg">
				{$t('project_page.project_code')}:
				<span class="text-primary-content">{project.code}</span>
			</span>
			<div class="text-lg">
				{$t('project_page.last_commit')}:
				<span class="text-primary-content"><FormatDate date={project.lastCommit} /></span>
			</div>
			<div class="text-lg">{$t('project_page.description')}:</div>
			<span>
				<EditableText
					bind:value={project.description}
					disabled={!canManage}
					saveHandler={updateProjectDescription}
					placeholder={$t('project_page.add_description')}
					multiline
				/>
			</span>
		</div>

		<div class="divider" />

		<div>
			<p class="text-2xl mb-4">
				{$t('project_page.members')}
			</p>

			<BadgeList>
				{#each project.ProjectUsers as member}
					<div class="dropdown dropdown-end">
						<MemberBadge
							member={{ name: member.User.name, role: member.role }}
							canManage={canManage && (member.User.id != userId || isAdmin)}
						/>
						<ul class="dropdown-content menu bg-base-200 p-2 shadow rounded-box">
							<li>
								<button>
									<span class="i-mdi-account-lock text-2xl" />
									{$t('project_page.set-role')}
								</button>
							</li>
							<li>
								<button
									class="hover:bg-error hover:text-error-content"
									on:click={() => deleteProjectUser(member)}
								>
									<span class="i-mdi-trash-can text-2xl" />
									{$t('project_page.remove-user')}
								</button>
							</li>
						</ul>
					</div>
				{/each}
				{#if canManage}
					<AddProjectUser projectId={project.id} />
				{/if}

				<DeleteModal
					bind:this={deleteUserModal}
					entityName={$t('project_page.remove-project-user-title')}
					isRemoveDialog
				>
					{$t('project_page.confirm-remove', { userName: userToDelete?.User.name ?? '' })}
				</DeleteModal>
			</BadgeList>
		</div>

		<div class="divider" />

		<div class="space-y-2">
			<p class="text-2xl mb-4">
				<a class="link" href="/api/hg-view/{project.code}" target="_blank" rel="noreferrer">
					{$t('project_page.history')}
					<span class="i-mdi-open-in-new align-middle" />
				</a>
			</p>

			<!-- <HgWeb code={project.code} /> -->
			<HgLogView json={project.changesets} />
		</div>
	{:else}
		{$t('project_page.not_found', { code: data.code })}
	{/if}
</div>
