<script lang="ts">
  import { FormModal, type FormModalResult } from '$lib/components/modals';
  import { OrgRoleSelect, tryParse } from '$lib/forms';
  import { OrgRole } from '$lib/gql/types';
  import t from '$lib/i18n';
  import { z } from 'zod';
  import { _changeOrgMemberRole } from './+page';

  export let orgId: string;

  const schema = z.object({
    role: z.enum([OrgRole.User, OrgRole.Admin])
  });
  type Schema = typeof schema;
  let formModal: FormModal<Schema>;
  $: form = formModal?.form();

  let name: string;

  export async function open(member: { userId: string; name: string; role: OrgRole }): Promise<FormModalResult<Schema>> {
    name = member.name;
    return await formModal.open(tryParse(schema, member), async () => {
      const result = await _changeOrgMemberRole(
        orgId,
        member.userId,
        $form.role,
      );
      if (result.error?.byType('OrgMembersMustBeVerified')) {
        return { role: [$t('org_page.add_user.user_must_be_verified')] };
      }
      if (result.error?.byType('OrgMembersMustBeVerifiedForRole')) {
        return { role: [$t('org_page.add_user.admin_must_be_verified')] };
      }
      return result.error?.message;
    });
  }
</script>

<FormModal bind:this={formModal} {schema} let:errors>
  <span slot="title">{$t('org_page.change_role_modal.title', { name })}</span>
  <OrgRoleSelect bind:value={$form.role} error={errors.role} />
  <span slot="submitText">{$t('org_page.change_role_modal.button_label')}</span>
</FormModal>
