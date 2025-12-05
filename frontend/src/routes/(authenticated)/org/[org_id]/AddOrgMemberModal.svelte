<script lang="ts">
  import {DialogResponse, FormModal} from '$lib/components/modals';
  import {Checkbox, Input, OrgRoleSelect, isEmail} from '$lib/forms';
  import {OrgRole} from '$lib/gql/types';
  import t from '$lib/i18n';
  import {z} from 'zod';
  import {useNotifications} from '$lib/notify';
  import {page} from '$app/stores';
  import UserTypeahead from '$lib/forms/UserTypeahead.svelte';
  import {SupHelp, helpLinks} from '$lib/components/help';
  import type {UUID} from 'crypto';
  import {_addOrgMember, type Org} from './+page';
  import type {SingleUserICanSeeTypeaheadResult, SingleUserTypeaheadResult} from '$lib/gql/typeahead-queries';
  import UserProjects, {type Project} from '$lib/components/Users/UserProjects.svelte';

  interface Props {
    org: Org;
  }

  const { org }: Props = $props();

  const schema = z.object({
    usernameOrEmail: z
      .string()
      .trim()
      .min(1, $t('org_page.add_user.empty_user_field'))
      .refine((value) => !value.includes('@') || isEmail(value), { error: $t('form.invalid_email') }),
    role: z.enum([OrgRole.User, OrgRole.Admin]).default(OrgRole.User),
    canInvite: z.boolean().default(false),
  });
  let formModal: FormModal<typeof schema> | undefined = $state();
  let form = $derived(formModal?.form());

  const { notifySuccess } = useNotifications();

  let newProjects: Project[] = $state([]);
  let alreadyAddedProjects: Project[] = $state([]);
  let selectedProjects: string[] = $state([]);

  function resetProjects(): void {
    newProjects = [];
    alreadyAddedProjects = [];
    selectedProjects = [];
  }

  function populateUserProjects(user: SingleUserTypeaheadResult | SingleUserICanSeeTypeaheadResult | null): void {
    resetProjects();
    if (user && 'projects' in user) {
      const userProjects = [
        ...user.projects.map((p) => ({
          memberRole: p.role,
          id: p.project.id,
          code: p.project.code,
          name: p.project.name,
        })),
      ];
      userProjects.forEach((proj) => {
        if (org.projects.find((p) => p.id === proj.id)) {
          alreadyAddedProjects.push(proj);
        } else {
          newProjects.push(proj);
        }
      });
    }
  }

  export async function openModal(): Promise<void> {
    if (!formModal || !$form) return;
    resetProjects();
    let userInvited = false;
    const { response, formState } = await formModal.open(async () => {
      const { error } = await _addOrgMember(
        org.id as UUID,
        $form.usernameOrEmail,
        $form.role,
        $form.canInvite,
        selectedProjects,
      );

      if (error?.byType('NotFoundError')) {
        if (error.message === 'Org not found') {
          return $t('org_page.add_user.org_not_found');
        } else {
          return { usernameOrEmail: [$t('org_page.add_user.username_not_found')] };
        }
      }
      if (error?.byType('OrgMemberInvitedByEmail')) {
        userInvited = true;
        return undefined; // Close modal as if success
      }
      if (error?.byType('OrgMembersMustBeVerified')) {
        return { usernameOrEmail: [$t('org_page.add_user.user_must_be_verified')] };
      }
      if (error?.byType('OrgMembersMustBeVerifiedForRole')) {
        return { role: [$t('org_page.add_user.admin_must_be_verified')] };
      }

      return error?.message;
    });
    if (response === DialogResponse.Submit) {
      const message = userInvited ? 'member_invited' : 'add_member';
      notifySuccess($t(`org_page.notifications.${message}`, { email: formState.usernameOrEmail.currentValue }));
      if (selectedProjects.length) {
        notifySuccess($t('org_page.notifications.added_projects', { count: selectedProjects.length }));
      }
    }
  }
</script>

<FormModal bind:this={formModal} {schema} --justify-actions="end">
  {#snippet title()}
    <span>
      {$t('org_page.add_user.modal_title')}
      <SupHelp helpLink={helpLinks.addOrgMember} />
      <!-- TODO: helpLinks.addOrgMember currently points to Add_Project_Member scribe. Create a scribe with Add_Org_Member help. -->
    </span>
  {/snippet}
  {#snippet children({ errors })}
    {#if $page.data.user?.isAdmin}
      <UserTypeahead
        id="usernameOrEmail"
        label={$t('login.label_email')}
        isAdmin={$page.data.user?.isAdmin}
        bind:value={$form!.usernameOrEmail}
        error={errors.usernameOrEmail}
        onSelectedUserChange={populateUserProjects}
        autofocus
        exclude={org.members.map((m) => m.user.id)}
      />
    {:else}
      <Input
        id="usernameOrEmail"
        type="text"
        label={$t('login.label_email')}
        bind:value={$form!.usernameOrEmail}
        error={errors.usernameOrEmail}
        autofocus
      />
    {/if}
    <OrgRoleSelect bind:value={$form!.role} error={errors.role} />
    {#if newProjects.length || alreadyAddedProjects.length}
      <div class="label label-text">
        {$t('org_page.add_user.also_add_projects')}
      </div>
      {#if newProjects.length}
        <UserProjects projects={newProjects} bind:selectedProjects />
      {:else}
        <span class="text-secondary px-1">
          {$t('org_page.add_user.all_projects_already_added', { count: alreadyAddedProjects.length })}
        </span>
      {/if}
    {/if}
  {/snippet}
  {#snippet extraActions()}
    <Checkbox
      id="invite"
      label={$t('org_page.add_user.invite')}
      variant="checkbox-warning"
      labelColor="text-warning"
      bind:value={$form!.canInvite}
    />
  {/snippet}
  {#snippet submitText()}
    <span>
      {#if $form!.canInvite && $form!.usernameOrEmail.includes('@')}
        {$t('org_page.add_user.submit_button_email')}
      {:else}
        {$t('org_page.add_user.submit_button')}
      {/if}
    </span>
  {/snippet}
</FormModal>
