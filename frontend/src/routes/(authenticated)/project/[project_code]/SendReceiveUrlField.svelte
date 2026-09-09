<script lang="ts">
  import { page } from '$app/state';
  import CopyToClipboardButton from '$lib/components/CopyToClipboardButton.svelte';
  import { FormField, Input } from '$lib/forms';
  import t from '$lib/i18n';
  import { buildSendReceiveUrl } from '$lib/util/sendReceiveUrl';

  interface Props {
    projectCode: string;
    login: string;
  }

  const { projectCode, login }: Props = $props();

  let password = $state<string | null>('');
  let projectHgUrl = $derived(
    buildSendReceiveUrl(
      login,
      password ?? '',
      projectCode,
      import.meta.env.DEV ? page.url.hostname : page.url.host,
      import.meta.env.DEV,
    ),
  );
</script>

<Input
  bind:value={password}
  type="password"
  autocomplete="off"
  label={$t('project_page.get_project.send_receive_password')}
  description={$t('project_page.get_project.send_receive_password_description')}
/>
<FormField label={$t('project_page.get_project.send_receive_url')}>
  <div class="join">
    <input value={projectHgUrl} class="input input-bordered join-item w-full focus:input-success" readonly />
    <CopyToClipboardButton textToCopy={projectHgUrl} join />
  </div>
</FormField>
