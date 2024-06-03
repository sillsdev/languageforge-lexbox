<script lang="ts">
  import { DetailsPage, DetailItem } from '$lib/layout';

  import t, { date } from '$lib/i18n';
  import { z } from 'zod';
  import EditableText from '$lib/components/EditableText.svelte';
  import { Button, type ErrorMessage } from '$lib/forms';
  import type { PageData } from './$types';
  import { OrgRole } from '$lib/gql/types';
  import { useNotifications } from '$lib/notify';
  import { _changeOrgName, _deleteOrgUser, type OrgSearchParams } from './+page';
  import OrgTabs, { type OrgTabId } from './OrgTabs.svelte';
  import { getSearchParams, queryParam } from '$lib/util/query-params';
  import OrgMemberTable from '$lib/components/Orgs/OrgMemberTable.svelte';
  import { Icon, TrashIcon } from '$lib/icons';

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

  const { notifySuccess } = useNotifications();

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
  <div slot="title" class="max-w-full flex items-baseline flex-wrap">
    <span class="mr-2">{$t('org_page.organization')}:</span>
    <span class="text-primary max-w-full">
      <EditableText
        disabled={!canManage}
        value={org.name}
        validation={orgNameValidation}
        saveHandler={updateOrgName}
      />
    </span>
  </div>
  <div class="mt-6">
    <!-- TODO: Add project count once orgs can own projects -->
    <OrgTabs bind:activeTab={$queryParamValues.tab} memberCount={org.members.length} projectCount={0} />
  </div>
  <div class="py-6 px-2">
    {#if $queryParamValues.tab === 'projects'}
    Projects list will go here once orgs have projects associated with them
    {:else if $queryParamValues.tab === 'members'}
    <OrgMemberTable
      orgId={org.id}
      shownUsers={org.members}
      {canManage}
      on:removeMember={(event) => _deleteOrgUser(org.id, event.detail.email ?? event.detail.username)}
    />
    {:else if $queryParamValues.tab === 'history'}
      <div class="space-y-2">
        <DetailItem title={$t('org_page.details.created_at')} text={$date(org.createdDate)} />
        <DetailItem title={$t('org_page.details.updated_at')} text={$date(org.updatedDate)} />
      </div>
    {:else if $queryParamValues.tab === 'settings'}
      <div class="flex justify-end">
        <Button outline variant="btn-error">
          {$t('org_page.leave_org')}
          <Icon icon="i-mdi-exit-run"/>
        </Button>
      </div>
      {#if canManage}
        <div class="divider" />
        <div class="flex justify-end">
          <Button variant="btn-error">
            {$t('org_page.delete_modal.submit')}
            <TrashIcon/>
          </Button>
        </div>
      {/if}
    {/if}
  </div>
</DetailsPage>
