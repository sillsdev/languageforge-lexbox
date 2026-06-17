<script lang="ts">
  import { page } from '$app/state';
  import CopyToClipboardButton from '$lib/components/CopyToClipboardButton.svelte';
  import { FormField } from '$lib/forms';
  import t from '$lib/i18n';
  import { AdminContent } from '$lib/layout';

  interface Props {
    projectCode: string;
  }

  const { projectCode }: Props = $props();

  let projectHgUrl = $derived(import.meta.env.DEV ? `http://hg.${page.url.hostname}/${projectCode}`
    : page.url.host.includes('develop') || page.url.host.includes('.dev') ? `https://hg-develop.lexbox.org/${projectCode}`
    : page.url.host.includes('staging') ? `https://hg-staging.languageforge.org/${projectCode}`
    : `https://hg-public.languageforge.org/${projectCode}`);
</script>

<AdminContent>
  <FormField label={$t('project_page.get_project.send_receive_url')}>
    <div class="join">
      <input value={projectHgUrl} class="input input-bordered join-item w-full focus:input-success" readonly />
      <CopyToClipboardButton textToCopy={projectHgUrl} join />
    </div>
  </FormField>
</AdminContent>
