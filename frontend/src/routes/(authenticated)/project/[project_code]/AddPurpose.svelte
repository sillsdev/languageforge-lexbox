<script lang="ts">
  import { z } from 'zod';
  import t from '$lib/i18n';
  import { useNotifications } from '$lib/notify';
  import { BadgeButton } from '$lib/components/Badges';
  import { Select } from '$lib/forms';
  import { AdminContent } from '$lib/layout';
  import { RetentionPolicy } from '$lib/gql/types';
  import { _setRetentionPolicy } from './+page';
  import { DialogResponse, FormModal } from '$lib/components/modals';

  export let projectId: string;

  const schema = z.object({
    retentionPolicy: z.nativeEnum(RetentionPolicy).default(RetentionPolicy.Training)
  });

  type Schema = typeof schema;
  let formModal: FormModal<Schema>;
  $: form = formModal?.form();

  const { notifySuccess } = useNotifications();

  async function openModal(): Promise<void> {
    const { response, formState } = await formModal.open(async () => {
      const { error } = await _setRetentionPolicy({
        projectId,
        retentionPolicy: $form.retentionPolicy,
      })
      if (error?.byType('NotFoundError')) {
        if (error.message === 'Project not found') return $t('project_page.add_org.project_not_found');
      }
    });

    if (response === DialogResponse.Submit && formState.retentionPolicy.currentValue) {
        notifySuccess($t('project_page.add_purpose.notify_success'));
    }
  }
</script>

<BadgeButton variant="badge-success" icon="i-mdi-plus" on:click={openModal}>
  {$t('project_page.add_purpose.add_button')}
</BadgeButton>

<FormModal bind:this={formModal} {schema} let:errors>
  <span slot="title">{$t('project_page.add_purpose.modal_title')}</span>
  <Select
    id="policy"
    label={$t('project.create.retention_policy')}
    bind:value={$form.retentionPolicy}
    error={errors.retentionPolicy}
  >
    <option value={RetentionPolicy.Verified}>{$t('retention_policy.language_project')}</option>
    <option value={RetentionPolicy.Training}>{$t('retention_policy.training')}</option>
    <option value={RetentionPolicy.Test}>{$t('retention_policy.test')}</option>
    <AdminContent>
      <option value={RetentionPolicy.Dev}>{$t('retention_policy.dev')}</option>
    </AdminContent>
  </Select>
  <span slot="submitText">{'Add Purpose'}</span>
</FormModal>
