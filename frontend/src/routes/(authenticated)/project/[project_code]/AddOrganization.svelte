<script lang="ts">
  import { z } from 'zod';
  import t from '$lib/i18n';
  import { Select } from '$lib/forms';
  import { _addProjectToOrg, _getOrgs } from './+page';
  import type { Organization } from '$lib/gql/types';
  import { FormModal } from '$lib/components/modals';
  import { BadgeButton } from '$lib/components/Badges';

  type Org = Pick<Organization, 'id' | 'name'>;
  export let projectId: string;
  export let userIsAdmin: boolean;
  let orgList: Org[] = [];

  const schema = z.object({
    orgId: z.string().trim()
  });

  type Schema = typeof schema;
  let formModal: FormModal<Schema>;
  $: form = formModal?.form();

  async function openModal(): Promise<void> {
    orgList = await _getOrgs(userIsAdmin);

    await formModal.open(async () => {
      const { error } = await _addProjectToOrg({
        projectId,
        orgId: $form.orgId,
      })
      if (error?.byType('NotFoundError')) {
        if (error.message === 'Organization not found') return $t('project_page.add_org.org_not_found');
        if (error.message === 'Project not found') return $t('project_page.add_org.project_not_found');
      }
    });
  }

</script>

<BadgeButton variant="badge-success" icon="i-mdi-account-plus-outline" on:click={openModal}>
  {$t('project_page.add_org.add_button')}
</BadgeButton>

<FormModal bind:this={formModal} {schema} let:errors>
  <span slot="title">{$t('project_page.add_org.modal_title')}</span>
  <Select
    id="org"
    label={$t('project_page.organization.title')}
    bind:value={$form.orgId}
    error={errors.orgId}
  >
    {#each orgList as org}
      <option value={org.id}>{org.name}</option>
    {/each}
  </Select>
  <span slot="submitText">{$t('project_page.add_org.submit_button')}</span>
</FormModal>
