<script lang="ts">
    import Input from '$lib/forms/Input.svelte';
    import Checkbox from '$lib/forms/Checkbox.svelte';
    import {tScoped} from '$lib/i18n';
    import {z} from 'zod';
    import {CircleArrowIcon} from '$lib/icons';
    import Modal from '$lib/components/modals/Modal.svelte';
    import {lexSuperForm} from '$lib/forms';
    import Form from '$lib/forms/Form.svelte';
    import TusUpload from '$lib/components/TusUpload.svelte';
    import {ResetStatus} from '$lib/gql/generated/graphql';
    import {_refreshProjectStatus} from './+page';

    enum ResetSteps {
        Download,
        Reset,
        Upload,
        Finished
    }

    let currentStep = ResetSteps.Download;

    function nextStep(): void {
        currentStep++;
    }

    function previousStep(): void {
        currentStep--;
    }

    let code: string;
    let modal: Modal;

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
        confirmProjectCode: z.string().refine((value) => value === code, () => ({message: $t('confirm_project_code_error')})),
        confirmDownloaded: z.boolean().refine((value) => value, () => ({message: $t('confirm_downloaded_error')})),
    });

    let {form, errors, enhance, reset} = lexSuperForm(verify, async () => {
        const url = `/api/project/resetProject/${code}`;
        const resetResponse = await fetch(url, {method: 'post'});
        //we should do the reset via a mutation, but this is easier for now
        //we need to refresh the status so if the admin closes the dialog they can resume back where they left off.
        await _refreshProjectStatus(code);
        if (resetResponse.ok) {
            nextStep();
        }
    });

    async function uploadComplete(): Promise<void> {
        await _refreshProjectStatus(code);
        nextStep();
    }

    function onClose(): void {
        currentStep = ResetSteps.Download;
        reset();
    }

</script>

<div class="reset-modal contents" class:hide-modal-actions={currentStep === ResetSteps.Upload}>
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
            <p class="mb-2 label">First, download a backup of the project that you can use to restore it in step 3:</p>
            <a rel="external" href="/api/project/backupProject/{code}"
                class="btn btn-success" download>
                {$t('download_button')}
                <span class="i-mdi-download text-2xl"/>
            </a>
        {:else if currentStep === ResetSteps.Reset}
            <Form id="reset-form" {enhance}>
                <Checkbox
                        id="confirmDownloaded"
                        label={$t('confirm_downloaded')}
                        bind:value={$form.confirmDownloaded}
                        error={$errors.confirmDownloaded}/>
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
              The project repository was successfully reset. Now upload a zip file to restore it:
            </div>
            <TusUpload endpoint={'/api/project/upload-zip/' + code}
                       uploadLabel={$t('upload_project')}
                       inputLabel={$t('select_zip')}
                       inputDescription={$t('should_only_contain_hg')}
                       accept="application/zip"
                       on:uploadComplete={uploadComplete}/>
        {:else if currentStep === ResetSteps.Finished}
            <div class="text-center">
                <span class="i-mdi-check text-6xl"/>
            </div>
        {:else}
            <span>Unknown step</span>
        {/if}
        <svelte:fragment slot="extraActions">
          {#if currentStep === ResetSteps.Reset}
            <button class="btn btn-secondary" on:click={previousStep}>
              <span class="i-mdi-chevron-left text-2xl"/>
                {$t('back')}
            </button>
          {/if}
        </svelte:fragment>
        <svelte:fragment slot="actions">
            {#if currentStep === ResetSteps.Download}
                <button class="btn btn-primary" on:click={nextStep}>
                    {$t('i_have_working_backup')}
                    <span class="i-mdi-chevron-right text-2xl"/>
                </button>
            {:else if currentStep === ResetSteps.Reset}
                <button class="btn btn-primary" type="submit" form="reset-form">
                    {$t('submit')}
                    <CircleArrowIcon/>
                </button>
            {:else if currentStep === ResetSteps.Finished}
                <button class="btn btn-primary" on:click={() => modal.submitModal()}>
                  {$t('close')}
                </button>
            {/if}
        </svelte:fragment>
    </Modal>
</div>

<style>
  :global(.hide-modal-actions .modal-action) {
      display: none !important;
  }
</style>
