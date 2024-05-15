<script lang="ts">
  import { DetailsPage, DetailItem } from '$lib/layout';

  import t from '$lib/i18n';
  import { z } from 'zod';
  import EditableText from '$lib/components/EditableText.svelte';
  import type { ErrorMessage } from '$lib/forms';
  import { Badge } from '$lib/components/Badges';
  import IconButton from '$lib/components/IconButton.svelte';
  import type { PageData } from './$types';
  import { OrgRole } from '$lib/gql/types';
  import { useNotifications } from '$lib/notify';
  import { _changeOrgName } from './+page';
  import MembersList from '$lib/components/MembersList.svelte';
  import ChangeMemberRoleModal from '../../project/[project_code]/ChangeMemberRoleModal.svelte';
  import { DialogResponse } from '$lib/components/modals';

  // TODO: Use org.description instead... once orgs *have* descriptions, that is. Or remove if we decide orgs won't have descriptions
  export let description = 'Fake description since orgs don\'t currently have descriptions';

  export let data: PageData;
  $: user = data.user;
  let orgStore = data.org;
  $: org = $orgStore;
  type Member = typeof org.members[number];

  let loadingExtraButton = false;

  $: canManage = user?.isAdmin || org?.members?.find((m) => m.user?.id == user.id)?.role == OrgRole.Admin;

  const { notifySuccess/*, notifyWarning*/ } = useNotifications();

  const orgNameValidation = z.string().trim().min(1, $t('project_page.project_name_empty_error'));
  // TODO: const orgNameValidation = z.string().trim().min(1, $t('org_page.org_name_empty_error'));

  async function updateOrgName(newName: string): Promise<ErrorMessage> {
    // TODO: Eventually this will look something like the following:
    const result = await _changeOrgName({ orgId: org.id, name: newName });
    if (result.error) {
      return result.error.message;
    }
    notifySuccess($t('project_page.notifications.rename_project', { name: newName }));
    // TODO: notifySuccess($t('org_page.notifications.rename_org', { name: newName }));
  }

  // TODO: Duplicated with project page.
  // Move inside MembersList now that ChangeMemberRoleModal is generic (handles both org and project roles)
  let changeMemberRoleModal: ChangeMemberRoleModal;
  async function changeMemberRole(member: Member): Promise<void> {
    const { response } = await changeMemberRoleModal.open({
      userId: member.user.id,
      name: member.user?.name ?? member.user?.email ?? '',
      role: member.role,
    });

    if (response === DialogResponse.Submit) {
      notifySuccess(
        $t('project_page.notifications.role_change', { // TODO: org_page.notifications.role_change
          name: member.user?.name ?? member.user?.email ?? '',
          role: member.role.toLowerCase(),
        }),
      );
    }
  }

</script>

<DetailsPage wide title={org.name}>
  <svelte:fragment slot="actions">
    Action buttons will go here
  </svelte:fragment>
  <svelte:fragment slot="title">
    <div class="max-w-full flex items-baseline flex-wrap">
      <span class="mr-2">{$t('project_page.project')}:</span>
      <span class="text-primary max-w-full">
        <EditableText
          disabled={!canManage}
          value={org.name}
          validation={orgNameValidation}
          saveHandler={updateOrgName}
        />
      </span>
    </div>
  </svelte:fragment>
  <svelte:fragment slot="badges">
    <Badge>
      123 members
    </Badge>
    <Badge>
      45 projects
    </Badge>
    <Badge>
      Any other badges?
    </Badge>
  </svelte:fragment>
  <DetailItem title="project_page.created_at" text="example with copy button" copyToClipboard={true} />
  <DetailItem title="project.table.last_change" text="example with refresh button">
    <IconButton
    slot="extraButton"
    loading={loadingExtraButton}
    icon="i-mdi-refresh"
    size="btn-sm"
    variant="btn-ghost"
    outline={false}
    on:click={() => alert('Button clicked')}
  />
  </DetailItem>
  <DetailItem
    title="project_page.description"
    text={description}
    editable={true}
    placeholder="desc goes here"
    saveHandler={(newval) => { description = newval; return Promise.resolve(undefined); }}
    disabled={false}
    multiline={true}
    />
  <MembersList
    members={org.members}
    canManageMember={(member) => member?.role === OrgRole.Admin || user.isAdmin}
    on:changeMemberRole={(event) => changeMemberRole(event.detail)}
    />
    <ChangeMemberRoleModal projectId={org.id} bind:this={changeMemberRoleModal} />
</DetailsPage>

