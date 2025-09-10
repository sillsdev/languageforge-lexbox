<script module lang="ts">
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import Server from '../../home/Server.svelte';

  const {Story} = defineMeta({});
</script>
<script lang="ts">
  import type {IProjectModel, IServerStatus} from '$lib/dotnet-types';
  import {ProjectRole} from '$lib/dotnet-types/generated-types/LexCore/Entities/ProjectRole';
  import {Router} from 'svelte-routing';

  const status: IServerStatus = {
    server: {
      id: 'test-server',
      displayName: 'Test Server',
      authority: 'https://test-server.com',
    },
    loggedIn: false,
    loggedInAs: 'test-user',
    displayName: 'Test Server',
  };

  const projects: IProjectModel[] = [
    {
      name: 'Test Project',
      code: 'test-project',
      crdt: true,
      fwdata: false,
      lexbox: true,
      server: {
        id: 'test-server',
        displayName: 'Test Server',
        authority: 'https://test-server.com',
      },
      id: 'test-project-id',
      role: ProjectRole.Editor,
    },
    {
      name: 'Test Project 2',
      code: 'test-project-2',
      crdt: false,
      fwdata: false,
      lexbox: true,
      server: {
        id: 'test-server',
        displayName: 'Test Server',
        authority: 'https://test-server.com',
      },
      id: 'test-project-2-id',
      role: ProjectRole.Editor,
    },
    {
      name: 'Test Project 3',
      code: 'test-project-3',
      crdt: true,
      fwdata: false,
      lexbox: true,
      server: {
        id: 'test-server',
        displayName: 'Test Server',
        authority: 'https://test-server.com',
      },
      id: 'test-project-3-id',
      role: ProjectRole.Editor,
    },
  ];
</script>

  <Story name="Server">
    {#snippet template()}
      <Router>
        <Server {status} {projects} localProjects={projects}/>
        <Server {status} projects={[]} localProjects={[]} loading/>
      </Router>
    {/snippet}
  </Story>

