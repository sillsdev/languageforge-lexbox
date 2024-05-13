<script lang="ts">
  import { TitlePage } from '$lib/layout';
  import t from '$lib/i18n';
  import CreateUser from '$lib/components/Users/CreateUser.svelte';
</script>

<TitlePage title={$t('register.title')}>
  <CreateUser />
</TitlePage>
