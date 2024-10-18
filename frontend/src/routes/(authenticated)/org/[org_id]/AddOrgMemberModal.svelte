<script lang="ts">
  import { DialogResponse, FormModal } from '$lib/components/modals';
  import { Checkbox, Input, OrgRoleSelect, isEmail } from '$lib/forms';
  import { OrgRole } from '$lib/gql/types';
  import t from '$lib/i18n';
  import { z } from 'zod';
  import { useNotifications } from '$lib/notify';
  import { page } from '$app/stores'
  import UserTypeahead from '$lib/forms/UserTypeahead.svelte';
  import { SupHelp, helpLinks } from '$lib/components/help';
  import type { UUID } from 'crypto';
  import { _addOrgMember } from './+page';
  import type { SingleUserTypeaheadResult, UsersInMyOrgTypeaheadResult } from '$lib/gql/typeahead-queries';
  import UserProjects, { type Project } from '$lib/components/Users/UserProjects.svelte';

  export let orgId: string;
  const schema = z.object({
    usernameOrEmail: z.string().trim()
      .min(1, $t('org_page.add_user.empty_user_field'))
      .refine((value) => !value.includes('@') || isEmail(value), { message: $t('form.invalid_email') }),
    role: z.enum([OrgRole.User, OrgRole.Admin]).default(OrgRole.User),
    canInvite: z.boolean().default(false),
  });
  let formModal: FormModal<typeof schema>;
  $: form = formModal?.form();

  const { notifySuccess } = useNotifications();

  let projects: Project[] = [];
  function populateUserProjects(user: SingleUserTypeaheadResult | UsersInMyOrgTypeaheadResult | null): void {
    if (!user || !('projects' in user)) {
      projects = [];
    } else {
      projects = [...user.projects.map(p => ({role: p.role, id: p.project.id, code: p.project.code, name: p.project.name}))];
    }
  }

  export async function openModal(): Promise<void> {
    let userInvited = false;
    const { response, formState } = await formModal.open(async () => {
      const { error } = await _addOrgMember(
        orgId as UUID,
        $form.usernameOrEmail,
        $form.role,
        $form.canInvite,
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

      return error?.message;
    });
    if (response === DialogResponse.Submit) {
      const message = userInvited ? 'member_invited' : 'add_member';
      notifySuccess($t(`org_page.notifications.${message}`, { email: formState.usernameOrEmail.currentValue }));
    }
  }
</script>

<FormModal bind:this={formModal} {schema} let:errors --justify-actions="end">
  <span slot="title">
    {$t('org_page.add_user.modal_title')}
    <SupHelp helpLink={helpLinks.addOrgMember} />
    <!-- TODO: helpLinks.addOrgMember currently points to Add_Project_Member scribe. Create a scribe with Add_Org_Member help. -->
  </span>
  {#if $page.data.user?.isAdmin}
    <UserTypeahead
      id="usernameOrEmail"
      label={$t('login.label_email')}
      isAdmin={$page.data.user?.isAdmin}
      bind:value={$form.usernameOrEmail}
      error={errors.usernameOrEmail}
      on:selectedUser={(event) => populateUserProjects(event.detail)}
      autofocus
      />
  {:else}
    <Input
      id="usernameOrEmail"
      type="text"
      label={$t('login.label_email')}
      bind:value={$form.usernameOrEmail}
      error={errors.usernameOrEmail}
      autofocus
    />
  {/if}
  <OrgRoleSelect bind:value={$form.role} error={errors.role} />
  {#if projects && projects.length}
    {$t('org_page.add_user.also_add_projects')}
    <UserProjects {projects} />
  {/if}
  <svelte:fragment slot="extraActions">
    <Checkbox
      id="invite"
      label={$t('org_page.add_user.invite')}
      variant="checkbox-warning"
      labelColor="text-warning"
      bind:value={$form.canInvite}
    />
  </svelte:fragment>
  <span slot="submitText">
    {#if $form.canInvite && $form.usernameOrEmail.includes('@')}
      {$t('org_page.add_user.submit_button_email')}
    {:else}
      {$t('org_page.add_user.submit_button')}
    {/if}
  </span>
</FormModal>
