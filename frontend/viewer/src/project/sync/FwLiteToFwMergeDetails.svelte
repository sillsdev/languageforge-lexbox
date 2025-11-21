<script lang="ts">
  import { ProjectSyncStatusEnum } from '$lib/dotnet-types/generated-types/LexCore/Sync/ProjectSyncStatusEnum';
  import { Button } from '$lib/components/ui/button';
  import { Icon } from '$lib/components/ui/icon';
  import type { IProjectSyncStatus } from '$lib/dotnet-types/generated-types/LexCore/Sync/IProjectSyncStatus';
  import { plural, t, T } from 'svelte-i18n-lingui';
  import * as Accordion from '$lib/components/ui/accordion';
  import SyncArrow from './SyncArrow.svelte';
  import flexLogo from '$lib/assets/flex-logo.png';
  import logoLight from '$lib/assets/logo-light.svg';
  import { FormatRelativeDate } from '$lib/components/ui/format';
  import { SyncStatus } from '$lib/dotnet-types/generated-types/LexCore/Sync/SyncStatus';
  import LoginButton from '$lib/auth/LoginButton.svelte';
  import type { ILexboxServer } from '$lib/dotnet-types';
  import * as Popover from '$lib/components/ui/popover';
  import {cn} from '$lib/utils';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';

  let {
    syncStatus,
    remoteStatus,
    server,
    loadingSyncLexboxToFlex = $bindable(false),
    loadingSyncLexboxToLocal,
    canSyncLexboxToFlex,
    syncLexboxToFlex = async () => {},
    onLoginStatusChange = () => {},
  }: {
    syncStatus: SyncStatus;
    remoteStatus?: IProjectSyncStatus;
    server?: ILexboxServer;
    loadingSyncLexboxToFlex: boolean;
    loadingSyncLexboxToLocal: boolean;
    canSyncLexboxToFlex?: boolean;
    syncLexboxToFlex?: () => Promise<void>;
    onLoginStatusChange?: (status: 'logged-in' | 'logged-out') => void;
  } = $props();
  const lastFlexSyncDate = $derived(
    remoteStatus?.lastMercurialCommitDate ? new Date(remoteStatus.lastMercurialCommitDate) : undefined,
  );
  const lastFwLiteSyncDate = $derived(
    remoteStatus?.lastCrdtCommitDate ? new Date(remoteStatus.lastCrdtCommitDate) : undefined,
  );
  let crdtToHgCount = $derived(remoteStatus?.pendingCrdtChanges ?? 0);
  let hgToCrdtCount = $derived(remoteStatus?.pendingMercurialChanges ?? 0);

  function countToMessage(count: number | undefined) {
    if (count === 0) return $t`No new data`;
    return $t`New data`;
  }

  let loading = $derived(remoteStatus === undefined);

  function onSyncLexboxToFlex() {
    loadingSyncLexboxToFlex = true;
    void syncLexboxToFlex().finally(() => {
      loadingSyncLexboxToFlex = false;
    });
  }
</script>

<p class="mb-6 text-lg text-center">
  <Popover.Root>
    <Popover.InfoTrigger>
      {$t`Sync FieldWorks Lite and FieldWorks Classic`}
    </Popover.InfoTrigger>
    <Popover.Content class="max-w-md text-sm">
      <div class="text-sm space-y-2">
        <p>
          {$t`This will synchronize the FieldWorks Lite and FieldWorks Classic copies of your project in Lexbox.`}
        </p>
        <p>
          {$t`FieldWorks Lite users will automatically receive changes that were made in FieldWorks Classic. FieldWorks Classic users will see changes that were made in FieldWorks Lite after they do Send/Receive.`}
        </p>
      </div>
    </Popover.Content>
  </Popover.Root>
</p>

<div class="grid grid-cols-[1fr_auto_1fr] gap-4">
  <div class="border rounded flex flex-col items-center justify-center text-center p-2">
    <Icon src={logoLight} class="size-10 mb-1" alt={$t`FieldWorks logo`} />
    <p><span class="max-xs:hidden">FieldWorks</span> Lite</p>
    <span class="text-sm text-foreground/80">
      <T msg="Last change: #">
        <FormatRelativeDate
          date={lastFwLiteSyncDate}
          showActualDate
          maxUnits={IsMobile.value ? 1 : 2}
          {loading}
          defaultValue={remoteStatus?.status === ProjectSyncStatusEnum.NeverSynced ? $t`Never` : $t`Unknown`}
        />
      </T>
    </span>
  </div>
  <div class="flex flex-col items-center justify-center gap-2">
    {#if syncStatus === SyncStatus.Offline}
      <Icon icon="i-mdi-cloud-off-outline" class="size-10 m-4" />
    {:else if syncStatus === SyncStatus.NotLoggedIn && server}
      <LoginButton
        text={$t`Login`}
        status={{ loggedIn: false, server: server }}
        statusChange={(s) => onLoginStatusChange(s)}
      />
    {:else}
      <span class="leading-tight" class:loading-text={loading} class:font-semibold={hgToCrdtCount} class:text-primary={hgToCrdtCount}>
        <Popover.Root>
          <Popover.InfoTrigger>
            {countToMessage(hgToCrdtCount)}
          </Popover.InfoTrigger>
          <Popover.Content class="max-w-full max-h-[40vh] overflow-y-auto text-sm border-primary space-y-2">
            <span class="text-primary font-semibold text-base">{$plural(hgToCrdtCount, {
              one: '# new FieldWorks Classic commit',
              other: '# new FieldWorks Classic commits',
            })}</span>
            <p>
              {$t`The number of FieldWorks Classic commits will not necessarily match the number of changes shown in the sync result message.`}
            </p>
            <Accordion.Root type="single">
              <Accordion.Item class="border-none">
                <Accordion.Trigger class="justify-start gap-2 pt-2 pb-2">
                  {$t`Why is that?`}
                </Accordion.Trigger>
                <Accordion.Content>
                  <div class="flex flex-col gap-2 text-balance">
                    <span>
                      {$t`One FieldWorks Classic commit may consist of changes to multiple entries or fields. On the other hand, a commit may only affect data that is not synced to FieldWorks Lite.`}
                    </span>
                    <span>
                      {$t`Changes can also be the result of fields being added to new versions of FieldWorks Lite.`}
                    </span>
                  </div>
                </Accordion.Content>
              </Accordion.Item>
            </Accordion.Root>
          </Popover.Content>
        </Popover.Root>
      </span>
      <SyncArrow dir="left" class={cn(hgToCrdtCount && 'text-primary')} tailLength={120} size={1.25} />
      <Button
        loading={loadingSyncLexboxToFlex}
        disabled={loadingSyncLexboxToLocal || !canSyncLexboxToFlex || !remoteStatus}
        onclick={onSyncLexboxToFlex}
        class="my-1"
        icon="i-mdi-sync"
        iconProps={{ class: 'size-5' }}
      >
        {$t`Sync`}
      </Button>
      <SyncArrow dir="right" class={cn(crdtToHgCount && 'text-primary')} tailLength={120} size={1.25} />
      <span class="leading-tight" class:loading-text={loading} class:font-semibold={crdtToHgCount} class:text-primary={crdtToHgCount}>

        <Popover.Root>
          <Popover.InfoTrigger>
            {countToMessage(crdtToHgCount)}
          </Popover.InfoTrigger>
          <Popover.Content class="max-w-full max-h-[40vh] overflow-y-auto text-sm border-primary space-y-2">
            <span class="text-primary font-semibold text-base">{$plural(crdtToHgCount, {
              one: '# new FieldWorks Lite commit',
              other: '# new FieldWorks Lite commits',
            })}</span>
            <p>
              {$t`The number of FieldWorks Lite commits will not necessarily match the number of changes shown in the sync result message.`}
            </p>
            <Accordion.Root type="single">
              <Accordion.Item class="border-none">
                <Accordion.Trigger class="justify-start gap-2 pt-2 pb-2">
                  {$t`Why is that?`}
                </Accordion.Trigger>
                <Accordion.Content>
                  <div class="flex flex-col gap-2 text-balance">
                    <span>
                      {$t`Changing the same field twice, for example, can result in two commits but only one change that needs to be applied to FieldWorks Classic. Commits can also consist of multiple changes.`}
                    </span>
                  </div>
                </Accordion.Content>
              </Accordion.Item>
            </Accordion.Root>
          </Popover.Content>
        </Popover.Root>
      </span>
    {/if}
  </div>
  <div class="border rounded flex flex-col items-center justify-center text-center p-2">
    <Icon src={flexLogo} class="size-10 mb-1" alt={$t`FieldWorks logo`} />
    <p><span class="max-xs:hidden">FieldWorks </span> Classic</p>
    <span class="text-sm text-foreground/80">
      <T msg="Last change: #">
        <FormatRelativeDate
          date={lastFlexSyncDate}
          showActualDate
          maxUnits={IsMobile.value ? 1 : 2}
          {loading}
          defaultValue={remoteStatus?.status === ProjectSyncStatusEnum.NeverSynced ? $t`Never` : $t`Unknown`}
        />
      </T>
    </span>
  </div>
</div>
