<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import t from '$lib/i18n';

  interface Props {
    managerName: string;
    requestingUserName: string;
    requestingUserId: string;
    projectCode: string;
    projectName: string;
    baseUrl: string;
  }

  const {
    managerName,
    requestingUserName,
    requestingUserId,
    projectCode,
    projectName,
    baseUrl
  }: Props = $props();
  let approveUrl = $derived(new URL(`/project/${projectCode}?addUserId=${requestingUserId}&addUserName=${encodeURIComponent(requestingUserName)}`, baseUrl));
</script>

<Email subject={$t('emails.join_project_request_email.subject', {requestingUserName, projectName})} name={managerName}>
  <mj-text>{$t('emails.join_project_request_email.body', {requestingUserName, projectName})}</mj-text>
  <mj-button href={approveUrl}>{$t('emails.join_project_request_email.approve_button')}</mj-button>
</Email>
