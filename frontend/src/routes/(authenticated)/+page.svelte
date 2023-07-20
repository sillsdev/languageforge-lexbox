<script lang="ts">
  import type { PageData } from './$types';
  import UserDashboard from './(dashboards)/UserDashboard.svelte';

  export let data: PageData;
  $: projects = data.projects;
</script>

<UserDashboard projects={$projects} />
