<script lang="ts">
  import {NewTabLinkRenderer} from '$lib/components/Markdown';
  import {tryGetErrorMessage} from '$lib/error/utils';
  import {Button, FormError} from '$lib/forms';
  import t from '$lib/i18n';
  import {Icon} from '$lib/icons';
  import {useNotifications} from '$lib/notify';
  import Markdown from 'svelte-exmarkdown';
  import {bounceIn} from 'svelte/easing';
  import {scale} from 'svelte/transition';
  import type {Project} from './+page';
  import {Modal} from '$lib/components/modals';

  export let project: Project;
  export let hasHarmonyCommits: boolean;

  const { notifySuccess, notifyWarning } = useNotifications();

  let syncing = false;
  let done = false;
  $: state = done ? 'done' : syncing ? 'syncing' : 'idle';
  let error: string | undefined = undefined;

  async function triggerSync(): Promise<string | undefined> {
    syncing = true;
    try {
      const response = await fetch(`/api/crdt/sync/${project.id}`, {
        method: 'POST',
      });

      if (response.ok) {
        const {crdtChanges, fwdataChanges} = await response.json();
        notifySuccess(`Synced successfully (${fwdataChanges} FwData changes. ${crdtChanges} CRDT changes)`);
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

  async function onSubmit(): Promise<void> {
    error = await triggerSync();
  }

  async function useInFwLite(): Promise<void> {
    await modal.openModal();
  }
  let modal: Modal;
</script>

{#if hasHarmonyCommits}
  <Button variant="btn-primary" class="gap-1" on:click={triggerSync} loading={state === 'syncing'} customLoader>
    Sync FieldWorks Lite
    <Icon icon="i-mdi-sync" spin={state === 'syncing'} spinReverse />
  </Button>
{:else}
  <Button variant="btn-primary" class="indicator" on:click={useInFwLite}>
    <span class="indicator-item badge badge-sm badge-accent translate-x-[calc(50%-16px)]">Beta</span>
    <span>
      {$t('project.crdt.try_fw_lite')}
    </span>
  </Button>
  <Modal bind:this={modal} showCloseButton={false} hideActions={state === 'syncing'} closeOnClickOutside={false}>
    <h2 class="text-xl mb-2">
      {$t('project.crdt.try_fw_lite')}
    </h2>
    {#if state === 'syncing'}
      <div class="text-center">
        <p class="mb-2 label justify-center">
          {$t('project.crdt.making_available')}
        </p>
        <span class="loading loading-lg" />
      </div>
    {:else if state === 'done'}
      <div class="prose max-w-none underline-links">
        <Markdown md={$t('project.crdt.now_available')} plugins={[{ renderer: { a: NewTabLinkRenderer } }]} />
        <p class="text-center">
          <span
            class="i-mdi-check-circle-outline text-7xl text-center text-success"
            transition:scale={{ duration: 600, start: 0.7, easing: bounceIn }}
          />
        </p>
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
