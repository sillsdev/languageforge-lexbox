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
    usernameOrEmail: z.string(),
    role: z.enum([ProjectRole.Editor, ProjectRole.Manager]).default(ProjectRole.Editor),
  });
  let formModal: FormModal<typeof schema>;
  $: form = formModal?.form();

  const { notifySuccess } = useNotifications();

  async function openModal(): Promise<void> {
    let userInvited = false;
    const { response, formState } = await formModal.open(async () => {
      const { error } = await _addProjectMember({
        projectId,
        usernameOrEmail: $form.usernameOrEmail,
        role: $form.role,
      });

      if (error?.byType('NotFoundError')) {
        if (error.message === 'Project not found') {
          return $t('project_page.add_user.project_not_found');
        } else {
          return { usernameOrEmail: [$t('project_page.add_user.username_not_found')] };
        }
      }
      if (error?.byType('ProjectMembersMustBeVerified')) {
        return { usernameOrEmail: [$t('project_page.add_user.user_must_be_verified')] };
      }
      if (error?.byType('AlreadyExistsError')) {
        return { usernameOrEmail: [$t('project_page.add_user.user_already_member')] };
      }
      if (error?.byType('ProjectMemberInvitedByEmail')) {
        userInvited = true;
        return undefined; // Close modal as if success
      }

      return error?.message;
    });
    if (response === DialogResponse.Submit) {
      const message = userInvited ? 'member_invited' : 'add_member';
      notifySuccess($t(`project_page.notifications.${message}`, { email: formState.usernameOrEmail.currentValue }));
    }
  }
</script>

<BadgeButton type="badge-success" icon="i-mdi-account-plus-outline" on:click={openModal}>
  {$t('project_page.add_user.add_button')}
</BadgeButton>

<FormModal bind:this={formModal} {schema} let:errors>
  <span slot="title">{$t('project_page.add_user.modal_title')}</span>
  <Input
    id="usernameOrEmail"
    type="text"
    label={$t('login.label_email')}
    bind:value={$form.usernameOrEmail}
    error={errors.usernameOrEmail}
    autofocus
  />
  <ProjectRoleSelect bind:value={$form.role} error={errors.role} />
  <span slot="submitText">
    {#if $form.usernameOrEmail.includes('@')}
      {$t('project_page.add_user.submit_button_email')}
    {:else}
      {$t('project_page.add_user.submit_button')}
    {/if}
  </span>
</FormModal>
