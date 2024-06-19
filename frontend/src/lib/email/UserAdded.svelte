<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import t from '$lib/i18n';

  export let name: string;
  export let baseUrl: string;
  export let projectName: string;
  export let projectCode: string;
  let projectUrl = new URL(`/?projectSearch=${encodeURIComponent(projectCode)}`, baseUrl);
</script>

<Email subject={$t('emails.user_added.subject', {projectName})} {name}>
  <mj-text>{$t('emails.user_added.body', {projectName})}</mj-text>
  <mj-button href={projectUrl}>{$t('emails.user_added.view_button')}</mj-button>
</Email>
