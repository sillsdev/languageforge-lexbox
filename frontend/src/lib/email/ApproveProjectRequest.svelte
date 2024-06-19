<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import type { CreateProjectInput } from '$lib/gql/types';
  import t from '$lib/i18n';

  export let name: string;
  export let baseUrl: string;
  export let project: CreateProjectInput;
  let projectUrl = new URL(`/project/${project.code}`, baseUrl);
  let projectName = project.name;
</script>

<Email subject={$t('emails.approve_project_request_email.subject', {projectName})} {name}>
  <mj-text>{$t('emails.approve_project_request_email.heading', {projectName})}</mj-text>
  <mj-button href={projectUrl}>{$t('emails.approve_project_request_email.view_button')}</mj-button>
</Email>
