<script lang="ts">
  import { BadgeButton } from '$lib/components/Badges';
  import { DialogResponse, FormModal } from '$lib/components/modals';
  import { Input, ProjectRoleSelect, TextArea, passwordFormRules } from '$lib/forms';
  import { ProjectRole } from '$lib/gql/types';
  import t from '$lib/i18n';
  import { z } from 'zod';
  import { _bulkAddProjectMembers } from './+page';
  import { useNotifications } from '$lib/notify';
  import { hash } from '$lib/util/hash';
  import { AdminContent } from '$lib/layout';

  export let projectId: string;
  const schema = z.object({
    usernamesText: z.string().min(1, $t('register.name_missing')),
    password: passwordFormRules($t),
    role: z.enum([ProjectRole.Editor, ProjectRole.Manager]).default(ProjectRole.Editor),
  });

  let formModal: FormModal<typeof schema>;
  $: form = formModal?.form();

  let createdCount: number | undefined = undefined;
  // let usernameConflicts: string[] = [];

  const { notifySuccess } = useNotifications();

  async function openModal(): Promise<void> {
    const { response, formState } = await formModal.open(async () => {
      const passwordHash = await hash($form.password);
      const usernames = $form.usernamesText.split('\n').map(s => s.trim());
      const { error, data } = await _bulkAddProjectMembers({
        projectId,
        passwordHash,
        usernames,
        role: $form.role,
      });

      // TODO: Handle this case
      // if (error?.byType('NotFoundError')) {
      //   return { email: [$t('project_page.bulk_add_members.project_not_found')] };
      // }

      createdCount = data?.bulkAddProjectMembers.bulkAddProjectMembersResult?.createdCount;
      // usernameConflicts = data?.bulkAddProjectMembers.bulkAddProjectMembersResult?.usernameConflicts ?? [];
      return error?.message;
    });
    if (response === DialogResponse.Submit) {
      // const message = userInvited ? 'member_invited' : 'add_member';
      notifySuccess($t(`project_page.notifications.bulk_add_members`, { count: createdCount ?? 0 }));
      // TODO: Display username conflicts somewhere as well
    }
  }
</script>

<AdminContent>
  <BadgeButton type="badge-success" icon="i-mdi-account-plus-outline" on:click={openModal}>
    {$t('project_page.bulk_add_members.add_button')}
  </BadgeButton>

  <FormModal bind:this={formModal} {schema} let:errors>
    <span slot="title">{$t('project_page.bulk_add_members.modal_title')}</span>
    <Input
      id="password"
      type="password"
      label={$t('project_page.bulk_add_members.shared_password')}
      bind:value={$form.password}
      error={errors.password}
    />
    <ProjectRoleSelect bind:value={$form.role} error={errors.role} />
    <TextArea
      id="usernamesText"
      label={$t('project_page.bulk_add_members.usernames')}
      bind:value={$form.usernamesText}
      error={errors.usernamesText}
    />
  <span slot="submitText">{$t('project_page.bulk_add_members.submit_button')}</span>
  </FormModal>
</AdminContent>
