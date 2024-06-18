<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import t from '$lib/i18n';

  export let name: string;
</script>

<Email subject={$t('emails.approve_project_request_email.subject')} {name}>
  <mj-text>$t('emails.approve_project_request_email.heading')</mj-text>
</Email>
