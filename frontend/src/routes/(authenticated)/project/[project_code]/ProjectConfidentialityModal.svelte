<script lang="ts">
  import { DialogResponse, FormModal } from '$lib/components/modals';
  import { ProjectConfidentialityCombobox } from '$lib/components/Projects';
  import { useNotifications } from '$lib/notify';
  import { z } from 'zod';
  import { _setProjectConfidentiality } from './+page';
  import t from '$lib/i18n';

  export let projectId: string;
  export let isConfidential: boolean | undefined;

  const schema = z.object({
    isConfidential: z.boolean(),
  });
  let formModal: FormModal<typeof schema>;
  $: form = formModal?.form();

  const { notifySuccess, notifyWarning } = useNotifications();

  export async function openModal(): Promise<void> {
    const originalValue = isConfidential;
    const { response, formState } = await formModal.open({ isConfidential: isConfidential ?? false }, async () => {
      const { error } = await _setProjectConfidentiality({
        projectId,
        isConfidential: $form.isConfidential,
      });

      return error?.message;
    });

    if (response === DialogResponse.Submit && formState.isConfidential.currentValue !== originalValue) {
      if (formState.isConfidential.currentValue) {
        notifyWarning($t('project.confidential.modal.project_now_confidential'));
      } else {
        notifySuccess($t('project.confidential.modal.project_now_not_confidential'));
      }
    }
  }
</script>

<FormModal bind:this={formModal} {schema} submitVariant={$form?.isConfidential ? 'btn-warning' : 'btn-primary'}>
  <span slot="title">{$t('project.confidential.modal.title')}</span>
  <ProjectConfidentialityCombobox bind:value={$form.isConfidential} />
  <span slot="submitText">
    {#if $form.isConfidential}
      {$t('project.confidential.modal.submit_button_confidential')}
    {:else}
      {$t('project.confidential.modal.submit_button_not_confidential')}
    {/if}
  </span>
</FormModal>
