<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import t from '$lib/i18n';
  import { toI18nKey } from '$lib/util/timespan';

  interface Props {
    name: string;
    verifyUrl: string;
    newAddress: boolean;
    lifetime: string;
  }

  const {
    name,
    verifyUrl,
    newAddress,
    lifetime
  }: Props = $props();

  let [expirationText, expirationParam] = $derived(toI18nKey(lifetime));
</script>

<Email subject={$t('emails.verify_email.subject')} {name}>
  {#if newAddress}
    <mj-text>{$t('emails.verify_email.to_verify_and_finish_changing_click')}</mj-text>
  {:else}
    <mj-text>{$t('emails.verify_email.to_verify_click')}</mj-text>
  {/if}
  <mj-button href={verifyUrl}>{$t('emails.verify_email.verify_button')}</mj-button>
  <mj-text>{$t(expirationText, expirationParam)}</mj-text>
</Email>
