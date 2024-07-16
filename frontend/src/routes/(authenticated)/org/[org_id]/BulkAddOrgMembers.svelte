<script lang="ts">
  import Button from '$lib/forms/Button.svelte';
  import { DialogResponse, FormModal, type FormSubmitReturn } from '$lib/components/modals';
  import { TextArea, isEmail } from '$lib/forms';
  import { OrgRole, type BulkAddOrgMembersResult } from '$lib/gql/types';
  import t from '$lib/i18n';
  import { z } from 'zod';
  import { _bulkAddOrgMembers } from './+page';
  import { AdminContent } from '$lib/layout';
  import Icon from '$lib/icons/Icon.svelte';
  import BadgeList from '$lib/components/Badges/BadgeList.svelte';
  import { distinct } from '$lib/util/array';
  import { SupHelp, helpLinks } from '$lib/components/help';
  import { usernameRe } from '$lib/user';
  import OrgMemberBadge from '$lib/components/Badges/OrgMemberBadge.svelte';
  import type { UUID } from 'crypto';

  enum BulkAddSteps {
    Add,
    Results,
  }

  let currentStep = BulkAddSteps.Add;

  export let orgId: string;
  const schema = z.object({
    usernamesText: z.string().trim().min(1, $t('org_page.bulk_add_members.empty_user_field')),
  });

  let formModal: FormModal<typeof schema>;
  $: form = formModal?.form();

  let addedMembers: BulkAddOrgMembersResult['addedMembers'] = [];
  let notFoundMembers: BulkAddOrgMembersResult['notFoundMembers'] = [];
  let existingMembers: BulkAddOrgMembersResult['existingMembers'] = [];

  function validateBulkAddInput(usernames: string[]): FormSubmitReturn<typeof schema> {
    if (usernames.length === 0) return { usernamesText: [$t('org_page.bulk_add_members.empty_user_field')] };

    for (const username of usernames) {
      if (username.includes('@')) {
        if (!isEmail(username)) return { usernamesText: [$t('org_page.bulk_add_members.invalid_email_address', { email: username })] };
      } else if (!usernameRe.test(username)) {
        return { usernamesText: [$t('org_page.bulk_add_members.invalid_username', { username })] };
      }
    }
  }

  async function openModal(): Promise<void> {
    currentStep = BulkAddSteps.Add;
    console.log('Opening modal');
    const { response } = await formModal.open(undefined, async (state) => {
      console.log('Submit button clicked');
      const usernames = state.usernamesText.currentValue
        .split('\n')
        // Remove whitespace
        .map(s => s.trim())
        // Remove empty lines before validating, otherwise final newline would count as invalid because empty string
        .filter(s => s)
        .filter(distinct);

      console.log('Usernames:', usernames);

      const bulkErrors = validateBulkAddInput(usernames);
      if (bulkErrors) return bulkErrors;

      const { error, data } = await _bulkAddOrgMembers(
        orgId as UUID,
        usernames,
        OrgRole.User,
      );

      console.log('Error:', error);
      console.log('Data:', data);

      addedMembers = data?.bulkAddOrgMembers.bulkAddOrgMembersResult?.addedMembers ?? [];
      notFoundMembers = data?.bulkAddOrgMembers.bulkAddOrgMembersResult?.notFoundMembers ?? [];
      existingMembers = data?.bulkAddOrgMembers.bulkAddOrgMembersResult?.existingMembers ?? [];
      return error?.message;
    }, { keepOpenOnSubmit: true });

    if (response === DialogResponse.Submit) {
      currentStep = BulkAddSteps.Results;
    }
  }
</script>

<AdminContent>
  <Button variant="btn-success" on:click={openModal}>
    {$t('org_page.bulk_add_members.add_button')}
    <span class="i-mdi-account-multiple-plus-outline text-2xl" />
  </Button>

  <FormModal bind:this={formModal} {schema} let:errors>
    <span slot="title">
      {$t('org_page.bulk_add_members.modal_title')}
      <SupHelp helpLink={helpLinks.bulkAddCreate} />
    </span>
    {#if currentStep == BulkAddSteps.Add}
      <p class="mb-2">{$t('org_page.bulk_add_members.explanation')}</p>
      <div class="contents usernames">
        <TextArea
          id="usernamesText"
          label={$t('org_page.bulk_add_members.usernames')}
          description={$t('org_page.bulk_add_members.usernames_description')}
          bind:value={$form.usernamesText}
          error={errors.usernamesText}
        />
      </div>
    {:else if currentStep == BulkAddSteps.Results}
      <div class="mb-4">
        <p class="flex gap-1 items-center">
          <Icon icon="i-mdi-plus" color="text-success" />
          {$t('org_page.bulk_add_members.members_added', {addedCount: addedMembers.length})}
        </p>
        {#if addedMembers.length > 0}
          <div class="mt-2">
            <BadgeList>
              {#each addedMembers as user}
              <OrgMemberBadge member={{ name: user.username, role: user.role }} />
              {/each}
            </BadgeList>
          </div>
        {/if}
      </div>
      <div class="mb-4">
        <p class="flex gap-1 items-center">
          <Icon icon="i-mdi-account-off" color="text-info" />
          {$t('org_page.bulk_add_members.accounts_not_found', {notFoundCount: notFoundMembers.length})}
        </p>
        {#if notFoundMembers.length > 0}
          <div class="mt-2">
            <BadgeList>
              {#each notFoundMembers as user}
                <OrgMemberBadge member={{ name: user.username, role: user.role }} />
              {/each}
            </BadgeList>
          </div>
        {/if}
      </div>
      {#if existingMembers.length > 0}
        <p class="flex gap-1 items-center">
          <Icon icon="i-mdi-account-outline" color="text-info" />
          {$t('org_page.bulk_add_members.already_members', {count: existingMembers.length})}
        </p>
        <div class="mt-2">
          <BadgeList>
            {#each existingMembers as user}
              <OrgMemberBadge member={{ name: user.username, role: user.role }} />
            {/each}
          </BadgeList>
        </div>
      {/if}
    {:else}
      <p>Internal error: unknown step {currentStep}</p>
    {/if}
    <span slot="submitText">{$t('org_page.bulk_add_members.submit_button')}</span>
    <span slot="closeText">{$t('org_page.bulk_add_members.finish_button')}</span>
  </FormModal>
</AdminContent>

<style lang="postcss">
  .usernames :global(.description) {
    @apply text-success;
  }
</style>
