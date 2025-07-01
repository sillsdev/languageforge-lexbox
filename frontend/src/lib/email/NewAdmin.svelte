<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import t from '$lib/i18n';

  interface Props {
    name: string;
    loggedInAdminName: string;
    loggedInAdminEmail: string;
    adminName: string;
    adminEmail: string;
  }

  const { name, adminName, adminEmail, loggedInAdminName, loggedInAdminEmail }: Props = $props();
</script>

<Email subject={$t('emails.new_admin.subject', {adminName})} {name}>
  <mj-text>{$t('emails.new_admin.body', {adminName, adminEmail, loggedInAdminName, loggedInAdminEmail})}</mj-text>
</Email>
