<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import t from '$lib/i18n';

  export let name: string;
</script>

<Email subject={$t('emails.password_changed.subject')} {name}>
  <mj-text>{$t('emails.password_changed.body')}</mj-text>
</Email>
