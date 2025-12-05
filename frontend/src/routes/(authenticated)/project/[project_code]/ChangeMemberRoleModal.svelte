<script lang="ts">
  import {FormModal, type FormModalResult} from '$lib/components/modals';
  import {ProjectRoleSelect, tryParse} from '$lib/forms';
  import {ProjectRole} from '$lib/gql/types';
  import t from '$lib/i18n';
  import {z} from 'zod';
  import {_changeProjectMemberRole} from './+page';
  import type {UUID} from 'crypto';

  interface Props {
    projectId: string;
    showObserver?: boolean;
  }

  const { projectId, showObserver }: Props = $props();

  const schema = z.object({
    role: z.enum([ProjectRole.Editor, ProjectRole.Manager, ProjectRole.Observer]),
  });
  type Schema = typeof schema;
  let formModal: FormModal<Schema> | undefined = $state();
  let form = $derived(formModal?.form());

  let name: string = $state('');

  export async function open(member: {
    userId: UUID;
    name: string;
    role: ProjectRole;
  }): Promise<FormModalResult<Schema>> {
    name = member.name;
    return await formModal!.open(tryParse(schema, member), async () => {
      const result = await _changeProjectMemberRole({
        projectId: projectId,
        userId: member.userId,
        role: $form!.role as ProjectRole,
      });
      if (result.error?.byType('ProjectMembersMustBeVerified')) {
        return { role: [$t('project_page.add_user.user_must_be_verified')] };
      }
      if (result.error?.byType('ProjectMembersMustBeVerifiedForRole')) {
        return { role: [$t('project_page.add_user.manager_must_be_verified')] };
      }
      return result.error?.message;
    });
  }
</script>

<FormModal bind:this={formModal} {schema}>
  {#snippet title()}
    <span>{$t('project_page.change_role_modal.title', { name })}</span>
  {/snippet}
  {#snippet children({ errors })}
    <ProjectRoleSelect bind:value={$form!.role} error={errors.role} {showObserver} />
  {/snippet}
  {#snippet submitText()}
    <span>{$t('project_page.change_role')}</span>
  {/snippet}
</FormModal>
