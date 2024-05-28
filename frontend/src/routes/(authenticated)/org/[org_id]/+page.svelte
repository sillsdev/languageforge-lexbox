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
  import { _changeOrgName, type OrgSearchParams } from './+page';
  // import AddOrgMember from './AddOrgMember.svelte'; // TODO: Implement appropriately via OrgMemberTable
  import OrgTabs, { type OrgTabId } from './OrgTabs.svelte';
  import { getSearchParams, queryParam } from '$lib/util/query-params';
  import OrgMemberTable from '$lib/components/Orgs/OrgMemberTable.svelte';

  // TODO: Use org.description instead... once orgs *have* descriptions, that is. Or remove if we decide orgs won't have descriptions
  export let description = 'Fake description since orgs don\'t currently have descriptions';

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

  let loadingExtraButton = false;

  $: canManage = user.isAdmin || org.members.find(m => m.user?.id === user.id && m.role === OrgRole.Admin)

  const { notifySuccess/*, notifyWarning*/ } = useNotifications();

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
  <OrgTabs bind:activeTab={$queryParamValues.tab}>
    <svelte:fragment slot="project-badges">
      <Badge>3</Badge> <!-- TODO: Actual project count -->
    </svelte:fragment>
    <svelte:fragment slot="member-badges">
      <Badge>5</Badge> <!-- TODO: Actual member count -->
    </svelte:fragment>
  </OrgTabs>
  {#if $queryParamValues.tab === 'projects'}
  Projects list will go here
  {:else if $queryParamValues.tab === 'members'}
  <OrgMemberTable shownUsers={org.members} {canManage} />
  {:else if $queryParamValues.tab === 'settings'}
  Settings not implemented yet
  {:else if $queryParamValues.tab === 'history'}
  History view not implemented yet
  {/if}
</DetailsPage>

