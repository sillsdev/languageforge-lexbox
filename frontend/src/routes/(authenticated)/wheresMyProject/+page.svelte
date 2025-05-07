<script lang="ts">
  import { TitlePage, HomeBreadcrumb } from '$lib/layout';
  import t from '$lib/i18n';
  import Markdown from 'svelte-exmarkdown';
  import FeatureFlagAlternateContent from '$lib/layout/FeatureFlagAlternateContent.svelte';
  import Button from '$lib/forms/Button.svelte';
  import { page } from '$app/state';
  import { _sendFWLiteBetaRequestEmail } from './+page';
  import type {UUID} from 'crypto';

  async function requestBetaAccess(): Promise<void> {
    await _sendFWLiteBetaRequestEmail(page.data.user.id as UUID, page.data.user.name);
  }
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
        <Button on:click={requestBetaAccess}>{$t('where_is_my_project.request_beta_access')}</Button>
      {/snippet}
    </FeatureFlagAlternateContent>
  </div>
</TitlePage>

<style>
</style>
