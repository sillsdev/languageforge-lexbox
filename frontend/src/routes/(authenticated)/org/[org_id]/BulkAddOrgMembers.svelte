<script lang="ts">
  import {DialogResponse, FormModal, type FormSubmitReturn} from '$lib/components/modals';
  import {TextArea, isEmail} from '$lib/forms';
  import {OrgRole, type BulkAddOrgMembersResult} from '$lib/gql/types';
  import t from '$lib/i18n';
  import {z} from 'zod';
  import {_bulkAddOrgMembers} from './+page';
  import Icon from '$lib/icons/Icon.svelte';
  import BadgeList from '$lib/components/Badges/BadgeList.svelte';
  import {distinct} from '$lib/util/array';
  import {SupHelp, helpLinks} from '$lib/components/help';
  import {usernameRe} from '$lib/user';
  import OrgMemberBadge from '$lib/components/Badges/OrgMemberBadge.svelte';
  import type {UUID} from 'crypto';
  import {invalidate} from '$app/navigation';

  // svelte-ignore non_reactive_update
  enum BulkAddSteps {
    Add,
    Results,
  }

  let currentStep = $state(BulkAddSteps.Add);

  interface Props {
    orgId: string;
  }

  const { orgId }: Props = $props();
  const schema = z.object({
    usernamesText: z.string().trim().min(1, $t('org_page.bulk_add_members.empty_user_field')),
  });

  let formModal: FormModal<typeof schema> | undefined = $state();
  let form = $derived(formModal?.form());

  let addedMembers: BulkAddOrgMembersResult['addedMembers'] = $state([]);
  let notFoundMembers: BulkAddOrgMembersResult['notFoundMembers'] = $state([]);
  let existingMembers: BulkAddOrgMembersResult['existingMembers'] = $state([]);

  function validateBulkAddInput(usernames: string[]): FormSubmitReturn<typeof schema> {
    if (usernames.length === 0) return { usernamesText: [$t('org_page.bulk_add_members.empty_user_field')] };

    for (const username of usernames) {
      if (username.includes('@')) {
        if (!isEmail(username))
          return { usernamesText: [$t('org_page.bulk_add_members.invalid_email_address', { email: username })] };
      } else if (!usernameRe.test(username)) {
        return { usernamesText: [$t('org_page.bulk_add_members.invalid_username', { username })] };
      }
    }
  }

  export async function open(): Promise<void> {
    if (!formModal) return;
    currentStep = BulkAddSteps.Add;
    const { response } = await formModal.open(undefined, async (state) => {
      const usernames = state.usernamesText.currentValue
        .split('\n')
        // Remove whitespace
        .map((s) => s.trim())
        // Remove empty lines before validating, otherwise final newline would count as invalid because empty string
        .filter((s) => s)
        .filter(distinct);

      const bulkErrors = validateBulkAddInput(usernames);
      if (bulkErrors) return bulkErrors;

      const { error, data } = await _bulkAddOrgMembers(orgId as UUID, usernames, OrgRole.User);

      addedMembers = data?.bulkAddOrgMembers.bulkAddOrgMembersResult?.addedMembers ?? [];
      notFoundMembers = data?.bulkAddOrgMembers.bulkAddOrgMembersResult?.notFoundMembers ?? [];
      existingMembers = data?.bulkAddOrgMembers.bulkAddOrgMembersResult?.existingMembers ?? [];
      return error?.message;
    });

    if (response === DialogResponse.Submit) {
      await invalidate(`org:${orgId}`);
      currentStep = BulkAddSteps.Results;
    }
  }
</script>

<FormModal bind:this={formModal} {schema} showDoneState>
  {#snippet title()}
    <span>
      {$t('org_page.bulk_add_members.modal_title')}
      <SupHelp helpLink={helpLinks.bulkAddCreate} />
    </span>
  {/snippet}
  {#snippet children({ errors })}
    {#if currentStep == BulkAddSteps.Add}
      <p class="mb-2">{$t('org_page.bulk_add_members.explanation')}</p>
      <div class="contents usernames">
        <TextArea
          id="usernamesText"
          label={$t('org_page.bulk_add_members.usernames')}
          description={$t('org_page.bulk_add_members.usernames_description')}
          bind:value={$form!.usernamesText}
          error={errors.usernamesText}
        />
      </div>
    {:else if currentStep == BulkAddSteps.Results}
      <div class="mb-4">
        <p class="flex gap-1 items-center">
          <Icon icon="i-mdi-plus" color="text-success" />
          {$t('org_page.bulk_add_members.members_added', { addedCount: addedMembers.length })}
        </p>
        {#if addedMembers.length > 0}
          <div class="mt-2">
            <BadgeList>
              {#each addedMembers as user (user.username)}
                <OrgMemberBadge member={{ name: user.username, role: user.role }} />
              {/each}
            </BadgeList>
          </div>
        {/if}
      </div>
      <div class="mb-4">
        <p class="flex gap-1 items-center">
          <Icon icon="i-mdi-account-off" color="text-info" />
          {$t('org_page.bulk_add_members.accounts_not_found', { notFoundCount: notFoundMembers.length })}
        </p>
        {#if notFoundMembers.length > 0}
          <div class="mt-2">
            <BadgeList>
              {#each notFoundMembers as user (user.username)}
                <OrgMemberBadge member={{ name: user.username, role: user.role }} />
              {/each}
            </BadgeList>
          </div>
        {/if}
      </div>
      {#if existingMembers.length > 0}
        <p class="flex gap-1 items-center">
          <Icon icon="i-mdi-account-outline" color="text-info" />
          {$t('org_page.bulk_add_members.already_members', { count: existingMembers.length })}
        </p>
        <div class="mt-2">
          <BadgeList>
            {#each existingMembers as user (user.username)}
              <OrgMemberBadge member={{ name: user.username, role: user.role }} />
            {/each}
          </BadgeList>
        </div>
      {/if}
    {:else}
      <p>Internal error: unknown step {currentStep}</p>
    {/if}
  {/snippet}
  {#snippet submitText()}
    <span>{$t('org_page.bulk_add_members.submit_button')}</span>
  {/snippet}
  {#snippet doneText()}
    <span>{$t('org_page.bulk_add_members.finish_button')}</span>
  {/snippet}
</FormModal>

<style lang="postcss">
  .usernames :global(.description) {
    @apply text-success;
  }
</style>
