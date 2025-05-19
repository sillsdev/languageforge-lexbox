<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import t from '$lib/i18n';
  import { toI18nKey } from '$lib/util/timespan';

  interface Props {
    verifyUrl: string;
    projectName: string;
    managerName: string;
    lifetime: string;
  }

  const { verifyUrl, projectName, managerName, lifetime }: Props = $props();

  let [expirationText, expirationParam] = $derived(toI18nKey(lifetime));
</script>

<Email subject={$t('emails.create_account_request_email_project.subject', {projectName})} name="">
  <mj-text>{$t('emails.create_account_request_email_project.body', {managerName, projectName})}</mj-text>
  <mj-button href={verifyUrl}>{$t('emails.create_account_request_email_project.join_button')}</mj-button>
  <mj-text>{$t(expirationText, expirationParam)}</mj-text>
</Email>
