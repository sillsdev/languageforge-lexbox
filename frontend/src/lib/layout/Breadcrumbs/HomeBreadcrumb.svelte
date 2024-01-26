<script>
  import t from '$lib/i18n';
  import PageBreadcrumb from './PageBreadcrumb.svelte';
</script>

<PageBreadcrumb href="/">
  <span class="inline-flex gap-1 items-center">
    {$t('user_dashboard.home_title')}
    <span class="i-mdi-home-outline text-sm mb-[-1px]" />
  </span>
</PageBreadcrumb>
