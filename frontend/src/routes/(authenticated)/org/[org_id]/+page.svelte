<script lang="ts">
  import { DetailsPage } from '$lib/layout';

  import t from '$lib/i18n';
  import { z } from 'zod';
  import EditableText from '$lib/components/EditableText.svelte';
  import type { ErrorMessage } from '$lib/forms';
  import { Badge } from '$lib/components/Badges';

  export let name = 'No-name Org';

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
</DetailsPage>
