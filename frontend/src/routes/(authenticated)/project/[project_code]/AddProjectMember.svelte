<script lang="ts">
  import { BadgeButton } from '$lib/components/Badges';
  import { DialogResponse, FormModal } from '$lib/components/modals';
  import { Input, ProjectRoleSelect } from '$lib/forms';
  import { ProjectRole } from '$lib/gql/types';
  import t from '$lib/i18n';
  import { z } from 'zod';
  import { _addProjectMember } from './+page';
  import { useNotifications } from '$lib/notify';
  import { isAdmin } from '$lib/user';
  import { page } from '$app/stores'
  import UserTypeahead from '$lib/forms/UserTypeahead.svelte';
  import type { SingleUserTypeaheadResult } from '$lib/gql/typeahead-queries';

  export let projectId: string;
  const schema = z.object({
    email: z.string().email($t('form.invalid_email')).optional(),
    role: z.enum([ProjectRole.Editor, ProjectRole.Manager]).default(ProjectRole.Editor),
  });
  let formModal: FormModal<typeof schema>;
  $: form = formModal?.form();

  export let selectedUser: SingleUserTypeaheadResult;

  const { notifySuccess } = useNotifications();

  async function openModal(): Promise<void> {
    let userInvited = false;
    let selectedEmail: string = '';
    const { response, formState } = await formModal.open(async () => {
      selectedEmail = $form.email ? $form.email : selectedUser.email ?? selectedUser.username ?? '';
      const { error } = await _addProjectMember({
        projectId,
        userEmail: selectedEmail,
        role: $form.role,
      });

      if (error?.byType('NotFoundError')) {
        return { email: [$t('project_page.add_user.project_not_found')] };
      }
      if (error?.byType('ProjectMembersMustBeVerified')) {
        return { email: [$t('project_page.add_user.user_must_be_verified')] };
      }
      if (error?.byType('ProjectMemberInvitedByEmail')) {
        userInvited = true;
        return undefined; // Close modal as if success
      }

      return error?.message;
    });
    if (response === DialogResponse.Submit) {
      const message = userInvited ? 'member_invited' : 'add_member';
      notifySuccess($t(`project_page.notifications.${message}`, { email: formState.email.currentValue ?? selectedEmail }));
    }
  }
</script>

<BadgeButton type="badge-success" icon="i-mdi-account-plus-outline" on:click={openModal}>
  {$t('project_page.add_user.add_button')}
</BadgeButton>

<FormModal bind:this={formModal} {schema} let:errors>
  <span slot="title">{$t('project_page.add_user.modal_title')}</span>
{#if isAdmin($page.data.user)}
  <UserTypeahead
    id="email"
    label={$t('admin_dashboard.column_email')}
    bind:result={selectedUser}
    error={errors.email}
    autofocus
    />
{:else}
  <Input
    id="email"
    type="email"
    label={$t('admin_dashboard.column_email')}
    bind:value={$form.email}
    error={errors.email}
    autofocus
  />
{/if}
  <ProjectRoleSelect bind:value={$form.role} error={errors.role} />
  <span slot="submitText">{$t('project_page.add_user.submit_button')}</span>
</FormModal>
