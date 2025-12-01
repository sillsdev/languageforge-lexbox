<script lang="ts">
  import {z} from 'zod';
  import t from '$lib/i18n';
  import {Select} from '$lib/forms';
  import {_addProjectToOrg, _getOrgs} from './+page';
  import type {Organization} from '$lib/gql/types';
  import {FormModal} from '$lib/components/modals';
  import {BadgeButton} from '$lib/components/Badges';

  type Org = Pick<Organization, 'id' | 'name'>;
  interface Props {
    projectId: string;
    userIsAdmin: boolean;
  }

  const { projectId, userIsAdmin }: Props = $props();
  let orgList: Org[] = $state([]);

  const schema = z.object({
    orgId: z.string().trim(),
  });

  type Schema = typeof schema;
  let formModal: FormModal<Schema> | undefined = $state();
  let form = $derived(formModal?.form());

  async function openModal(): Promise<void> {
    if (!formModal || !$form) return;
    orgList = await _getOrgs(userIsAdmin);
    const selected = orgList.length > 0 && orgList.length < 6 ? { orgId: orgList[0].id } : {};

    await formModal.open(selected, async () => {
      const { error } = await _addProjectToOrg({
        projectId,
        orgId: $form.orgId,
      });
      if (error?.byType('NotFoundError')) {
        if (error.message === 'Organization not found') return $t('project_page.add_org.org_not_found');
        if (error.message === 'Project not found') return $t('project_page.add_org.project_not_found');
      }
    });
  }
</script>

<BadgeButton variant="badge-success" icon="i-mdi-account-plus-outline" onclick={openModal}>
  {$t('project_page.add_org.add_button')}
</BadgeButton>

<FormModal bind:this={formModal} {schema}>
  {#snippet title()}
    <span>{$t('project_page.add_org.modal_title')}</span>
  {/snippet}
  {#snippet children({ errors })}
    <Select id="org" label={$t('project_page.organization.title')} bind:value={$form!.orgId} error={errors.orgId}>
      {#each orgList as org (org.id)}
        <option value={org.id}>{org.name}</option>
      {/each}
    </Select>
  {/snippet}
  {#snippet submitText()}
    <span>{$t('project_page.add_org.submit_button')}</span>
  {/snippet}
</FormModal>
