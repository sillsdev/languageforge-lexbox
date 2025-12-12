<script lang="ts">
  import {getErrorMessage} from '$lib/error/utils';
  import {Button, FormError} from '$lib/forms';
  import t from '$lib/i18n';
  import {Icon} from '$lib/icons';
  import {useNotifications} from '$lib/notify';
  import {bounceIn} from 'svelte/easing';
  import {scale} from 'svelte/transition';
  import {_refreshProjectRepoInfo, type Project} from './+page';
  import {Modal} from '$lib/components/modals';
  import {NewTabLinkMarkdown} from '$lib/components/Markdown';
  import {Duration} from '$lib/util/time';

  interface Props {
    project: Project;
    isEmpty: boolean;
    canManageProject: boolean;
  }

  const { project, isEmpty, canManageProject }: Props = $props();
  type SyncResult = { crdtChanges: number; fwdataChanges: number };
  type SyncJobResult = { status: string; error?: string; syncResult?: SyncResult };

  const { notifySuccess, notifyWarning } = useNotifications();

  let syncing = $state(false);
  let done = $state(false);
  let modalState = $derived(isEmpty ? 'empty' : done ? 'done' : syncing ? 'syncing' : 'idle');
  let error: string | undefined = $state(undefined);

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
        await _refreshProjectRepoInfo(project.code);
        if (syncResults.syncResult) {
          notifySuccess(
            $t('project.crdt.sync_result', {
              fwdataChanges: syncResults.syncResult.fwdataChanges,
              crdtChanges: syncResults.syncResult.crdtChanges,
            }),
            Duration.Persistent,
          );
          done = true;
        } else if (syncResults.error) {
          return syncResults.error;
        }
        return;
      }

      return await extractErrorMessage(response);
    } catch (error) {
      return getErrorMessage(error);
    } finally {
      syncing = false;
    }
  }

  async function extractErrorMessage(response: Response): Promise<string> {
    try {
      const problemDetails = await response.json();
      return problemDetails.detail || problemDetails.title || $t('project.crdt.sync_trigger_failed', {
        status: response.status,
        statusText: response.statusText,
      });
    } catch {
      return $t('project.crdt.sync_trigger_failed', {
        status: response.status,
        statusText: response.statusText,
      });
    }
  }


  async function awaitSyncFinished(): Promise<SyncJobResult | string> {
    while (true) {
      try {
        const response = await fetch(`/api/fw-lite/sync/await-sync-finished/${project.id}`, {
          signal: AbortSignal.timeout(30_000),
        });
        if (response.status === 500) {
          return $t('project.crdt.sync_failed');
        }
        if (response.status === 200) {
          const result = (await response.json()) as SyncJobResult;
          return result;
        }
      } catch (error) {
        if (error instanceof DOMException && (error.name === 'AbortError' || error.name === 'TimeoutError')) {
          continue;
        }
        return getErrorMessage(error);
      }
    }
  }

  async function onSubmit(): Promise<void> {
    if (!canManageProject) {
      // The button is disabled, but it's much easier to enable a button than send an http request
      // and the result of this is ugly
      error = $t('project.crdt.no_permission_to_manage');
      return;
    }
    error = await triggerSync();
  }

  async function syncProject(): Promise<void> {
    let error = await triggerSync();
    if (error) notifyWarning(error, Duration.Persistent);
  }

  async function useInFwLite(): Promise<void> {
    await modal?.openModal();
  }
  let modal: Modal | undefined = $state();
</script>

{#if project.hasHarmonyCommits}
  <Button
    variant="btn-primary"
    class="gap-1 indicator"
    onclick={syncProject}
    loading={syncing}
    active={syncing}
    customLoader
  >
    <span class="indicator-item badge badge-sm badge-accent translate-x-[calc(50%-16px)] shadow">Beta</span>
    {$t('project.crdt.sync_fwlite')}
    <span style="transform: rotateY(180deg)">
      <Icon icon="i-mdi-sync" spin={syncing} spinReverse />
    </span>
  </Button>
{:else}
  <Button variant="btn-primary" class="indicator" onclick={useInFwLite}>
    <span class="indicator-item badge badge-sm badge-accent translate-x-[calc(50%-16px)] shadow">Beta</span>
    <span>
      {$t('project.crdt.try_fw_lite')}
    </span>
  </Button>
{/if}

<Modal bind:this={modal} showCloseButton={false} hideActions={modalState === 'syncing'} closeOnClickOutside={false}>
  <h2 class="text-xl mb-6">
    {#if modalState === 'syncing'}
      {$t('project.crdt.making_available')}
    {:else if modalState === 'done'}
      {$t('project.crdt.now_available')}
    {:else}
      {$t('project.crdt.try_fw_lite')}
    {/if}
  </h2>
  {#if modalState === 'syncing'}
    <p class="text-center my-6">
      <span class="loading loading-lg"></span>
    </p>
    <div class="prose max-w-none underline-links">
      <NewTabLinkMarkdown md={$t('project.crdt.while_you_wait')} />
    </div>
  {:else if modalState === 'done'}
    <p class="text-center my-6">
      <span
        class="i-mdi-check-circle-outline text-7xl text-center text-success"
        transition:scale={{ duration: 600, start: 0.7, easing: bounceIn }}
      ></span>
    </p>
    <div class="prose max-w-none underline-links">
      <NewTabLinkMarkdown md={$t('project.crdt.to_start_using')} />
    </div>
  {:else if modalState === 'empty'}
    <div class="prose max-w-none">
      {$t('project.crdt.empty_project')}
    </div>
  {:else}
    <div class="prose max-w-none underline-links">
      <NewTabLinkMarkdown md={$t('project.crdt.try_info')} />
      {#if error}
        <div class="contents text-error">
          <NewTabLinkMarkdown
            md={`${$t('errors.apology')} ${$t('project.crdt.reach_out_for_help', { subject: encodeURIComponent($t('project.crdt.email_subject', { projectCode: project.code })) })}`}
          />
        </div>
        <FormError {error} right />
      {/if}
      {#if !canManageProject}
        <FormError error={$t('project.crdt.no_permission_to_manage')} right />
      {/if}
    </div>
  {/if}
  {#snippet actions({ close })}
    {#if modalState === 'idle'}
      <Button variant="btn-primary" onclick={onSubmit} disabled={!canManageProject}>
        {$t('project.crdt.submit')}
      </Button>
      <Button onclick={close}>
        {$t('project.crdt.cancel')}
      </Button>
    {:else if modalState === 'empty'}
      <Button onclick={close}>
        {$t('common.close')}
      </Button>
    {:else if modalState === 'done'}
      <Button variant="btn-primary" onclick={close}>
        {$t('project.crdt.finish')}
      </Button>
    {/if}
  {/snippet}
</Modal>
