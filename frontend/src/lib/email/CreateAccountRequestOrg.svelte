<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import t from '$lib/i18n';
  import { toI18nKey } from '$lib/util/timespan';

  export let verifyUrl: string;
  export let orgName: string;
  export let managerName: string;
  export let lifetime: string;

  $: [expirationText, expirationParam] = toI18nKey(lifetime);
</script>

<Email subject={$t('emails.create_account_request_email_org.subject', {orgName})} name="">
  <mj-text>{$t('emails.create_account_request_email_org.body', {managerName, orgName})}</mj-text>
  <mj-button href={verifyUrl}>{$t('emails.create_account_request_email_org.join_button')}</mj-button>
  <mj-text>{$t(expirationText, expirationParam)}</mj-text>
</Email>
