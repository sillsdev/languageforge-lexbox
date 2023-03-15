<script lang="ts">
    import type {PageData} from './$types';
    import {Page} from "$lib/layout";
    import FormatDate from "$lib/components/FormatDate.svelte";
    import Input from "$lib/forms/Input.svelte";

    export let data: PageData;
    let projectSearch = '';
    $: projectSearchLower = projectSearch.toLocaleLowerCase();
    $: projects = data.projects.filter(p => !projectSearch
        || p.name.toLocaleLowerCase().includes(projectSearchLower)
        || p.code.toLocaleLowerCase().includes(projectSearchLower)).slice(0, projectSearch ? undefined : 10);

    let userSearch = '';
    $: userSearchLower = userSearch.toLocaleLowerCase();
    $: users = data.users.filter(u => !userSearch
    || u.name.toLocaleLowerCase().includes(userSearchLower)
    || u.email.toLocaleLowerCase().includes(userSearchLower)).slice(0, userSearch ? undefined : 10);
</script>
<main>
	<div class="grid grid-cols-2">
		<div class="overflow-x-auto">
			<span class="text-xl">Projects</span>
			<Input
				type="text"
				label="Filter"
				placeholder="search..."
				autofocus
				bind:value={projectSearch}
			/>
			<div class="divider" />
			<table class="table">
				<thead>
					<tr>
						<th>Name</th>
						<th>Code</th>
						<th>Users</th>
						<th>⬇️ Last Change</th>
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
								<FormatDate date={project.lastCommit} nullLabel="unknown" />
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>
		<div class="overflow-x-auto">
			<span class="text-xl">Users</span>
			<Input type="text" label="Filter" placeholder="search..." autofocus bind:value={userSearch} />
			<div class="divider" />
			<table class="table">
				<thead>
					<tr>
						<th>⬇️ Name</th>
						<th>Email</th>
						<th>Role</th>
						<th>Created</th>
					</tr>
				</thead>
				<tbody>
					{#each users as user}
						<tr>
							<td>{user.name}</td>
							<td>{user.email}</td>
							<td>{user.isAdmin ? 'Admin' : 'User'}</td>
							<td>
								<FormatDate date={user.createdDate} nullLabel="unknown" />
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>
	</div>
</main>
