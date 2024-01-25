<script>
  import { Layout } from '$lib/layout';
  import PageBreadcrumb from '$lib/layout/Breadcrumbs/PageBreadcrumb.svelte';
  import t from '$lib/i18n';
</script>
<PageBreadcrumb href="/">{$t('user_dashboard.home_title')}</PageBreadcrumb>
<Layout>
  <slot />
</Layout>
