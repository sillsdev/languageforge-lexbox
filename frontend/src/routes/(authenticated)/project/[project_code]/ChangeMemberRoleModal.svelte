<script lang="ts">
  import { FormModal, type FormModalResult } from '$lib/components/modals';
  import { tryParse } from '$lib/forms';
  import ProjectRoleSelect from '$lib/forms/ProjectRoleSelect.svelte';
  import { ProjectRole } from '$lib/gql/types';
  import t from '$lib/i18n';
  import { z } from 'zod';
  import { _changeProjectMemberRole } from './+page';

  export let projectId: string;

  const schema = z.object({
    role: z.enum([ProjectRole.Editor, ProjectRole.Manager]),
  });
  type Schema = typeof schema;
  let formModal: FormModal<Schema>;
  $: form = formModal?.form();

  let name: string;

  export async function open(member: { userId: string; name: string; role: ProjectRole }): Promise<FormModalResult<Schema>> {
    name = member.name;
    return await formModal.open(tryParse(schema, member), async () => {
      const result = await _changeProjectMemberRole({
        projectId,
        userId: member.userId,
        role: $form.role,
      });
      return result.error?.message;
    });
  }
</script>

<FormModal bind:this={formModal} {schema} let:errors>
  <span slot="title">{$t('project_page.change_role_modal.title', { name })}</span>
  <ProjectRoleSelect bind:value={$form.role} error={errors.role} />
  <span slot="submitText">{$t('project_page.change_role')}</span>
</FormModal>
