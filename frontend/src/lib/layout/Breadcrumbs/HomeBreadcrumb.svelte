<script>
  import {resolve} from '$app/paths';
  import PageBreadcrumb from './PageBreadcrumb.svelte';
</script>

<PageBreadcrumb href={resolve('/')}>
<!--  aria label require for screen readers and automated testing -->
  <span aria-label="home" class="i-mdi-home-outline text-lg mb-[-4px]"></span>
</PageBreadcrumb>
