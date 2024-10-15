<script lang="ts">
  import { TitlePage, HomeBreadcrumb } from '$lib/layout';
  import t from '$lib/i18n';
  import Markdown from 'svelte-exmarkdown';
</script>

<HomeBreadcrumb/>
<TitlePage title={$t('about.title')}>
  <div class="prose text-lg">
    <Markdown md={$t('login.welcome')}/>
  </div>
</TitlePage>
