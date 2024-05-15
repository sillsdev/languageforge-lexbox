<script lang="ts">
  import { FormModal, type FormModalResult } from '$lib/components/modals';
  import { tryParse } from '$lib/forms';
  import { OrgRole, ProjectRole } from '$lib/gql/types';
  import t from '$lib/i18n';
  import { z } from 'zod';
  import { _changeProjectMemberRole } from './+page';
  import { _changeOrgMemberRole } from '../../org/[org_id]/+page';
  import type { UUID } from 'crypto';
  import MemberRoleSelect from '$lib/forms/MemberRoleSelect.svelte';

  export let roleType: 'project' | 'org' = 'project';
  export let projectId: string; // TODO: Rename to projectOrOrgId

  $: schema = z.object({
    role: roleType === 'project'
      ? z.enum([ProjectRole.Editor, ProjectRole.Manager])
      : z.enum([OrgRole.User, OrgRole.Admin]),
  });
  type Schema = typeof schema;
  let formModal: FormModal<Schema>;
  $: form = formModal?.form();

  let name: string;

  export async function open(member: { userId: UUID; name: string; role: ProjectRole | OrgRole }): Promise<FormModalResult<Schema>> {
    name = member.name;
    return await formModal.open(tryParse(schema, member), async () => {
      const result =
        roleType === 'project'
        ? await _changeProjectMemberRole({
            projectId,
            userId: member.userId,
            role: $form.role as ProjectRole,
          })
        : await _changeOrgMemberRole(
            projectId as UUID,
            member.userId,
            $form.role as OrgRole
          );
      // @ts-expect-error Errors could be from either the Project or Org GQL mutations
      if (result.error?.byType('ProjectMembersMustBeVerified')) {
        return { role: [$t('project_page.add_user.user_must_be_verified')] };
      }
      // @ts-expect-error Errors could be from either the Project or Org GQL mutations
      if (result.error?.byType('ProjectMembersMustBeVerifiedForRole')) {
        return { role: [$t('project_page.add_user.manager_must_be_verified')] };
      }
      return result.error?.message;
    });
  }
</script>

<FormModal bind:this={formModal} {schema} let:errors>
  <span slot="title">{$t('project_page.change_role_modal.title', { name })}</span>
  <MemberRoleSelect type={roleType} bind:value={$form.role} error={errors.role} />
  <span slot="submitText">{$t('project_page.change_role')}</span>
</FormModal>
