<script lang="ts">
  import { DetailsPage } from '$lib/layout';

  import t from '$lib/i18n';
  import { z } from 'zod';
  import EditableText from '$lib/components/EditableText.svelte';
  import type { ErrorMessage } from '$lib/forms';
  import { Badge } from '$lib/components/Badges';
  import DetailItem from '$lib/layout/DetailItem.svelte';
  import IconButton from '$lib/components/IconButton.svelte';

  export let name = 'No-name Org';
  export let description = 'Describe this org here';

  let loadingExtraButton = false;

  // TODO: Implement canManage logic (org managers only? Site admins also?)
  let canManage = true;

  const orgNameValidation = z.string().trim().min(1, $t('project_page.project_name_empty_error'));

  function updateOrgName(newName: string): Promise<ErrorMessage> {
    // TODO: Eventually this will look something like the following:
    // const result = await _changeOrgName({ orgId: org.id, name: newName });
    // if (result.error) {
    //   return result.error.message;
    // }
    // notifySuccess($t('org_page.notifications.rename_org', { name: newName }));

    // TODO: Implement that, then remove this:
    name = newName;
    return Promise.resolve(undefined);
  }
</script>

<DetailsPage wide title={name}>
  <svelte:fragment slot="actions">
    Action buttons will go here
  </svelte:fragment>
  <svelte:fragment slot="title">
    <div class="max-w-full flex items-baseline flex-wrap">
      <span class="mr-2">{$t('project_page.project')}:</span>
      <span class="text-primary max-w-full">
        <EditableText
          disabled={!canManage}
          value={name}
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
  <DetailItem title="project_page.created_at" text="typos are impossible" copyToClipboard={true} />
  <DetailItem title="project.table.last_change" text="extra button">
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
</DetailsPage>
