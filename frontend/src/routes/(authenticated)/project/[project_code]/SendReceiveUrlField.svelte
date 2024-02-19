<script lang="ts">
  import { page } from '$app/stores';
  import IconButton from '$lib/components/IconButton.svelte';
  import { FormField } from '$lib/forms';
  import t from '$lib/i18n';
  import { AdminContent } from '$lib/layout';
  import { delay } from '$lib/util/time';

  export let projectCode: string;

  $: projectHgUrl = import.meta.env.DEV
    ? `http://hg.${$page.url.hostname}/${projectCode}`
    : `https://hg-public.${$page.url.host.replace('depot', 'forge')}/${projectCode}`;

  var copyingToClipboard = false;
  var copiedToClipboard = false;

  async function copyProjectUrlToClipboard(): Promise<void> {
    copyingToClipboard = true;
    await navigator.clipboard.writeText(projectHgUrl);
    copiedToClipboard = true;
    copyingToClipboard = false;
    await delay();
    copiedToClipboard = false;
  }
</script>

<AdminContent>
  <FormField label={$t('project_page.get_project.send_receive_url')}>
    <div class="join">
      <input value={projectHgUrl} class="input input-bordered join-item w-full focus:input-success" readonly />
      <div class="join-item tooltip-open" class:tooltip={copiedToClipboard} data-tip={$t('clipboard.copied')}>
        {#if copiedToClipboard}
          <IconButton fake icon="i-mdi-check" variant="btn-success" />
        {:else}
          <div class="contents">
            <IconButton
              loading={copyingToClipboard}
              icon="i-mdi-content-copy"
              on:click={copyProjectUrlToClipboard}
            />
          </div>
        {/if}
      </div>
    </div>
  </FormField>
</AdminContent>
