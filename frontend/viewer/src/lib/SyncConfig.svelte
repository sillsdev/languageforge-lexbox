﻿<script lang="ts">

  import {mdiBookArrowUpOutline, mdiBookSyncOutline} from '@mdi/js';
  import {Button, SelectField} from 'svelte-ux';
  import {writable} from 'svelte/store';
  import {type ServerStatus, useProjectsService} from './services/projects-service';
  import {getContext} from 'svelte';
  import {AppNotification} from './notifications/notifications';

  const projectsService = useProjectsService();
  let projectName = getContext<string>('project-name');
  const servers = writable<ServerStatus[]>([], set => {
    projectsService.fetchServers().then(set)
    .catch(error => {
      console.error(`Failed to fetch servers`, error);
      throw error;
    });
  });

  let isUploaded = false;
  let projectServer = writable<string | null>(null, set => {
    projectsService.getProjectServer(projectName).then(server => {
      isUploaded = !!server;
      return server;
    }).then(set)
    .catch(error => {
      console.error(`Failed to get project server for ${projectName}`, error);
      throw error;
    });
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

  let targetProjectId: string | null = null;
  async function serverProjectsForUpload(serverAuthority: string) {
    const remoteProjects = await projectsService.fetchRemoteProjects();
    return remoteProjects[serverAuthority].filter(p => !p.crdt);
  }

  async function upload() {
    if (!$projectServer) return;
    if (!targetProjectId) return;
    uploading = true;
    //todo if not logged in then login
    const success = await projectsService.uploadCrdtProject($projectServer, projectName, targetProjectId);
    uploading = false;
    if (success) isUploaded = true;
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
{:else if isUploaded && server.loggedIn}
  <Button disabled color="success" icon={mdiBookSyncOutline} size="md">
    Syncing with {server.displayName}
  </Button>
{/if}
{#if $projectServer && !isUploaded && server.loggedIn}
  {#await serverProjectsForUpload($projectServer)}
    <div class="loading loading-dots loading-lg"></div>
  {:then projects}
    <SelectField
      label="Target project"
      options={(projects).map((p) => ({ value: p.id, label: p.name, group: p.name }))}
      bind:value={targetProjectId}
      classes={{root: 'view-select w-auto', options: 'view-select-options'}}
      clearable={false}
      labelPlacement="top"
      clearSearchOnOpen={false}
      fieldActions={(elem) => /* a hack to disable typing/filtering */ {elem.readOnly = true; return [];}}
      search={() => /* a hack to always show all options */ Promise.resolve()}>
    </SelectField>
  {/await}
  <Button variant="fill-light" color="primary" disabled={!targetProjectId} loading={uploading} on:click={upload} icon={mdiBookArrowUpOutline}>
    Upload to {server.displayName}
  </Button>
{/if}
{#if server && !server.loggedIn}
  {#if isUploaded}
    <span>Your login has expired to sync with {server.displayName}. Please login again.</span>
  {/if}
  <Button variant="fill" href="/api/auth/login/{server.authority}">Login</Button>
{/if}
