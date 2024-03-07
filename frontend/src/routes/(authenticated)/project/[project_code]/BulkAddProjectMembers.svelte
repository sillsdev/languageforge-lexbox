<script lang="ts">
  import { BadgeButton, MemberBadge } from '$lib/components/Badges';
  import { DialogResponse, FormModal } from '$lib/components/modals';
  import { Input, ProjectRoleSelect, TextArea, passwordFormRules } from '$lib/forms';
  import { ChangeProjectDescriptionDocument, ProjectRole } from '$lib/gql/types';
  import t from '$lib/i18n';
  import { z } from 'zod';
  import { _bulkAddProjectMembers } from './+page';
  import { hash } from '$lib/util/hash';
  import { AdminContent } from '$lib/layout';
  import { createEventDispatcher } from 'svelte';
  import Icon from '$lib/icons/Icon.svelte';
  import BadgeList from '$lib/components/Badges/BadgeList.svelte';

  enum BulkAddSteps {
    Add,
    Results,
  }

  let currentStep = BulkAddSteps.Add;

  const dispatch = createEventDispatcher();

  export let projectId: string;
  const schema = z.object({
    usernamesText: z.string().min(1, $t('register.name_missing')),
    password: passwordFormRules($t),
  });

  let formModal: FormModal<typeof schema>;
  $: form = formModal?.form();

  let createdCount: number;
  let usernameConflicts: string[] = [];

  const usernameRe = /^[a-zA-Z0-9_]+$/;

  function validateUsernames(usernames: string[]): boolean {
    return usernames.every(s => usernameRe.test(s));
  }

  async function openModal(): Promise<void> {
    const { response, formState } = await formModal.open(async () => {
      const passwordHash = await hash($form.password);
      const usernames = $form.usernamesText.split('\n').map(s => s.trim());
      if (!validateUsernames(usernames)) {
        return $t('project_page.bulk_add_members.usernames_alphanum_only');
      }
      const { error, data } = await _bulkAddProjectMembers({
        projectId,
        passwordHash,
        usernames,
        role: ProjectRole.Editor, // Managers not allowed to have shared passwords
      });

      createdCount = data?.bulkAddProjectMembers.bulkAddProjectMembersResult?.createdCount ?? 0;
      usernameConflicts = data?.bulkAddProjectMembers.bulkAddProjectMembersResult?.usernameConflicts ?? [];
      return error?.message;
    });
    if (response === DialogResponse.Submit) {
      if (currentStep < BulkAddSteps.Results) {
        // Go to next page
        currentStep++;
        await openModal();
      } else {
        currentStep = BulkAddSteps.Add;
        dispatch('bulkCreated', createdCount);
        return;
      }
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
    <p class="flex gap-1 items-center mb-4"><Icon icon="i-mdi-check" color="text-success" /> {$t('project_page.bulk_add_members.accounts_created', {createdCount})}</p>
      {#if usernameConflicts && usernameConflicts.length > 0}
        <p class="mb-2">{$t('project_page.bulk_add_members.username_conflict_explanation')}</p>
        <BadgeList>
          {#each usernameConflicts as username}
          <MemberBadge member={{ name: username, role: ProjectRole.Editor }} canManage={false} />
          {/each}
        </BadgeList>
      {/if}
    {:else}
    <p>Internal error: unknown step {currentStep}</p>
    {/if}
  <span slot="submitText">{$t(currentStep == BulkAddSteps.Add ? 'project_page.bulk_add_members.submit_button' : 'project_page.bulk_add_members.finish_button')}</span>
  </FormModal>
</AdminContent>
