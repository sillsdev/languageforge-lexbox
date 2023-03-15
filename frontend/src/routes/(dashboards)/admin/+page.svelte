<script lang="ts">
    import type {PageData} from './$types'
    import FormatDate from "$lib/components/FormatDate.svelte"
    import Input from "$lib/forms/Input.svelte"
	import t from '$lib/i18n'

    export let data: PageData

    let projectSearch = ''
	let userSearch = ''

	$: projectSearchLower = projectSearch.toLocaleLowerCase()
    $: projects = data.projects.filter(p => !projectSearch
        || p.name.toLocaleLowerCase().includes(projectSearchLower)
        || p.code.toLocaleLowerCase().includes(projectSearchLower)).slice(0, projectSearch ? undefined : 10)
    $: userSearchLower = userSearch.toLocaleLowerCase()
    $: users = data.users.filter(u => !userSearch
    	|| u.name.toLocaleLowerCase().includes(userSearchLower)
    	|| u.email.toLocaleLowerCase().includes(userSearchLower)).slice(0, userSearch ? undefined : 10)
</script>
<main>
	<div class="grid grid-cols-2">
		<div class="overflow-x-auto">
			<span class="text-xl">{ t('admin_dashboard.page_header') }</span>

			<Input
				type="text"
				label={ t('admin_dashboard.filter_label') }
				placeholder={ t('admin_dashboard.filter_placeholder') }
				autofocus
				bind:value={projectSearch}
			/>

			<div class="divider" />

			<table class="table">
				<thead>
					<tr>
						<th>{ t('admin_dashboard.column_name') }</th>
						<th>{ t('admin_dashboard.column_code') }</th>
						<th>{ t('admin_dashboard.column_users') }</th>
						<th>⬇️ { t('admin_dashboard.column_last_change') }</th>
					</tr>
				</thead>
				<tbody>
					{#each projects as project}
						<tr>
							<td>
								<a class="link" href={`/project/${project.code}`}>
                                    {project.name}
								</a>
							</td>
							<td>{project.code}</td>
							<td>{project.projectUsersAggregate.aggregate?.count ?? 0}</td>
							<td>
								<FormatDate date={project.lastCommit} nullLabel="–" />
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>

		<div class="overflow-x-auto">
			<span class="text-xl">Users</span>

			<Input type="text" label={ t('admin_dashboard.filter_label') } placeholder={ t('admin_dashboard.filter_placeholder') } bind:value={userSearch} />

			<div class="divider" />

			<table class="table">
				<thead>
					<tr>
						<th>⬇️ { t('admin_dashboard.column_name') }</th>
						<th>{ t('admin_dashboard.column_email') }</th>
						<th>{ t('admin_dashboard.column_role') }</th>
						<th>{ t('admin_dashboard.column_created') }</th>
					</tr>
				</thead>
				<tbody>
					{#each users as user}
						<tr>
							<td>{user.name}</td>
							<td>{user.email}</td>
							<td>{user.isAdmin ? t('user_types.admin') : t('user_types.user')}</td>
							<td>
								<FormatDate date={user.createdDate} nullLabel="–" />
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>
	</div>
</main>
