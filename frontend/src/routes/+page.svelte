<script lang="ts">
    import type { PageData } from './$types';
    import UserDashboard from "./(dashboards)/UserDashboard.svelte";

    export let data: PageData;
</script>

<UserDashboard projects={data.projects}/>