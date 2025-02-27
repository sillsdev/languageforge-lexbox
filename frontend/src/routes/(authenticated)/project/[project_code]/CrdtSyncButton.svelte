<script lang="ts">
  import {tryGetErrorMessage} from '$lib/error/utils';
  import {Button, FormError} from '$lib/forms';
  import t from '$lib/i18n';
  import {Icon} from '$lib/icons';
  import {useNotifications} from '$lib/notify';
  import {bounceIn} from 'svelte/easing';
  import {scale} from 'svelte/transition';
  import type {Project} from './+page';
  import {Modal} from '$lib/components/modals';
  import {NewTabLinkMarkdown} from '$lib/components/Markdown';

  export let project: Project;
  export let hasHarmonyCommits: boolean;
  type SyncResult = {crdtChanges: number, fwdataChanges: number};

  const { notifySuccess, notifyWarning } = useNotifications();

  let syncing = false;
  let done = false;
  $: state = done ? 'done' : syncing ? 'syncing' : 'idle';
  let error: string | undefined = undefined;

  async function triggerSync(): Promise<string | undefined> {
    syncing = true;
    try {
      const response = await fetch(`/api/fw-lite/sync/trigger/${project.id}`, {
        method: 'POST',
      });

      if (response.ok) {
        const syncResults = await awaitSyncFinished();
        if (typeof syncResults === 'string') {
          return syncResults;
        }
        notifySuccess($t('project.crdt.sync_result', { fwdataChanges: syncResults.fwdataChanges, crdtChanges: syncResults.crdtChanges }));
        done = true;
        return;
      }
      const error = `Failed to sync: ${response.statusText} (${response.status})`;
      notifyWarning(error);
      console.error(error, await response.text());
      return error;
    } catch (error) {
      return tryGetErrorMessage(error);
    } finally {
      syncing = false;
    }
  }

  async function awaitSyncFinished(): Promise<SyncResult | string> {
    while (true) {
      try {
        const response = await fetch(`/api/fw-lite/sync/await-sync-finished/${project.id}`, {signal: AbortSignal.timeout(30_000)});
        if (response.status === 500) {
          return 'Sync failed, please contact support';
        }
        if (response.status === 200) {
          const result = await response.json() as SyncResult;
          return result;
        }
      } catch (error) {
        if (error instanceof DOMException && (error.name === 'AbortError' || error.name === 'TimeoutError')) {
          continue;
        }
        return tryGetErrorMessage(error) ?? 'Unknown error';
      }

    }
  }

  async function onSubmit(): Promise<void> {
    error = await triggerSync();
  }

  async function syncProject(): Promise<void> {
    let error = await triggerSync();
    if (error) notifyWarning(error);
  }

  async function useInFwLite(): Promise<void> {
    await modal.openModal();
  }
  let modal: Modal;
</script>

{#if hasHarmonyCommits}
  <Button variant="btn-primary" class="gap-1 indicator" on:click={syncProject} loading={state === 'syncing'} active={state === 'syncing'} customLoader>
    <span class="indicator-item badge badge-sm badge-accent translate-x-[calc(50%-16px)] shadow">Beta</span>
    {$t('project.crdt.sync_fwlite')}
    <span style="transform: rotateY(180deg)">
      <Icon icon="i-mdi-sync" spin={state === 'syncing'} spinReverse/>
    </span>
  </Button>
{:else}
  <Button variant="btn-primary" class="indicator" on:click={useInFwLite}>
    <span class="indicator-item badge badge-sm badge-accent translate-x-[calc(50%-16px)] shadow">Beta</span>
    <span>
      {$t('project.crdt.try_fw_lite')}
    </span>
  </Button>
  <Modal bind:this={modal} showCloseButton={false} hideActions={state === 'syncing'} closeOnClickOutside={false}>
    <h2 class="text-xl mb-6">
      {#if state === 'syncing'}
        {$t('project.crdt.making_available')}
      {:else if state === 'done'}
        {$t('project.crdt.now_available')}
      {:else}
        {$t('project.crdt.try_fw_lite')}
      {/if}
    </h2>
    {#if state === 'syncing'}
      <div class="mb-6 prose max-w-none underline-links">
        <NewTabLinkMarkdown md={$t('project.crdt.while_you_wait')} />
      </div>
      <p class="text-center">
        <span class="loading loading-lg"></span>
      </p>
    {:else if state === 'done'}
      <div class="prose max-w-none underline-links">
        <NewTabLinkMarkdown md={$t('project.crdt.to_start_using')} />
        <p class="text-center">
          <span
            class="i-mdi-check-circle-outline text-7xl text-center text-success"
            transition:scale={{ duration: 600, start: 0.7, easing: bounceIn }}
          ></span>
        </p>
      </div>
    {:else}
      <div class="prose max-w-none underline-links">
        <NewTabLinkMarkdown md={$t('project.crdt.try_info')} />
        {#if error}
          <NewTabLinkMarkdown
            md={`${$t('errors.apology')} ${$t('project.crdt.reach_out_for_help', { subject: encodeURIComponent($t('project.crdt.email_subject', { projectCode: project.code }))})}`}
          />
        {/if}
      </div>
      <FormError {error} right/>
    {/if}
    <svelte:fragment slot="actions" let:close>
      {#if state === 'idle'}
        <Button variant="btn-primary" on:click={onSubmit}>
          {$t('project.crdt.submit')}
        </Button>
        <Button on:click={close}>
          {$t('project.crdt.cancel')}
        </Button>
      {:else if state === 'done'}
        <Button variant="btn-primary" on:click={close}>
          {$t('project.crdt.finish')}
        </Button>
      {/if}
    </svelte:fragment>
  </Modal>
{/if}
