<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import t from '$lib/i18n';
  import { toI18nKey } from '$lib/util/timespan';

  interface Props {
    name: string;
    resetUrl: string;
    lifetime: string;
  }

  let { name, resetUrl, lifetime }: Props = $props();

  let [expirationText, expirationParam] = $derived(toI18nKey(lifetime));
</script>

<Email subject={$t('emails.forgot_password.subject')} {name}>
  <mj-text>{$t('emails.forgot_password.to_reset_click')}</mj-text>
  <mj-button href={resetUrl}>{$t('emails.forgot_password.reset_button')}</mj-button>
  <mj-text>{$t(expirationText, expirationParam)}</mj-text>
</Email>
