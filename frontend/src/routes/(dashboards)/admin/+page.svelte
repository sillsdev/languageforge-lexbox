<script lang="ts">
    import type {PageData} from './$types';
    import AdminDashboard from "../AdminDashboard.svelte";

    export let data: PageData;
</script>

<AdminDashboard projects={data.projects}/>