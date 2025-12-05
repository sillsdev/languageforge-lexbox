<script lang="ts">
  import {FormModal, type FormModalResult} from '$lib/components/modals';
  import {OrgRoleSelect, tryParse} from '$lib/forms';
  import {OrgRole} from '$lib/gql/types';
  import t from '$lib/i18n';
  import {z} from 'zod';
  import {_changeOrgMemberRole} from './+page';

  interface Props {
    orgId: string;
  }

  const { orgId }: Props = $props();

  const schema = z.object({
    role: z.enum([OrgRole.User, OrgRole.Admin]),
  });
  type Schema = typeof schema;
  let formModal: FormModal<Schema> | undefined = $state();
  let form = $derived(formModal?.form());

  let name: string = $state('');

  export async function open(member: {
    userId: string;
    name: string;
    role: OrgRole;
  }): Promise<FormModalResult<Schema>> {
    name = member.name;
    return await formModal!.open(tryParse(schema, member), async () => {
      const result = await _changeOrgMemberRole(orgId, member.userId, $form!.role);
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

<FormModal bind:this={formModal} {schema}>
  {#snippet title()}
    <span>{$t('org_page.change_role_modal.title', { name })}</span>
  {/snippet}
  {#snippet children({ errors })}
    <OrgRoleSelect bind:value={$form!.role} error={errors.role} />
  {/snippet}
  {#snippet submitText()}
    <span>{$t('org_page.change_role_modal.button_label')}</span>
  {/snippet}
</FormModal>
