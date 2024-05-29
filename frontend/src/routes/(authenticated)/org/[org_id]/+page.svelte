<script lang="ts">
  import { DetailsPage, DetailItem } from '$lib/layout';

  import t, { date, number } from '$lib/i18n';
  import { z } from 'zod';
  import EditableText from '$lib/components/EditableText.svelte';
  import type { ErrorMessage } from '$lib/forms';
  import { Badge } from '$lib/components/Badges';
  import type { PageData } from './$types';
  import { OrgRole } from '$lib/gql/types';
  import { useNotifications } from '$lib/notify';
  import { _changeOrgName, _deleteOrgUser, type OrgSearchParams } from './+page';
  import AddOrgMember from './AddOrgMember.svelte';
  import OrgTabs, { type OrgTabId } from './OrgTabs.svelte';
  import { getSearchParams, queryParam } from '$lib/util/query-params';
  import OrgMemberTable from '$lib/components/Orgs/OrgMemberTable.svelte';

  export let data: PageData;
  $: user = data.user;
  let orgStore = data.org;
  $: org = $orgStore;

  const queryParams = getSearchParams<OrgSearchParams>({
    // TODO: Will we want any of the following params that the admin dashboard uses?
    // userSearch: queryParam.string<string>(''),
    // showDeletedProjects: queryParam.boolean<boolean>(false),
    // hideDraftProjects: queryParam.boolean<boolean>(false),
    // confidential: queryParam.string<Confidentiality | undefined>(undefined),
    // projectType: queryParam.string<ProjectType | undefined>(undefined),
    // memberSearch: queryParam.string(undefined),
    // projectSearch: queryParam.string<string>(''),
    tab: queryParam.string<OrgTabId>('projects'),
  });
  const { queryParamValues } = queryParams;

  $: canManage = user.isAdmin || !!org.members.find(m => m.user?.id === user.id && m.role === OrgRole.Admin)

  const { notifySuccess/*, notifyWarning*/ } = useNotifications();

  let addMemberModal: AddOrgMember;

  const orgNameValidation = z.string().trim().min(1, $t('org_page.org_name_empty_error'));

  async function updateOrgName(newName: string): Promise<ErrorMessage> {
    const result = await _changeOrgName({ orgId: org.id, name: newName });
    if (result.error) {
      return result.error.message;
    }
    notifySuccess($t('org_page.notifications.rename_org', { name: newName }));
  }
</script>

<DetailsPage wide title={org.name}>
  <svelte:fragment slot="actions">
    <!-- No action buttons currently -->
  </svelte:fragment>
  <svelte:fragment slot="title">
    <div class="max-w-full flex items-baseline flex-wrap">
      <span class="mr-2">{$t('org_page.organisation')}:</span>
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
      {org.members.length} members
    </Badge>
    <Badge>
      ?? projects
      <!-- Orgs owning projects is not yet implemented -->
    </Badge>
  </svelte:fragment>
  <DetailItem title="org_page.details.created_at" text={$date(org.createdDate)} />
  <DetailItem title="org_page.details.updated_at" text={$date(org.updatedDate)} />
  <DetailItem title="org_page.details.member_count" text={$number(org.members.length)} />
  <OrgTabs bind:activeTab={$queryParamValues.tab}>
    <!-- TODO: Add project count once orgs can own projects -->
    <svelte:fragment slot="project-badges">
      <Badge>??</Badge>
    </svelte:fragment>
    <svelte:fragment slot="member-badges">
      <Badge>{$number(org.members.length)}</Badge>
    </svelte:fragment>
  </OrgTabs>
  {#if $queryParamValues.tab === 'projects'}
  Projects list will go here once orgs have projects associated with them
  {:else if $queryParamValues.tab === 'members'}
  <OrgMemberTable
    orgId={org.id}
    shownUsers={org.members}
    {canManage}
    on:addMember={() => addMemberModal.openModal()}
    on:removeMember={(event) => _deleteOrgUser(org.id, event.detail.email ?? event.detail.username)}
  />
  {:else if $queryParamValues.tab === 'settings'}
  Settings not implemented yet
  {:else if $queryParamValues.tab === 'history'}
  History view not implemented yet
  {/if}
</DetailsPage>

<!-- Or we change AddOrgMember to be just the modal, not the button, and do something like this:
<AddOrgMember orgId={org.id} bind:this={addMemberModal} />
-->
