<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import t from '$lib/i18n';

  export let name: string;
  export let verifyUrl: string;
  export let newAddress: boolean;
</script>

<Email subject={$t('emails.verify_email.subject')} {name}>
  {#if newAddress}
    <mj-text>{$t('emails.verify_email.to_verify_and_finish_changing_click')}</mj-text>
  {:else}
    <mj-text>{$t('emails.verify_email.to_verify_click')}</mj-text>
  {/if}
  <mj-button href={verifyUrl}>{$t('emails.verify_email.verify_button')}</mj-button>
  <mj-text>{$t('emails.link_expiration_warning')}</mj-text>
</Email>
