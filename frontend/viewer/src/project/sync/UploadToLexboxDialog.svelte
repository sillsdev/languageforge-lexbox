<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Button} from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import Label from '$lib/components/ui/label/label.svelte';
  import Select from '$lib/components/field-editors/select.svelte';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import {useAuthService, useProjectsService} from '$lib/services/service-provider';
  import type {ILexboxServer} from '$lib/dotnet-types';
  import ResponsiveDialog from '$lib/components/responsive-dialog/responsive-dialog.svelte';
  import {Icon} from '$lib/components/ui/icon';
  import {resource} from 'runed';

  interface Props {
    projectCode: string;
    projectId: string;
    projectName: string;
    refresh: () => Promise<void> | void;
  }

  let {projectCode, projectId, projectName, refresh}: Props = $props();

  const projectsService = useProjectsService();
  const authService = useAuthService();

  let open = $state(false);
  let loading = $state(false);
  useBackHandler({addToStack: () => open, onBack: () => open = false, key: 'upload-to-lexbox-dialog'});

  let servers = $state<ILexboxServer[]>([]);
  let selectedServer = $state<ILexboxServer>();
  // The Lexbox project keeps the local id and code, so folder-shared copies of this project still match it.
  // Dev-only feature for now, so the purpose is preselected as "Software Developer" (admins only on Lexbox).
  const createUrl = $derived(selectedServer
    ? `${selectedServer.authority}/project/create?${new URLSearchParams({
      id: projectId, code: projectCode, name: projectName, retentionPolicy: 'DEV',
    }).toString()}`
    : undefined);

  // Whether a project with this id exists on the selected server; undefined while checking
  const exists = resource(() => selectedServer, async (server, _prev, {signal}) => {
    if (!server) return false;
    const serverProjects = await projectsService.serverProjects(server.id, true);
    signal.throwIfAborted();
    return serverProjects?.projects.some(p => p.id === projectId) ?? false;
  }, {lazy: true});

  export function openDialog() {
    loading = false;
    open = true;
    void loadServers();
  }

  async function loadServers() {
    const statuses = await authService.servers();
    servers = statuses.filter(s => s.loggedIn).map(s => s.server);
    selectedServer = servers[0];
    await exists.refetch();
  }

  async function upload() {
    if (!selectedServer) return;
    loading = true;
    try {
      await exists.refetch();
      if (!exists.current) return;
      await projectsService.uploadCrdtProject(selectedServer, projectCode);
      await refresh();
      open = false;
    } finally {
      loading = false;
    }
  }
</script>

<ResponsiveDialog bind:open title={$t`Upload to Lexbox`} contentProps={{
  hideClose: loading,
  interactOutsideBehavior: loading ? 'ignore' : 'close',
  escapeKeydownBehavior: loading ? 'ignore' : 'close',
}}>
  <div class="gap-4 grid grid-cols-[auto_1fr]">
    <Label class="grid grid-cols-subgrid col-span-2 gap-4 items-center">
      {$t`Server`}
      {#if servers.length > 1}
        <Select options={servers} bind:value={selectedServer} idSelector="id" labelSelector="displayName" />
      {:else if selectedServer}
        <span>{selectedServer.displayName}</span>
      {:else}
        <span class="text-muted-foreground">{$t`Not logged in`}</span>
      {/if}
    </Label>
    <Label class="grid grid-cols-subgrid col-span-2 gap-4 items-center">
      {$t`Project ID`}
      <code class="text-sm">{projectId}</code>
    </Label>
    <div class="col-span-2 flex flex-col items-start gap-2">
      {#if !selectedServer}
        <p class="text-sm text-muted-foreground">{$t`Log in to Lexbox first.`}</p>
      {:else if exists.loading}
        <p class="text-sm text-muted-foreground flex items-center gap-2">
          <Icon icon="i-mdi-loading" class="animate-spin" />
          {$t`Checking ${selectedServer.displayName}...`}
        </p>
      {:else if exists.current}
        <p class="text-sm text-muted-foreground flex items-center gap-2">
          <Icon icon="i-mdi-check" class="text-primary" />
          {$t`The project exists on ${selectedServer.displayName}. Ready to upload.`}
        </p>
      {:else}
        <p class="text-sm text-muted-foreground">
          {$t`There is no project with this ID on ${selectedServer.displayName} yet. Create it, then come back here and upload.`}
        </p>
        <Button variant="outline" href={createUrl} target="_blank" rel="noopener" icon="i-mdi-open-in-new" autofocus>
          {$t`Create the project on Lexbox`}
        </Button>
      {/if}
    </div>
  </div>

  <Dialog.DialogFooter>
    <Button onclick={() => open = false} variant="secondary" disabled={loading}>{$t`Cancel`}</Button>
    <Button onclick={upload} disabled={loading || !selectedServer || exists.loading} {loading} icon="i-mdi-cloud-upload-outline">
      {$t`Upload`}
    </Button>
  </Dialog.DialogFooter>
</ResponsiveDialog>
