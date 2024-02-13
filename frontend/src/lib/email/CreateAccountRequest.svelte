<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import t from '$lib/i18n';

  export let name: string;
  export let verifyUrl: string;
</script>

<Email subject={$t('emails.create_account_request_email.subject'/*, {projectName: projectName}*/)} {name}>
  <mj-text>{$t('emails.create_account_request_email.body')}</mj-text>
  <mj-button href={verifyUrl}>{$t('emails.create_account_request_email.join_button')}</mj-button>
</Email>
