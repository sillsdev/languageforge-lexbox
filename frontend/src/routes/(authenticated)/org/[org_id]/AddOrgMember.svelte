<script lang="ts">
  import { BadgeButton } from '$lib/components/Badges';
  import { DialogResponse, FormModal } from '$lib/components/modals';
  import { Input, OrgRoleSelect, isEmail } from '$lib/forms';
  import { OrgRole } from '$lib/gql/types';
  import t from '$lib/i18n';
  import { z } from 'zod';
  import { _addOrgMember } from './+page';
  import { useNotifications } from '$lib/notify';
  import { page } from '$app/stores'
  import UserTypeahead from '$lib/forms/UserTypeahead.svelte';
  import type { SingleUserTypeaheadResult } from '$lib/gql/typeahead-queries';
  import { SupHelp, helpLinks } from '$lib/components/help';
  import type { UUID } from 'crypto';

  export let orgId: string;
  const schema = z.object({
    usernameOrEmail: z.string().trim()
      .min(1, $t('org_page.add_user.empty_user_field'))
      .refine((value) => !value.includes('@') || isEmail(value), { message: $t('form.invalid_email') }),
    role: z.enum([OrgRole.User, OrgRole.Admin]).default(OrgRole.User),
  });
  let formModal: FormModal<typeof schema>;
  $: form = formModal?.form();

  let selectedUser: SingleUserTypeaheadResult;

  const { notifySuccess } = useNotifications();

  async function openModal(): Promise<void> {
    let userInvited = false;
    let selectedEmail: string = '';
    const { response, formState } = await formModal.open(async () => {
      selectedEmail = $form.usernameOrEmail ? $form.usernameOrEmail : selectedUser?.email ?? selectedUser?.username ?? '';
      const { error } = await _addOrgMember(
        orgId as UUID,
        selectedEmail,
        $form.role,
      );

      if (error?.byType('NotFoundError')) {
        if (error.message === 'Org not found') {
          return $t('org_page.add_user.org_not_found');
        } else {
          return { usernameOrEmail: [$t('org_page.add_user.username_not_found')] };
        }
      }
      // TODO: _addOrgMember should probably return some of the error types below as well
      // if (error?.byType('InvalidEmailError')) {
      //   return { usernameOrEmail: [$t('form.invalid_email')] };
      // }
      // if (error?.byType('OrgMembersMustBeVerified')) {
      //   return { usernameOrEmail: [$t('org_page.add_user.user_must_be_verified')] };
      // }
      // if (error?.byType('OrgMembersMustBeVerifiedForRole')) {
      //   return { role: [$t('org_page.add_user.admin_must_be_verified')] };
      // }
      // if (error?.byType('AlreadyExistsError')) {
      //   return { usernameOrEmail: [$t('org_page.add_user.user_already_member')] };
      // }
      // if (error?.byType('OrgMemberInvitedByEmail')) {
      //   userInvited = true;
      //   return undefined; // Close modal as if success
      // }

      return error?.message;
    });
    if (response === DialogResponse.Submit) {
      const message = userInvited ? 'member_invited' : 'add_member';
      notifySuccess($t(`org_page.notifications.${message}`, { email: formState.usernameOrEmail.currentValue ?? selectedEmail }));
    }
  }
</script>

<BadgeButton variant="badge-success" icon="i-mdi-account-plus-outline" on:click={openModal}>
  {$t('org_page.add_user.add_button')}
</BadgeButton>

<FormModal bind:this={formModal} {schema} let:errors>
  <span slot="title">
    {$t('org_page.add_user.modal_title')}
    <SupHelp helpLink={helpLinks.addProjectMember} />
    <!-- TODO: helpLinks.addOrgMember -->
  </span>
  {#if $page.data.user?.isAdmin}
    <UserTypeahead
      id="usernameOrEmail"
      label={$t('login.label_email')}
      bind:value={$form.usernameOrEmail}
      error={errors.usernameOrEmail}
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
  <OrgRoleSelect withLabel bind:value={$form.role} error={errors.role} />
  <span slot="submitText">
    {#if $form.usernameOrEmail.includes('@')}
      {$t('org_page.add_user.submit_button_email')}
    {:else}
      {$t('org_page.add_user.submit_button')}
    {/if}
  </span>
</FormModal>
