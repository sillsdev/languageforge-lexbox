<script lang="ts">

  import {mdiBookArrowUpOutline, mdiBookSyncOutline} from '@mdi/js';
  import {Button, SelectField} from 'svelte-ux';
  import {writable} from 'svelte/store';
  import {type ServerStatus, useProjectsService} from './services/projects-service';
  import {getContext} from 'svelte';

  const projectsService = useProjectsService();
  let projectName = getContext<string>('project-name');
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
    $projectServer = $servers[0].authority;
  }
  $: server = $servers.find((server) => server.authority === $projectServer) ?? {
    displayName: 'Unknown',
    authority: '',
    loggedIn: false
  };
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

{#if $servers.length > 1 && !isUploaded}
  <SelectField
    label="Sync Server"
    disabled={isUploaded}
    options={($servers).map((server) => ({ value: server.authority, label: server.displayName, group: server.displayName }))}
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
    Syncing with {server.displayName}
  </Button>
{/if}
{#if $projectServer && !isUploaded && server.loggedIn}
  <Button variant="fill-light" color="primary" loading={uploading} on:click={upload} icon={mdiBookArrowUpOutline}>
    Upload to {server.displayName}
  </Button>
{:else if $projectServer && !isUploaded && !server.loggedIn}
  <!--todo after login we are sent home, should be sent back to the current project-->
  <Button variant="fill" href="/api/auth/login/{server.authority}">Login</Button>
{/if}
