<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import t from '$lib/i18n';

  interface Props {
    name: string;
    adminName: string;
    adminEmail: string;
  }

  const { name, adminName, adminEmail }: Props = $props();
</script>

<Email subject={$t('emails.new_admin.subject', {adminName})} {name}>
  <mj-text>{$t('emails.new_admin.body', {adminName, adminEmail})}</mj-text>
</Email>
