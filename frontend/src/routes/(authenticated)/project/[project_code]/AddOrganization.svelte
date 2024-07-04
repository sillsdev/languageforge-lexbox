<script lang="ts">
  import { BadgeButton } from '$lib/components/Badges';
  import { FormModal } from '$lib/components/modals';
  import { z } from 'zod';
  import { _addProjectToOrg } from './+page';
  import { Select } from '$lib/forms';

  export let orgList;
  export let projectId: string;

  const schema = z.object({
    orgId: z.string().trim()
  });

  type Schema = typeof schema;
  let formModal: FormModal<Schema>;
  $: form = formModal?.form();

  async function openModal(): Promise<void> {
    await formModal.open(async () => {
      await _addProjectToOrg({
        projectId,
        orgId: $form.orgId,
      })
    });

    // Error Handling
  }

</script>

<BadgeButton variant="badge-success" icon="i-mdi-account-plus-outline" on:click={openModal}>
  {'Add Organization'}
</BadgeButton>

<FormModal bind:this={formModal} {schema} let:errors>
  <span slot="title">{'Choose Organization'}</span>
  <Select
    id="org"
    label={'organization'}
    bind:value={$form.orgId}
    error={errors.orgId}
    on:change
  >
    {#each orgList as org}
      <option value={org.id}>{org.name}</option>
    {/each}
  </Select>
</FormModal>
