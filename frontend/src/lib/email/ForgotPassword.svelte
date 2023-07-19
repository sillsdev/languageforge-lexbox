<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import t from '$lib/i18n';

  export let name: string;
  export let resetUrl: string;
</script>

<Email subject={$t('emails.forgot_password.subject')} {name}>
  <mj-text>{$t('emails.forgot_password.to_reset_click')}</mj-text>
  <mj-button href={resetUrl}>{$t('emails.forgot_password.reset_button')}</mj-button>
  <mj-text>{$t('emails.shared.or_nvaigate_to_url')} {resetUrl}</mj-text>
</Email>
