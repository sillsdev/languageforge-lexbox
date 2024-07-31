<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import t from '$lib/i18n';

  export let approveUrl: string;
  export let managerName: string;
  export let requestingUserName: string;
  export let projectName: string;
</script>

<Email subject={$t('emails.join_project_request_email.subject', {requestingUserName, projectName})} name={managerName}>
  <mj-text>{$t('emails.join_project_request_email.body', {requestingUserName, projectName})}</mj-text>
  <mj-button href={approveUrl}>{$t('emails.join_project_request_email.approve_button')}</mj-button>
</Email>
