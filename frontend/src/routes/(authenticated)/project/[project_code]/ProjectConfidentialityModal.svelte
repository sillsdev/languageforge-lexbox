<script lang="ts">
  import {DialogResponse, FormModal} from '$lib/components/modals';
  import {ProjectConfidentialityCombobox} from '$lib/components/Projects';
  import {useNotifications} from '$lib/notify';
  import {z} from 'zod';
  import {_setProjectConfidentiality} from './+page';
  import t from '$lib/i18n';

  interface Props {
    projectId: string;
    isConfidential: boolean | undefined;
  }

  const { projectId, isConfidential }: Props = $props();

  const schema = z.object({
    isConfidential: z.boolean(),
  });
  let formModal: FormModal<typeof schema> | undefined = $state();
  let form = $derived(formModal?.form());

  const { notifySuccess, notifyWarning } = useNotifications();

  export async function openModal(): Promise<void> {
    if (!formModal || !$form) return;
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
  {#snippet title()}
    <span>{$t('project.confidential.modal.title')}</span>
  {/snippet}
  <ProjectConfidentialityCombobox bind:value={$form!.isConfidential} />
  {#snippet submitText()}
    <span>
      {#if $form!.isConfidential}
        {$t('project.confidential.modal.submit_button_confidential')}
      {:else}
        {$t('project.confidential.modal.submit_button_not_confidential')}
      {/if}
    </span>
  {/snippet}
</FormModal>
