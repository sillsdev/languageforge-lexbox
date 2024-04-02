<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import t from '$lib/i18n';
  import { parseSimpleTimespan, type TimespanComponent } from '$lib/util/timespan';

  export let name: string;
  export let resetUrl: string;
  export let lifetime: string;

  let timecount: number;
  let timetype: TimespanComponent = 'days';
  let expirationText: `emails.link_expiration_${TimespanComponent}`;
  $: expirationText = `emails.link_expiration_${timetype}`;
  $: [timecount, timetype] = parseSimpleTimespan(lifetime);
  let expirationParam: Record<TimespanComponent, number> = {};
  $: { expirationParam = {}; expirationParam[timetype] = timecount; }
</script>

<Email subject={$t('emails.forgot_password.subject')} {name}>
  <mj-text>{$t('emails.forgot_password.to_reset_click')}</mj-text>
  <mj-button href={resetUrl}>{$t('emails.forgot_password.reset_button')}</mj-button>
  <mj-text>{$t(expirationText, expirationParam)}</mj-text>
</Email>
