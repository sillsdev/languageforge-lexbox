<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import t from '$lib/i18n';

  interface Props {
    name: string;
    baseUrl: string;
    projectName: string;
    projectCode: string;
  }

  const {
    name,
    baseUrl,
    projectName,
    projectCode
  }: Props = $props();
  let projectUrl = $derived(new URL(`/?projectSearch=${encodeURIComponent(projectCode)}`, baseUrl));
</script>

<Email subject={$t('emails.user_added.subject', {projectName})} {name}>
  <mj-text>{$t('emails.user_added.body', {projectName})}</mj-text>
  <mj-button href={projectUrl}>{$t('emails.user_added.view_button')}</mj-button>
</Email>
