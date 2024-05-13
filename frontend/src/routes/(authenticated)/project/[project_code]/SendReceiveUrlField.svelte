<script lang="ts">
  import { page } from '$app/stores';
  import CopyToClipboardButton from '$lib/components/CopyToClipboardButton.svelte';
  import { FormField } from '$lib/forms';
  import t from '$lib/i18n';
  import { AdminContent } from '$lib/layout';

  export let projectCode: string;

  $: projectHgUrl = import.meta.env.DEV ? `http://hg.${$page.url.hostname}/${projectCode}`
    : $page.url.host.includes('develop') || $page.url.host.includes('.dev') ? `https://hg-develop.lexbox.org/${projectCode}`
    : $page.url.host.includes('staging') ? `https://hg-staging.${$page.url.host.replace('depot', 'forge')}/${projectCode}`
    : `https://hg-public.${$page.url.host.replace('depot', 'forge')}/${projectCode}`;
</script>

<AdminContent>
  <FormField label={$t('project_page.get_project.send_receive_url')}>
    <div class="join">
      <input value={projectHgUrl} class="input input-bordered join-item w-full focus:input-success" readonly />
      <CopyToClipboardButton textToCopy={projectHgUrl} />
    </div>
  </FormField>
</AdminContent>
