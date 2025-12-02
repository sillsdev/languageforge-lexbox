<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import type { CreateProjectInput } from '$lib/gql/types';
  import t from '$lib/i18n';

  interface Props {
    name: string;
    baseUrl: string;
    project: CreateProjectInput;
  }

  const { name, baseUrl, project }: Props = $props();
  let projectUrl = $derived(new URL(`/?projectSearch=${encodeURIComponent(project.code)}`, baseUrl));
  let projectName = $derived(project.name);
</script>

<Email subject={$t('emails.approve_project_request_email.subject', {projectName})} {name}>
  <mj-text>{$t('emails.approve_project_request_email.heading', {projectName})}</mj-text>
  <mj-button href={projectUrl}>{$t('emails.approve_project_request_email.view_button')}</mj-button>
</Email>
