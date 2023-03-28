<script lang="ts">
	import AddProjectUser from '$lib/components/modals/AddProjectUser.svelte';
	import FormatDate from '$lib/components/FormatDate.svelte';
	import FormatProjectType from '$lib/components/FormatProjectType.svelte';
	import FormatRetentionPolicy from '$lib/components/FormatRetentionPolicy.svelte';
	import FormatUserProjectRole from '$lib/components/FormatUserProjectRole.svelte';
	import HgWeb from '$lib/components/HgWeb.svelte';
	import t from '$lib/i18n';
	import type { PageData } from './$types';
	import DeleteModal from '$lib/components/modals/DeleteModal.svelte';
	import { _deleteProjectUser, type ProjectUser } from './+page';

	export let data: PageData;

	$: project = data.project;

	let deleteUserModal: DeleteModal;
	let userToDelete: ProjectUser|undefined;
	async function deleteProjectUser(projectUser: ProjectUser) {
		userToDelete = projectUser;
		await deleteUserModal.prompt(async () => {
			await _deleteProjectUser(project!.id, projectUser.User.id);
		});
	}
</script>

{#if project}
	<div>
		<p>
			<span class="text-2xl">{project.name}</span>
		</p>

		<div>
			<span>{project.code}</span>

			<div class="badge badge-lg"><FormatProjectType type={project.type} /></div>
			<div class="badge badge-lg"><FormatRetentionPolicy policy={project.retentionPolicy} /></div>

			<span>{$t('project_page.last_commit')} <FormatDate date={project.lastCommit} /></span>
		</div>

		<p>{project.description || $t('project_page.missing_description')}</p>
	</div>

	<div class="divider" />

	<div>
		<p class="text-xl mb-2">
			<span class="i-mdi-account-group align-[-3px]" />
			{$t('project_page.members')}
		</p>

		<div class="flex flex-wrap gap-3">
			{#each project.ProjectUsers as member}
				<div class="dropdown">
					<button class="badge badge-lg badge-primary pr-0">
						{member.User.name}

						<!-- -1px is to cover the border on the right side -->
						<div class="badge badge-lg ml-2" style="margin-right: -1px;">
							<FormatUserProjectRole projectRole={member.role} />
						</div>
					</button>
					<ul class="dropdown-content menu bg-base-200 p-2 shadow rounded-box">
						<li>
							<button>
								<span class="i-mdi-account-lock text-2xl" />
								{ $t('project_page.set-role') }
							</button>
						</li>
						<li>
							<button class="hover:bg-error hover:text-error-content" on:click={() => deleteProjectUser(member)}>
								<span class="i-mdi-trash-can text-2xl" />
								{ $t('project_page.remove-user') }
							</button>
						</li>
					</ul>
				</div>
			{/each}
			<AddProjectUser projectId={project.id} />

			<DeleteModal bind:this={deleteUserModal} entityName={$t('project_page.remove-project-user-title')} isRemoveDialog>
				{ $t('project_page.confirm-remove', {userName: userToDelete?.User.name ?? ''}) }
			</DeleteModal>
		</div>
	</div>

	<div class="divider" />

	<div>
		<a class="text-xl link" href="/api/hg-view/{project.code}" target="_blank" rel="noreferrer">
			{$t('project_page.history')}
			<span class="i-mdi-open-in-new align-middle" />
		</a>

		<HgWeb code={project.code} />
	</div>
{:else}
	{$t('project_page.not_found', { code: data.code })}
{/if}
