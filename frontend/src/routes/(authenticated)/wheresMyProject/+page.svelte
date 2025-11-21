<script lang="ts">
  import type { PageData } from './$types';
  import { TitlePage } from '$lib/layout';
  import t from '$lib/i18n';
  import Markdown from 'svelte-exmarkdown';
  import FeatureFlagAlternateContent from '$lib/layout/FeatureFlagAlternateContent.svelte';
  import Button from '$lib/forms/Button.svelte';
  import { page } from '$app/state';
  import { _sendFWLiteBetaRequestEmail } from './+page';
  import type { UUID } from 'crypto';
  import { ProjectRole, SendFwLiteBetaRequestEmailResult } from '$lib/gql/generated/graphql';
  import { useNotifications } from '$lib/notify';

  interface Props {
    data: PageData;
  }

  const { data }: Props = $props();

  const user = $derived(data.user);
  const managesProjects = $derived(user.isAdmin || user.projects.some(proj => proj.role === ProjectRole.Manager));

  const { notifySuccess } = useNotifications();

  let requesting = $state(false);

  async function requestBetaAccess(): Promise<void> {
    requesting = true;
    try {
      const gqlResult = await _sendFWLiteBetaRequestEmail(page.data.user.id as UUID, page.data.user.name);
      if (gqlResult.error) {
        if (gqlResult.error.byType('NotFoundError')) {
          console.log('User not found, no dialog shown');
        }
      }
      const result = gqlResult.data?.sendFWLiteBetaRequestEmail.sendFWLiteBetaRequestEmailResult;
      if (result === SendFwLiteBetaRequestEmailResult.BetaAccessRequestSent) {
        notifySuccess($t('where_is_my_project.access_request_sent'));
      }
      if (result === SendFwLiteBetaRequestEmailResult.UserAlreadyInBeta) {
        notifySuccess($t('where_is_my_project.already_in_beta'));
      }
    } finally {
      requesting = false;
    }
  }
</script>

<TitlePage title={$t('where_is_my_project.title')}>
  <div class="prose text-lg">
    <FeatureFlagAlternateContent flag="FwLiteBeta">
      {#snippet hasFlagContent()}
        <Markdown md={$t('where_is_my_project.body')} />
      {/snippet}
      {#snippet missingFlagContent()}
        {#if managesProjects}
          <Markdown md={$t('where_is_my_project.user_not_in_beta')} />
          <div class="text-center">
            <Button loading={requesting} variant="btn-primary" onclick={requestBetaAccess}>{$t('where_is_my_project.request_beta_access')}</Button>
          </div>
        {:else}
          <Markdown md={$t('where_is_my_project.non_manager_not_in_beta')} />
        {/if}
      {/snippet}
    </FeatureFlagAlternateContent>
  </div>
</TitlePage>
