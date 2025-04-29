<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import t from '$lib/i18n';
  import { toI18nKey } from '$lib/util/timespan';

  interface Props {
    verifyUrl: string;
    orgName: string;
    managerName: string;
    lifetime: string;
  }

  let {
    verifyUrl,
    orgName,
    managerName,
    lifetime
  }: Props = $props();

  let [expirationText, expirationParam] = $derived(toI18nKey(lifetime));
</script>

<Email subject={$t('emails.create_account_request_email_org.subject', {orgName})} name="">
  <mj-text>{$t('emails.create_account_request_email_org.body', {managerName, orgName})}</mj-text>
  <mj-button href={verifyUrl}>{$t('emails.create_account_request_email_org.join_button')}</mj-button>
  <mj-text>{$t(expirationText, expirationParam)}</mj-text>
</Email>
