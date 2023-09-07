<script context="module" lang="ts">
  export type ResetProjectModalI18nShape = {
    title: string,
    submit: string,
    // eslint-disable-next-line @typescript-eslint/naming-convention
    download_button: string,
    // eslint-disable-next-line @typescript-eslint/naming-convention
    confirm_downloaded: string,
    // eslint-disable-next-line @typescript-eslint/naming-convention
    confirm_downloaded_error: string,
    // eslint-disable-next-line @typescript-eslint/naming-convention
    confirm_project_code: string,
    // eslint-disable-next-line @typescript-eslint/naming-convention
    confirm_project_code_error: string,
    // eslint-disable-next-line @typescript-eslint/naming-convention
    admin_required: string,
    // eslint-disable-next-line @typescript-eslint/naming-convention
    reset_project: string,
    // eslint-disable-next-line @typescript-eslint/naming-convention
    reset_project_notification: string,
  };
</script>

<script lang="ts">
  import Input from '$lib/forms/Input.svelte';
  import Checkbox from '$lib/forms/Checkbox.svelte';
  import { tScoped, type I18nShapeKey } from '$lib/i18n';
  import { z } from 'zod';
  import { FormModal } from '$lib/components/modals';
  import type { FormModalResult } from '$lib/components/modals/FormModal.svelte';
  import { CircleArrowIcon } from '$lib/icons';
  import { notifySuccess } from '$lib/notify';
  import FormFieldError from '$lib/forms/FormFieldError.svelte';

  export let i18nScope: I18nShapeKey<ResetProjectModalI18nShape>;
  // let name: string;
  // export let code: string;
  let code: string;

  export async function open(_code: string): Promise<FormModalResult<Schema>> {
    code = _code;
    return await resetProjectModal.open(async () => {
      const url = `/api/project/resetProject/${code}`;
      const resetResponse = await fetch(url, {method: 'post'});
      if (resetResponse.ok) {
        notifySuccess(
          $t('reset_project_notification', { code })
      )}
    });
  }

  $: t = tScoped<ResetProjectModalI18nShape>(i18nScope);

  $: verify = z.object({
    confirmProjectCode: z.string().refine((value) => value === code, {message: $t('confirm_project_code_error')}),
    confirmDownloaded: z.boolean().refine((value) => value, {message: $t('confirm_downloaded_error')}),
  });

  type Schema = typeof verify;

  let resetProjectModal: FormModal<Schema>;
  $: modalForm = resetProjectModal?.form();
</script>

<div class="reset-modal contents">
  <FormModal bind:this={resetProjectModal} schema={verify} let:errors>
    <span slot="title">{$t('title')}</span>
    <a rel="external" target="_blank" href="/api/project/backupProject/{code}" download>
      <span class="btn">{$t('download_button')}</span>
    </a>
    <Checkbox label={$t('confirm_downloaded')} bind:value={$modalForm.confirmDownloaded} />
    <FormFieldError id="confirm-checkbox-error-label" error={errors.confirmDownloaded} />
    <Input
      id="confirmProjectCode"
      type="text"
      label={$t('confirm_project_code')}
      error={errors.confirmProjectCode}
      bind:value={$modalForm.confirmProjectCode}
    />
    <svelte:fragment slot="submitText">
      {$t('submit')}
      <CircleArrowIcon />
    </svelte:fragment>
  </FormModal>
</div>
