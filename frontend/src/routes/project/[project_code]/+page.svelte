<script lang="ts">
	import AddProjectUser from './AddProjectUser.svelte';
	import FormatDate from '$lib/components/FormatDate.svelte';
	import FormatProjectType from '$lib/components/FormatProjectType.svelte';
	import FormatRetentionPolicy from '$lib/components/FormatRetentionPolicy.svelte';
	import FormatUserProjectRole from '$lib/components/FormatUserProjectRole.svelte';
	import HgWeb from '$lib/components/HgWeb.svelte';
	import t from '$lib/i18n';
	import type { PageData } from './$types';
	import DeleteModal from '$lib/components/modals/DeleteModal.svelte';
	import {
		_changeProjectDescription,
		_changeProjectName,
		_deleteProjectUser,
		type ProjectUser,
	} from './+page';
  import HgLogView from '$lib/components/HgLogView.svelte';
	import EditableText from '$lib/components/EditableText.svelte';
	import { Badge, BadgeList, TaggedBadge } from '$lib/components/Badges';
	import { z } from 'zod';
	import { user } from '$lib/user';

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

	const canManage = $user?.role == 'admin' || $user?.projects.find(project => project.code == project.code)?.role == 'Manager';

	const projectNameValidation = z.string().min(1, $t('project_page.project_name_empty_error'));
</script>

<div class="space-y-4">
	{#if project}
		<div class="text-3xl flex items-center gap-3 flex-wrap">
			<span>{$t('project_page.project')}:</span>
			<EditableText
				disabled={!canManage}
				value={project.name}
				validation={projectNameValidation}
				saveHandler={(newValue) => _changeProjectName({ projectId: _project.id, name: newValue })}
			/>
			<BadgeList>
				<Badge><FormatProjectType type={project.type} /></Badge>
				<Badge><FormatRetentionPolicy policy={project.retentionPolicy} /></Badge>
			</BadgeList>
		</div>

		<div class="divider" />

		<p class="text-2xl mb-4">{$t('project_page.summary')}</p>

		<div class="space-y-2">
			<span class="text-lg"
				>{$t('project_page.project_code')}:
				<span class="text-primary-content">{project.code}</span></span
			>
			<div class="text-lg">
				{$t('project_page.last_commit')}:
				<span class="text-primary-content"><FormatDate date={project.lastCommit} /></span>
			</div>
			<div class="text-lg">{$t('project_page.description')}:</div>
			<span>
				<EditableText
					value={project.description}
					disabled={!canManage}
					saveHandler={(newValue) =>
						_changeProjectDescription({ projectId: _project.id, description: newValue })}
					placeholder={$t('project_page.add_description')}
					multiline={true}
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
						<TaggedBadge button={canManage} icon={canManage ? 'i-mdi-dots-vertical' : ''}>
							<span>
								{member.User.name}
							</span>

							<FormatUserProjectRole slot="tag" projectRole={member.role} />
						</TaggedBadge>
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
			<HgLogView json={data.log.changesets}></HgLogView>
		</div>
	{:else}
		{$t('project_page.not_found', { code: data.code })}
	{/if}
</div>
