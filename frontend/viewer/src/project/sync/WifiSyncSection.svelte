<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {Input} from '$lib/components/ui/input';
  import {Switch} from '$lib/components/ui/switch';
  import {FormatRelativeDate} from '$lib/components/ui/format';
  import {t} from 'svelte-i18n-lingui';
  import {renderSVG} from 'uqr';
  import {onDestroy} from 'svelte';
  import {useSyncStatusService} from '$lib/services/sync-status-service';
  import {usePlatformFeaturesService} from '$lib/services/platform-features-service';
  import type {IHostingView} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/Hosting/IHostingView';
  import type {IHostingOptions} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/Hosting/IHostingOptions';
  import type {IJoinSessionView} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/Hosting/IJoinSessionView';
  import type {INearbyHostView} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/Hosting/INearbyHostView';
  import {JoinSessionState} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/Hosting/JoinSessionState';
  import {JoinState} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/Hosting/JoinState';
  import {joinRejectionMessage, localSyncProblemMessage} from './wifi-sync-messages';
  import type {SyncTarget} from './sync-target';

  interface Props {
    /** Which sync is running, shared with the rest of the dialog so only one runs at a time */
    syncing?: SyncTarget;
    onSynced?: () => void;
  }

  let {syncing = $bindable(), onSynced = () => {}}: Props = $props();

  const api = useSyncStatusService().syncStatusApi;
  const platformFeatures = usePlatformFeaturesService();
  const canHost = $derived(platformFeatures.features.supportsWifiSyncHosting);

  let hosting = $state<IHostingView>();
  let options = $state<IHostingOptions>();
  let join = $state<IJoinSessionView>();
  let nearby = $state<INearbyHostView[]>();
  let address = $state('');
  let busy = $state<'start' | 'search' | 'join' | 'sync'>();
  let hostingError = $state<string>();

  let loaded = $state(false);
  let searching = false;

  const joinState = $derived(join?.state ?? JoinSessionState.NotJoined);
  const joining = $derived(joinState === JoinSessionState.Waiting || joinState === JoinSessionState.Joined);

  async function refresh() {
    const [h, j] = await Promise.all([api.getHosting(), api.getJoinSession()]);
    // the joiner syncs on its own; refresh the dialog's counts when it does
    if (j.lastSync && j.lastSync !== join?.lastSync) onSynced();
    hosting = h ?? undefined;
    join = j;
    loaded = true;
  }

  void refresh();
  const poll = setInterval(() => void refresh(), 1500);
  onDestroy(() => clearInterval(poll));

  $effect(() => {
    if (canHost && !options) void api.getHostingOptions().then(o => (options = o));
  });

  async function startHosting(networkAddress: string | undefined) {
    busy = 'start';
    hostingError = undefined;
    try {
      const result = await api.startHosting(networkAddress);
      if (result.error) hostingError = result.error;
      hosting = result.hosting ?? undefined;
    } finally {
      busy = undefined;
    }
  }

  async function stopHosting() {
    await api.stopHosting();
    hosting = undefined;
  }

  // searching also tells each host this device is nearby, so it's listed on the host before anyone asks to join
  async function search(manual: boolean) {
    if (searching) return;
    searching = true;
    if (manual) busy = 'search';
    try {
      nearby = await api.findNearbyHosts();
    } finally {
      searching = false;
      if (manual) busy = undefined;
    }
  }

  const idle = $derived(loaded && !hosting && !joining);
  $effect(() => {
    if (!idle) return;
    void search(false);
    const searchAgain = setInterval(() => void search(false), 15_000);
    return () => clearInterval(searchAgain);
  });

  async function joinHost(hostAddress: string) {
    busy = 'join';
    try {
      join = await api.joinHost(hostAddress);
    } finally {
      busy = undefined;
    }
  }

  async function syncNow() {
    busy = 'sync';
    syncing = 'nearby';
    try {
      join = await api.syncWithHostNow();
      onSynced();
    } finally {
      busy = undefined;
      syncing = undefined;
    }
  }

  async function leave() {
    join = await api.leaveHost();
  }

  function qrDataUri(text: string): string {
    return 'data:image/svg+xml;utf8,' + encodeURIComponent(renderSVG(text, {border: 2}));
  }
</script>

<div class="flex flex-col items-center gap-3">
  {#if hosting}
    <!-- hosting panel -->
    <div class="w-full border rounded p-3 flex flex-col items-center gap-3">
      {#if !hosting.isThisProject}
        <p class="text-center">{$t`This device is sharing ${hosting.projectName}. Stop sharing it to share this project.`}</p>
      {:else if hosting.wifiQr}
        <p class="text-center">{$t`Scan with a phone camera to join this device's Wi-Fi, or join it in Wi-Fi settings:`}</p>
        <img src={qrDataUri(hosting.wifiQr)} alt={$t`Wi-Fi QR code`} class="size-48 bg-white rounded" />
        <dl class="grid grid-cols-[auto_1fr] gap-x-3 text-sm">
          <dt class="text-foreground/70">{$t`Network`}</dt><dd class="font-mono">{hosting.networkName}</dd>
          <dt class="text-foreground/70">{$t`Password`}</dt><dd class="font-mono">{hosting.networkPassword}</dd>
          <dt class="text-foreground/70">{$t`Address`}</dt><dd class="font-mono">{hosting.address}</dd>
        </dl>
      {:else}
        <p class="text-center">{$t`Other devices on this network can join at`} <span class="font-mono">{hosting.address}</span></p>
      {/if}

      {#if hosting.isThisProject}
        <div class="w-full flex flex-col gap-2">
          {#each hosting.peers as peer (peer.requestId)}
            <div class="flex flex-wrap items-center gap-2 border rounded px-2 py-1">
              <Icon icon="i-mdi-cellphone-link" class="size-5" />
              <span class="font-medium">{peer.displayName}</span>
              {#if peer.versionMismatch}
                <span class="text-warning text-sm" title={peer.appVersion}>⚠ {$t`different app version`}</span>
              {/if}
              <span class="grow"></span>
              {#if peer.state === JoinState.Pending}
                <Button size="sm" onclick={() => void api.approveJoiner(peer.requestId)}>{$t`Accept`}</Button>
                <Button size="sm" variant="ghost" onclick={() => void api.declineJoiner(peer.requestId)}>{$t`Decline`}</Button>
              {:else if peer.lastSync}
                <span class="text-sm text-foreground/70">{$t`Synced`} <FormatRelativeDate date={new Date(peer.lastSync)} live /></span>
              {:else}
                <span class="text-sm text-foreground/70">{$t`Joined`}</span>
              {/if}
            </div>
          {:else}
            <p class="text-center text-sm text-foreground/70">{$t`No one has joined yet.`}</p>
          {/each}
          {#if hosting.nearbyDevices.length}
            <p class="text-sm text-foreground/70">{$t`Also on this network`}</p>
            {#each hosting.nearbyDevices as device (device.address)}
              <div class="flex flex-wrap items-center gap-2 px-2 py-1 text-sm">
                <Icon icon={device.hasApp ? 'i-mdi-cellphone-check' : 'i-mdi-cellphone'} class="size-5" />
                <span class="font-medium">{device.name ?? device.address}</span>
                {#if device.name}
                  <span class="font-mono text-foreground/70">{device.address}</span>
                {/if}
                {#if device.versionMismatch}
                  <span class="text-warning">⚠ {$t`different app version`}</span>
                {/if}
                <span class="grow"></span>
                <span class="text-foreground/70">
                  {device.hasApp ? $t`Hasn't asked to join` : $t`FieldWorks Lite isn't open on it`}
                </span>
              </div>
            {/each}
          {/if}
          <label class="flex items-center gap-2 text-sm self-center">
            <Switch checked={hosting.autoApprove} onCheckedChange={(checked) => void api.setAutoApprove(checked)} />
            {$t`Accept everyone who joins`}
          </label>
        </div>
      {/if}
      <Button variant="outline" icon="i-mdi-stop" onclick={() => void stopHosting()}>{$t`Stop sharing`}</Button>
    </div>
  {:else if joining && join}
    <!-- joined, or waiting to be -->
    <div class="w-full border rounded p-3 flex flex-col items-center gap-2">
      {#if join.state === JoinSessionState.Waiting}
        <p class="text-center">{$t`Waiting for ${join.hostAddress ?? ''} to accept...`}</p>
      {:else}
        <p class="text-center">{$t`Connected to ${join.projectName ?? ''} at ${join.hostAddress ?? ''}`}</p>
        <p class="text-sm text-foreground/70">
          {#if join.lastSync}
            {$t`Last synced`} <FormatRelativeDate date={new Date(join.lastSync)} live />
          {:else}
            {$t`Syncing...`}
          {/if}
        </p>
        {#if join.problem}
          <p class="text-sm text-warning text-center">{localSyncProblemMessage(join.problem)}</p>
        {/if}
        <Button
          variant="outline"
          icon="i-mdi-sync"
          loading={busy === 'sync'}
          disabled={!!syncing && syncing !== 'nearby'}
          onclick={() => void syncNow()}>
          {$t`Sync now`}
        </Button>
      {/if}
      <Button variant="ghost" icon="i-mdi-logout" onclick={() => void leave()}>{$t`Leave`}</Button>
    </div>
  {:else}
    {#if join?.state === JoinSessionState.Rejected}
      <p class="text-sm text-warning text-center">{joinRejectionMessage(join.rejection)}</p>
    {:else if join?.state === JoinSessionState.Ended && join.problem}
      <p class="text-sm text-warning text-center">{localSyncProblemMessage(join.problem)}</p>
    {:else if join?.state === JoinSessionState.Ended}
      <p class="text-sm text-foreground/70 text-center">{$t`The sync session ended.`}</p>
    {/if}

    {#if canHost}
      <div class="flex flex-col items-center gap-2">
        {#if options?.canCreateNetwork}
          <Button icon="i-mdi-wifi-plus" loading={busy === 'start'} onclick={() => void startHosting(undefined)}>
            {$t`Share this project on a new Wi-Fi network`}
          </Button>
        {/if}
        {#each options?.networks ?? [] as network (network.address)}
          <Button variant="outline" size="sm" loading={busy === 'start'} onclick={() => void startHosting(network.address)}>
            {$t`Share on ${network.name} (${network.address})`}
          </Button>
        {/each}
        {#if hostingError}
          <p class="text-sm text-warning text-center">{hostingError}</p>
        {/if}
      </div>
    {/if}

    <div class="w-full flex flex-col items-center gap-2">
      <Button variant="outline" icon="i-mdi-magnify" loading={busy === 'search'} onclick={() => void search(true)}>
        {$t`Find nearby devices`}
      </Button>
      {#if nearby}
        {#each nearby as host (host.address)}
          <div class="w-full flex flex-wrap items-center gap-2 border rounded px-2 py-1">
            <span class="font-medium">{host.projectName}</span>
            <span class="font-mono text-sm text-foreground/70">{host.address}</span>
            {#if !host.isThisProject}
              <span class="text-sm text-warning">{$t`different project`}</span>
            {/if}
            {#if host.versionMismatch}
              <span class="text-sm text-warning">⚠ {$t`different app version`}</span>
            {/if}
            <span class="grow"></span>
            <Button size="sm" disabled={!host.isThisProject} loading={busy === 'join'} onclick={() => void joinHost(host.address)}>
              {$t`Join`}
            </Button>
          </div>
        {:else}
          <p class="text-sm text-foreground/70">{$t`No devices found. Make sure you're on the same Wi-Fi network.`}</p>
        {/each}
      {/if}
      <form class="flex gap-2" onsubmit={(e) => { e.preventDefault(); if (address.trim()) void joinHost(address); }}>
        <Input bind:value={address} placeholder={$t`Address, e.g. 192.168.137.1`} class="w-56" />
        <Button type="submit" variant="outline" loading={busy === 'join'} disabled={!address.trim()}>{$t`Join`}</Button>
      </form>
    </div>
  {/if}
</div>
