<script lang="ts">
  import { BadgeButton, MemberBadge } from '$lib/components/Badges';
  import { DialogResponse, FormModal } from '$lib/components/modals';
  import { Input, TextArea, passwordFormRules } from '$lib/forms';
  import { ProjectRole, type BulkAddProjectMembersResult } from '$lib/gql/types';
  import t from '$lib/i18n';
  import { z } from 'zod';
  import { _bulkAddProjectMembers } from './+page';
  import { hash } from '$lib/util/hash';
  import { AdminContent } from '$lib/layout';
  import Icon from '$lib/icons/Icon.svelte';
  import BadgeList from '$lib/components/Badges/BadgeList.svelte';

  enum BulkAddSteps {
    Add,
    Results,
  }

  let currentStep = BulkAddSteps.Add;

  export let projectId: string;
  const schema = z.object({
    usernamesText: z.string().min(1, $t('register.name_missing')),
    password: passwordFormRules($t),
  });

  let formModal: FormModal<typeof schema>;
  $: form = formModal?.form();

  let addedMembers: BulkAddProjectMembersResult['addedMembers'] = [{username: 'test2', 'role': ProjectRole.Manager}];
  let createdMembers: BulkAddProjectMembersResult['createdMembers'] = [{username: 'test', 'role': ProjectRole.Editor}];
  let existingMembers: BulkAddProjectMembersResult['existingMembers'] = [{username: 'test2', 'role': ProjectRole.Manager}];
  $: addedCount = addedMembers.length + createdMembers.length;

  const usernameRe = /^[a-zA-Z0-9_]+$/;

  function validateUsernames(usernames: string[]): boolean {
    return usernames.every(s => usernameRe.test(s));
  }

  async function openModal(): Promise<void> {
    currentStep = BulkAddSteps.Add;
    const { response } = await formModal.open(undefined, async (state) => {
      const passwordHash = await hash(state.password.currentValue);
      const usernames = state.usernamesText.currentValue
        .split('\n')
        // Remove whitespace
        .map(s => s.trim())
        // Remove empty lines before validating, otherwise final newline would count as invalid because empty string
        .filter(s => s);
      if (!validateUsernames(usernames)) {
        return $t('project_page.bulk_add_members.usernames_alphanum_only');
      }
      const { error, data } = await _bulkAddProjectMembers({
        projectId,
        passwordHash,
        usernames,
        role: ProjectRole.Editor, // Managers not allowed to have shared passwords
      });

      addedMembers = data?.bulkAddProjectMembers.bulkAddProjectMembersResult?.addedMembers ?? [];
      createdMembers = data?.bulkAddProjectMembers.bulkAddProjectMembersResult?.createdMembers ?? [];
      existingMembers = data?.bulkAddProjectMembers.bulkAddProjectMembersResult?.existingMembers ?? [];
      return error?.message;
    }, { keepOpenOnSubmit: true });

    if (response === DialogResponse.Submit) {
      currentStep = BulkAddSteps.Results;
    }
  }
</script>

<AdminContent>
  <BadgeButton type="badge-success" icon="i-mdi-account-plus-outline" on:click={openModal}>
    {$t('project_page.bulk_add_members.add_button')}
  </BadgeButton>

  <FormModal bind:this={formModal} {schema} let:errors>
    <span slot="title">{$t('project_page.bulk_add_members.modal_title')}</span>
    {#if currentStep == BulkAddSteps.Add}
      <p>{$t('project_page.bulk_add_members.explanation')}</p>
      <Input
        id="password"
        type="password"
        label={$t('project_page.bulk_add_members.shared_password')}
        bind:value={$form.password}
        error={errors.password}
      />
      <TextArea
        id="usernamesText"
        label={$t('project_page.bulk_add_members.usernames')}
        bind:value={$form.usernamesText}
        error={errors.usernamesText}
      />
    {:else if currentStep == BulkAddSteps.Results}
      <p class="flex gap-1 items-center mb-2">
        <Icon icon="i-mdi-plus" color="text-success" />
        {$t('project_page.bulk_add_members.members_added', {addedCount})}
      </p>
      <div class="mb-4 ml-8">
        <p class="flex gap-1 items-center mb-2">
          <Icon icon="i-mdi-account-outline" color="text-success" />
          {$t('project_page.bulk_add_members.existing_added_members', {existedCount: addedMembers.length})}
        </p>
        <BadgeList>
          {#each addedMembers as user}
            <MemberBadge member={{ name: user.username, role: user.role }} />
          {/each}
        </BadgeList>
      </div>
      <div class="mb-4 ml-8">
        <p class="flex gap-1 items-center mb-2">
          <Icon icon="i-mdi-creation-outline" color="text-success" />
          {$t('project_page.bulk_add_members.accounts_created', {createdCount: createdMembers.length})}
        </p>
        <BadgeList>
          {#each createdMembers as user}
            <MemberBadge member={{ name: user.username, role: user.role }} />
          {/each}
        </BadgeList>
      </div>
      <p class="flex gap-1 items-center mb-2">
        <Icon icon="i-mdi-account-outline" color="text-info" />
        {$t('project_page.bulk_add_members.already_members', {count: existingMembers.length})}
      </p>
      <BadgeList>
        {#each existingMembers as user}
          <MemberBadge member={{ name: user.username, role: user.role }} />
        {/each}
      </BadgeList>
    {:else}
      <p>Internal error: unknown step {currentStep}</p>
    {/if}
    <span slot="submitText">{$t('project_page.bulk_add_members.submit_button')}</span>
    <span slot="closeText">{$t('project_page.bulk_add_members.finish_button')}</span>
  </FormModal>
</AdminContent>
