<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import t from '$lib/i18n';
  import { toI18nKey } from '$lib/util/timespan';

  export let verifyUrl: string;
  export let projectName: string;
  export let managerName: string;
  export let lifetime: string;

  $: [expirationText, expirationParam] = toI18nKey(lifetime);
  let template: 'emails.create_account_request_email' | 'emails.create_account_without_project_request_email';
  $: template = projectName ? 'emails.create_account_request_email' : 'emails.create_account_without_project_request_email';
</script>

<Email subject={$t(`${template}.subject`, {projectName})} name="">
  <mj-text>{$t(`${template}.body`, {managerName, projectName})}</mj-text>
  <mj-button href={verifyUrl}>{$t(`${template}.join_button`)}</mj-button>
  <mj-text>{$t(expirationText, expirationParam)}</mj-text>
</Email>
