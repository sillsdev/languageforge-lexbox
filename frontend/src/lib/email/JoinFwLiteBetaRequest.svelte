<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import t from '$lib/i18n';

  export let name: string;
  export let email: string;
  export let baseUrl: string;
  let approveUrl = new URL(`/admin/?userSearch=${encodeURIComponent(email)}`, baseUrl);
</script>

<Email subject={$t('emails.join_fw_lite_beta_request_email.subject', { name })}>
  <mj-text>{$t('emails.join_fw_lite_beta_request_email.body', { name })}</mj-text>
  <mj-button href={approveUrl}>{$t('emails.join_fw_lite_beta_request_email.approve_button')}</mj-button>
</Email>
