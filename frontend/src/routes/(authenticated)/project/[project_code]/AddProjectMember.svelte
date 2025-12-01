<script lang="ts">
  import {DialogResponse, FormModal} from '$lib/components/modals';
  import {ProjectRoleSelect, isEmail} from '$lib/forms';
  import {ProjectRole} from '$lib/gql/types';
  import t from '$lib/i18n';
  import {z} from 'zod';
  import {_addProjectMember, type Project} from './+page';
  import {useNotifications} from '$lib/notify';
  import {page} from '$app/stores';
  import UserTypeahead from '$lib/forms/UserTypeahead.svelte';
  import {SupHelp, helpLinks} from '$lib/components/help';
  import Checkbox from '$lib/forms/Checkbox.svelte';

  interface Props {
    project: Project;
  }

  const { project }: Props = $props();
  const schema = z.object({
    usernameOrEmail: z
      .string()
      .trim()
      .min(1, $t('project_page.add_user.empty_user_field'))
      .refine((value) => !value.includes('@') || isEmail(value), { message: $t('form.invalid_email') }),
    role: z.enum([ProjectRole.Editor, ProjectRole.Manager, ProjectRole.Observer]).default(ProjectRole.Editor),
    canInvite: z.boolean().default(false),
  });
  let formModal: FormModal<typeof schema> | undefined = $state();
  let form = $derived(formModal?.form());
  let selectedUserId: string | undefined = $state(undefined);

  const { notifySuccess } = useNotifications();

  export async function openModal(initialUserId?: string, initialUserName?: string): Promise<void> {
    if (!formModal || !$form) return;
    let userInvited = false;
    const initialValue = initialUserName ? { usernameOrEmail: initialUserName } : undefined;
    if (initialUserId) selectedUserId = initialUserId;
    const { response, formState } = await formModal.open(initialValue, async () => {
      const { error } = await _addProjectMember({
        projectId: project.id,
        usernameOrEmail: $form.usernameOrEmail ?? '',
        userId: selectedUserId,
        role: $form.role,
        canInvite: $form.canInvite,
      });

      if (error?.byType('NotFoundError')) {
        if (error.message === 'Project not found') {
          return $t('project_page.add_user.project_not_found');
        } else {
          return { usernameOrEmail: [$t('project_page.add_user.username_not_found')] };
        }
      }
      if (error?.byType('InvalidEmailError')) {
        return { usernameOrEmail: [$t('form.invalid_email')] };
      }
      if (error?.byType('ProjectMembersMustBeVerified')) {
        return { usernameOrEmail: [$t('project_page.add_user.user_must_be_verified')] };
      }
      if (error?.byType('ProjectMembersMustBeVerifiedForRole')) {
        return { role: [$t('project_page.add_user.manager_must_be_verified')] };
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

<FormModal bind:this={formModal} {schema} --justify-actions="end">
  {#snippet title()}
    <span>
      {$t('project_page.add_user.modal_title')}
      <SupHelp helpLink={helpLinks.addProjectMember} />
    </span>
  {/snippet}
  {#snippet children({ errors })}
    <UserTypeahead
      id="usernameOrEmail"
      isAdmin={$page.data.user?.isAdmin}
      label={$t('login.label_email')}
      bind:value={$form!.usernameOrEmail}
      error={errors.usernameOrEmail}
      autofocus
      onSelectedUserChange={(user) => {
        selectedUserId = user?.id;
      }}
      exclude={project.users.map((m) => m.user.id)}
    />
    <ProjectRoleSelect bind:value={$form!.role} error={errors.role} showObserver={project.hasHarmonyCommits} />
  {/snippet}
  {#snippet extraActions()}
    <Checkbox
      id="invite"
      label={$t('project_page.add_user.invite_checkbox')}
      variant="checkbox-warning"
      labelColor="text-warning"
      bind:value={$form!.canInvite}
    />
  {/snippet}
  {#snippet submitText()}
    <span>
      {#if $form!.canInvite && $form!.usernameOrEmail.includes('@')}
        {$t('project_page.add_user.submit_button_invite')}
      {:else}
        {$t('project_page.add_user.submit_button')}
      {/if}
    </span>
  {/snippet}
</FormModal>
