<script lang="ts">
  import { TitlePage, HomeBreadcrumb } from '$lib/layout';
  import t from '$lib/i18n';
  import Markdown from 'svelte-exmarkdown';
  import FeatureFlagAlternateContent from '$lib/layout/FeatureFlagAlternateContent.svelte';
</script>

<HomeBreadcrumb />
<TitlePage title={$t('where_is_my_project.title')}>
  <div class="prose text-lg">
    <FeatureFlagAlternateContent flag="FwLiteBeta">
      {#snippet hasFlagContent()}
        <Markdown md={$t('where_is_my_project.body')} />
      {/snippet}
      {#snippet missingFlagContent()}
        <Markdown md={$t('where_is_my_project.user_not_in_beta')} />
      {/snippet}
    </FeatureFlagAlternateContent>
  </div>
</TitlePage>

<style>
</style>
