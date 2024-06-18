<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import t from '$lib/i18n';

  export let name: string;
  export let projectName: string;
  export let projectUrl: string;
</script>

<Email subject={$t('emails.approve_project_request_email.subject', {projectName})} {name}>
  <mj-text>{$t('emails.approve_project_request_email.heading', {projectName})}</mj-text>
  <mj-button href={projectUrl}>{$t('emails.approve_project_request_email.view_button')}</mj-button>
</Email>
