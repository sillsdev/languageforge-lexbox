<script lang="ts">
  import Input from '$lib/forms/Input.svelte';
  import Checkbox from '$lib/forms/Checkbox.svelte';
  import { tScoped } from '$lib/i18n';
  import { z } from 'zod';
  import { CircleArrowIcon } from '$lib/icons';
  import Modal from '$lib/components/modals/Modal.svelte';
  import { FormError, lexSuperForm, type ErrorMessage } from '$lib/forms';
  import Form from '$lib/forms/Form.svelte';
  import TusUpload from '$lib/components/TusUpload.svelte';
  import { ResetStatus } from '$lib/gql/generated/graphql';
  import { _refreshProjectMigrationStatusAndRepoInfo } from './+page';
  import { scale } from 'svelte/transition';
  import { bounceIn } from 'svelte/easing';

  enum ResetSteps {
    Download,
    Reset,
    Upload,
    Finished,
  }

  let currentStep = ResetSteps.Download;

  function nextStep(): void {
    currentStep++;
    error = undefined;
  }

  function previousStep(): void {
    currentStep--;
    error = undefined;
  }

  let code: string;
  let modal: Modal;
  let error: ErrorMessage | undefined = undefined;

  export async function open(_code: string, resetStatus: ResetStatus): Promise<boolean> {
    code = _code;
    if (resetStatus == ResetStatus.InProgress) {
      currentStep = ResetSteps.Upload;
    }
    await modal.openModal(true, true);
    return currentStep == ResetSteps.Finished;
  }

  const t = tScoped('project_page.reset_project_modal');

  let verify = z.object({
    confirmProjectCode: z.string().refine(
      (value) => value === code,
      () => ({ message: $t('confirm_project_code_error') })
    ),
    confirmDownloaded: z.boolean().refine(
      (value) => value,
      () => ({ message: $t('confirm_downloaded_error') })
    ),
  });

  let { form, errors, enhance, reset } = lexSuperForm(verify, async () => {
    const url = `/api/project/resetProject/${code}`;
    const resetResponse = await fetch(url, { method: 'post' });
    //we should do the reset via a mutation, but this is easier for now
    //we need to refresh the status so if the admin closes the dialog they can resume back where they left off.
    await _refreshProjectMigrationStatusAndRepoInfo(code);
    if (resetResponse.ok) {
      nextStep();
    } else {
      error = resetResponse.statusText;
    }
  });

  async function uploadComplete(): Promise<void> {
    await _refreshProjectMigrationStatusAndRepoInfo(code);
    nextStep();
  }

  function onClose(): void {
    currentStep = ResetSteps.Download;
    reset();
  }

  async function leaveProjectEmpty(): Promise<void> {
    const url = `/api/project/finishResetProject/${code}`;
    const resetResponse = await fetch(url, { method: 'post' });
    //we should do the reset via a mutation, but this is easier for now
    //we need to refresh the status, because the project is no longer being reset
    await _refreshProjectMigrationStatusAndRepoInfo(code);
    if (resetResponse.ok) {
      nextStep();
    } else {
      error = resetResponse.statusText;
    }
  }
</script>

<div class="reset-modal contents">
  <Modal bind:this={modal} on:close={onClose} showCloseButton={false}>
    <h2 class="text-xl mb-4">{$t('title', { code })}</h2>
    <ul class="steps w-full mb-2">
      <li class="step step-primary">{$t('backup_step')}</li>
      <li class="step" class:step-primary={currentStep >= ResetSteps.Reset}>{$t('reset_step')}</li>
      <li class="step" class:step-primary={currentStep >= ResetSteps.Upload}>{$t('restore_step')}</li>
      <li class="step" class:step-primary={currentStep >= ResetSteps.Finished}>{$t('finished_step')}</li>
    </ul>

    <div class="divider my-2" />

    {#if currentStep === ResetSteps.Download}
      <p class="mb-2 label">
        {$t('download_instruction')}
      </p>
      <a rel="external" href="/api/project/backupProject/{code}" class="btn btn-success" download>
        {$t('download_button')}
        <span class="i-mdi-download text-2xl" />
      </a>
    {:else if currentStep === ResetSteps.Reset}
      <Form id="reset-form" {enhance}>
        <Checkbox
          id="confirmDownloaded"
          label={$t('confirm_downloaded')}
          bind:value={$form.confirmDownloaded}
          error={$errors.confirmDownloaded}
        />
        <Input
          id="confirmProjectCode"
          type="text"
          label={$t('confirm_project_code')}
          bind:value={$form.confirmProjectCode}
          error={$errors.confirmProjectCode}
        />
      </Form>
    {:else if currentStep === ResetSteps.Upload}
      <div class="label">
        {$t('upload_instruction')}
      </div>
      <TusUpload
        endpoint={'/api/project/upload-zip/' + code}
        uploadLabel={$t('upload_project')}
        inputLabel={$t('select_zip')}
        inputDescription={$t('should_only_contain_hg')}
        accept="application/zip"
        on:uploadComplete={uploadComplete}
      />
    {:else if currentStep === ResetSteps.Finished}
      <div class="text-center">
        <p class="mb-2 label justify-center">
          {$t('reset_success')}
        </p>
        <span
          class="i-mdi-check-circle-outline text-7xl text-success"
          transition:scale={{ duration: 600, start: 0.7, easing: bounceIn }}
        />
      </div>
    {:else}
      <span>Unknown step</span>
    {/if}
    <FormError {error} />
    <svelte:fragment slot="extraActions">
      {#if currentStep === ResetSteps.Reset}
        <button class="btn btn-secondary" on:click={previousStep}>
          <span class="i-mdi-chevron-left text-2xl" />
          {$t('back')}
        </button>
      {/if}
    </svelte:fragment>
    <svelte:fragment slot="actions">
      {#if currentStep === ResetSteps.Download}
        <button class="btn btn-primary" on:click={nextStep}>
          {$t('i_have_working_backup')}
          <span class="i-mdi-chevron-right text-2xl" />
        </button>
      {:else if currentStep === ResetSteps.Reset}
        <button class="btn btn-primary" type="submit" form="reset-form">
          {$t('submit')}
          <CircleArrowIcon />
        </button>
      {:else if currentStep === ResetSteps.Upload}
        <button class="btn btn-primary" on:click={leaveProjectEmpty}>
          {$t('leave_project_empty')}
          <span class="i-mdi-chevron-right text-2xl" />
        </button>
      {:else if currentStep === ResetSteps.Finished}
        <button class="btn btn-primary" on:click={() => modal.submitModal()}>
          {$t('close')}
        </button>
      {/if}
    </svelte:fragment>
  </Modal>
</div>
