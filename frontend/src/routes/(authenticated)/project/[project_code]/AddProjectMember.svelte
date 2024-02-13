<script lang="ts">
  import { BadgeButton } from '$lib/components/Badges';
  import { DialogResponse, FormModal } from '$lib/components/modals';
  import { Input, ProjectRoleSelect } from '$lib/forms';
  import { ProjectRole } from '$lib/gql/types';
  import t from '$lib/i18n';
  import { z } from 'zod';
  import { _addProjectMember } from './+page';
  import { useNotifications } from '$lib/notify';

  export let projectId: string;
  const schema = z.object({
    email: z.string().email($t('project_page.add_user.email_required')),
    role: z.enum([ProjectRole.Editor, ProjectRole.Manager]).default(ProjectRole.Editor),
  });
  let formModal: FormModal<typeof schema>;
  $: form = formModal?.form();

  const { notifySuccess } = useNotifications();

  async function openModal(): Promise<void> {
    const { response, formState } = await formModal.open(async () => {
      const { error } = await _addProjectMember({
        projectId,
        userEmail: $form.email,
        role: $form.role,
      });

      if (error?.byType('NotFoundError')) {
        return { email: [$t('project_page.add_user.user_not_found')] };
      }
      if (error?.byType('ProjectMembersMustBeVerified')) {
        return { email: [$t('project_page.add_user.user_not_verified')] };
      }

      return error?.message;
    });
    if (response === DialogResponse.Submit) {
      notifySuccess($t('project_page.notifications.add_member', { email: formState.email.currentValue }));
    }
  }
</script>

<BadgeButton type="badge-success" icon="i-mdi-account-plus-outline" on:click={openModal}>
  {$t('project_page.add_user.add_button')}
</BadgeButton>

<FormModal bind:this={formModal} {schema} let:errors>
  <span slot="title">{$t('project_page.add_user.modal_title')}</span>
  <Input
    id="email"
    type="email"
    label={$t('admin_dashboard.column_email')}
    bind:value={$form.email}
    error={errors.email}
    autofocus
  />
  <ProjectRoleSelect bind:value={$form.role} error={errors.role} />
  <span slot="submitText">{$t('project_page.add_user.submit_button')}</span>
</FormModal>
