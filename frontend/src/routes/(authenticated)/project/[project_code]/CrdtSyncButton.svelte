<script lang="ts">
  import {NewTabLinkRenderer} from '$lib/components/Markdown';
  import ConfirmModal from '$lib/components/modals/ConfirmModal.svelte';
  import {tryGetErrorMessage} from '$lib/error/utils';
  import {Button} from '$lib/forms';
  import t from '$lib/i18n';
  import {Icon} from '$lib/icons';
  import {useNotifications} from '$lib/notify';
  import Markdown from 'svelte-exmarkdown';
  import {bounceIn} from 'svelte/easing';
  import {scale} from 'svelte/transition';
  import type {Project} from './+page';

  export let project: Project;
  export let hasHarmonyCommits: boolean;

  const { notifySuccess, notifyWarning } = useNotifications();

  let syncing = false;

  async function triggerSync(): Promise<string | undefined> {
    syncing = true;
    try {
      const response = await fetch(`/api/crdt/sync/${project.id}`, {
        method: 'POST',
      });

      if (response.ok) {
        const { crdtChanges, fwdataChanges } = await response.json();
        notifySuccess(`Synced successfully (${fwdataChanges} FwData changes. ${crdtChanges} CRDT changes)`);
      } else {
        const error = `Failed to sync: ${response.statusText} (${response.status})`;
        notifyWarning(error);
        console.error(error, await response.text());
        return error;
      }
    } catch (error) {
      return tryGetErrorMessage(error);
    } finally {
      syncing = false;
    }
  }

  async function useInFwLite(): Promise<void> {
    await confirmModal.open(async () => {
      return await triggerSync();
    });
  }
  let confirmModal: ConfirmModal;
</script>

{#if hasHarmonyCommits}
  <Button variant="btn-primary" class="gap-1" on:click={triggerSync} loading={syncing} customLoader>
    FwData
    <Icon icon="i-mdi-sync" spin={syncing} spinReverse />
    CRDT
  </Button>
{:else}
  <Button variant="btn-primary" on:click={useInFwLite}>{$t('project.crdt.try_fw_lite')}</Button>
  <ConfirmModal bind:this={confirmModal} hideActions={syncing} showDoneState let:done let:error
    title={$t('project.crdt.try_fw_lite')}
    submitText={$t('project.crdt.submit')}
    cancelText={$t('project.crdt.cancel')}
    >
    {#if syncing}
      <div class="text-center">
        <p class="mb-2 label justify-center">
          {$t('project.crdt.making_available')}
        </p>
        <span class="loading loading-lg" />
      </div>
    {:else if done && !error}
      <div class="text-center">
        <p class="mb-2 label justify-center underline-links">
          <Markdown md={$t('project.crdt.now_available')} plugins={[{ renderer: { a: NewTabLinkRenderer } }]} />
        </p>
        <span
          class="i-mdi-check-circle-outline text-7xl text-success"
          transition:scale={{ duration: 600, start: 0.7, easing: bounceIn }}
        />
      </div>
    {:else}
      <div class="prose max-w-none underline-links">
        <Markdown md={$t('project.crdt.try_info')} plugins={[{ renderer: { a: NewTabLinkRenderer } }]} />
        {#if error}
          <Markdown
            md={`${$t('errors.apology')} ${$t('project.crdt.reach_out_for_help', { subject: encodeURIComponent($t('project.crdt.email_subject', { projectCode: project.code }))})}`}
            plugins={[{ renderer: { a: NewTabLinkRenderer } }]} />
        {/if}
      </div>
    {/if}
  </ConfirmModal>
{/if}
