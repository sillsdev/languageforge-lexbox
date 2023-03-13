<script lang="ts">
    import UserDashboard from "../UserDashboard.svelte";
    import type {PageData} from './$types';

    export let data: PageData;
</script>

<UserDashboard projects={data.projects}/>