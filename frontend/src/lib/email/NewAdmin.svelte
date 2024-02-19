<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import t from '$lib/i18n';

  export let name: string;
  export let adminName: string;
  export let adminEmail: string;
</script>

<Email subject={$t('emails.new_admin.subject', {adminName})} {name}>
  <mj-text>{$t('emails.new_admin.body', {adminName, adminEmail})}</mj-text>
</Email>
