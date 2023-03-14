<script lang="ts">
    import type { PageData } from './$types';
    import { Page } from "$lib/layout";
    import FormatDate from "$lib/components/FormatDate.svelte";
    import Input from "$lib/forms/Input.svelte";

    export let data: PageData;
    let search = '';
    $: searchLower = search.toLocaleLowerCase();
    $: projects = data.projects.filter(p => !search 
    || p.name.toLocaleLowerCase().includes(searchLower) 
    || p.code.toLocaleLowerCase().includes(searchLower));
</script>
<Page>
    <svelte:fragment slot=header>
        Projects
    </svelte:fragment>
    <Input type="text" label="Filter" placeholder="search..." autofocus bind:value={search}/>
    <div class="divider"></div>
    <table class="table w-full">
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
                <td>{project.name}</td>
                <td>{project.code}</td>
                <td>{project.projectUsersAggregate.aggregate?.count ?? 0}</td>
                <td><FormatDate date={project.lastCommit} nullLabel="unknown"/></td>
            </tr>
        {/each}
        </tbody>
    </table>
</Page>
