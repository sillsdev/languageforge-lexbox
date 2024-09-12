<script lang="ts">
  import {Button, Drawer, SelectField, Switch} from 'svelte-ux';
  import type {LexboxFeatures} from '../config-types';
  import DevContent from './DevContent.svelte';
  import {type View, views} from '../entry-editor/view-data';
  import type {ViewSettings} from '../services/view-service';
  import {generateExternalChanges} from '../debug';
  import {writable} from 'svelte/store';
  import {type ServerStatus, useProjectsService} from '../services/projects-service';
  import {getContext} from 'svelte';
  import {mdiBookArrowUpOutline, mdiBookSyncOutline} from '@mdi/js';

  const projectsService = useProjectsService();
  let projectName = getContext<string>('project-name');
  export let activeView: View;
  export let viewSettings: ViewSettings;
  export let features: LexboxFeatures;
  export let open = false;
  const servers = writable<ServerStatus[]>([], set => {
    projectsService.fetchServers().then(set);
  });
  let isUploaded = false;
  let projectServer = writable<string | null>(null, set => {
    projectsService.getProjectServer(projectName).then(server => {
      isUploaded = !!server;
      return server;
    }).then(set);
  });
  $: if (!$projectServer && $servers.length > 0) {
    $projectServer = $servers[0].displayName;
  }
  let uploading = false;

  async function upload() {
    if (!$projectServer) return;
    uploading = true;
    //todo if not logged in then login
    await projectsService.uploadCrdtProject($projectServer, projectName);
    uploading = false;
    isUploaded = true;
  }
</script>

<Drawer bind:open placement="right" classes={{ root: 'w-[400px] max-w-full' }}>
  <div class="flex flex-col h-full gap-4 px-6 py-4 w-full font-semibold">
    <SelectField
      label="Fields"
      options={views.map((view) => ({ value: view, label: view.label, group: view.label }))}
      bind:value={activeView}
      classes={{root: 'view-select w-auto', options: 'view-select-options'}}
      clearable={false}
      labelPlacement="top"
      clearSearchOnOpen={false}
      fieldActions={(elem) => /* a hack to disable typing/filtering */ {elem.readOnly = true; return [];}}
      search={() => /* a hack to always show all options */ Promise.resolve()}>
    </SelectField>
    <!-- svelte-ignore a11y-label-has-associated-control -->
    <label class="flex gap-2 items-center text-sm h-10">
      <Switch bind:checked={viewSettings.hideEmptyFields}
              color="neutral"/>
      Hide empty fields
    </label>
    {#if $servers.length > 1}
      <SelectField
      label="Sync Server"
      disabled={isUploaded}
      options={($servers).map((server) => ({ value: server, label: server.displayName, group: server.displayName }))}
      bind:value={$projectServer}
      classes={{root: 'view-select w-auto', options: 'view-select-options'}}
      clearable={false}
      labelPlacement="top"
      clearSearchOnOpen={false}
      fieldActions={(elem) => /* a hack to disable typing/filtering */ {elem.readOnly = true; return [];}}
      search={() => /* a hack to always show all options */ Promise.resolve()}>
    </SelectField>
    {:else if isUploaded}
      <Button disabled color="success" icon={mdiBookSyncOutline} size="md">
        Syncing with {$projectServer}
      </Button>
    {/if}
    {#if $projectServer && !isUploaded}
      <Button variant="fill-light" color="primary" loading={uploading} on:click={upload} icon={mdiBookArrowUpOutline}>Upload
        to {$projectServer}</Button>
    {/if}

    <div class="grow"></div>
    <DevContent>
      <div class="flex flex-col gap-4">
        Debug
        <!-- svelte-ignore a11y-label-has-associated-control -->
        <label class="flex gap-2 items-center text-sm h-10 text-warning">
          <Switch bind:checked={features.write}/>
          Write
        </label>
        <!-- svelte-ignore a11y-label-has-associated-control -->
        <label class="flex gap-2 items-center text-sm h-10 text-warning">
          <Switch bind:checked={features.history}/>
          History
        </label>
        <!-- svelte-ignore a11y-label-has-associated-control -->
        <label class="flex gap-2 items-center text-sm h-10 text-warning">
          <Switch bind:checked={$generateExternalChanges}
                  color="warning"/>
          Simulate conflicting changes
        </label>
      </div>
    </DevContent>
  </div>
</Drawer>

<style lang="postcss">
  :global(.view-select input) {
    cursor: pointer;
  }

  /* We set the group, because the SelectField started breaking when using objects as option values
  (because there's an #each keyed on <group-value.Tostring()> = <undefined-[Object object]> = duplicates).
  So, having a group fixes things :(.*/
  :global(.view-select-options .group-header) {
    display: none;
  }
</style>
