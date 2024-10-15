<script lang="ts">
  import { HeaderPage, HomeBreadcrumb } from '$lib/layout';
  import t from '$lib/i18n';
  import Markdown from 'svelte-exmarkdown';
  import AppLogo from '$lib/icons/AppLogo.svelte';
</script>

<HomeBreadcrumb/>
<HeaderPage wide title={$t('about.title')}>
  <div class="prose text-lg">
    <Markdown md={$t('login.welcome')}/>
  </div>
</HeaderPage>
