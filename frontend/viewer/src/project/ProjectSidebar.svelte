<script lang="ts" module>
  export type View = 'dashboard' | 'browse' | 'tasks' | 'activity';
</script>

<script lang="ts">
  import * as Sidebar from '$lib/components/ui/sidebar';
  import { Icon } from '$lib/components/ui/icon';
  import type {IconClass} from '../lib/icon-class';
  import {useFwLiteConfig, useTroubleshootingService} from '../lib/services/service-provider';
  import ProjectDropdown from './ProjectDropdown.svelte';
  import { t } from 'svelte-i18n-lingui';
  import ThemePicker from '$lib/ThemePicker.svelte';
  import {navigate, useRouter} from 'svelte-routing';
  import type {IProjectModel} from '$lib/dotnet-types';
  import {usePrimaryAction} from './SidebarPrimaryAction.svelte';
  import DevContent from '$lib/layout/DevContent.svelte';
  import TroubleshootDialog from '$lib/troubleshoot/TroubleshootDialog.svelte';
  import SyncDialog from './SyncDialog.svelte';
  import {useFeatures} from '$lib/services/feature-service';
  import {useProjectStats} from '$lib/project-stats';
  import {formatNumber} from '$lib/components/ui/format';
  import {useProjectEventBus} from '$lib/services/event-bus';
  import {SyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/SyncStatus';
  import LocalizationPicker from '$lib/i18n/LocalizationPicker.svelte';

  const config = useFwLiteConfig();
  const features = useFeatures();
  const stats = useProjectStats();
  const projectEventBus = useProjectEventBus();
  let syncStatus = $state<SyncStatus>();
  projectEventBus.onSync(e => syncStatus = e.status);

  function handleProjectSelect(selectedProject: IProjectModel) {
    if (selectedProject.fwdata) {
      navigate('/fwdata/' + selectedProject.code);
    } else if (selectedProject.crdt) {
      navigate('/project/' + selectedProject.code);
    }
  }

  let sidebar: Sidebar.Root | undefined = $state();
  const primaryAction = usePrimaryAction();

  const {base, activeRoute} = useRouter();
  function goto(view: string) {
    let newLocation = `${$base.uri}/${view}`;
    navigate(newLocation);
  }

  const supportsTroubleshooting = useTroubleshootingService();
  let troubleshootDialog = $state<TroubleshootDialog>();
  let syncDialog = $state<SyncDialog>();
</script>

{#snippet ViewButton(view: View, icon: IconClass, label: string, stat?: string)}
  <Sidebar.MenuItem>
    <Sidebar.MenuButton onclick={() => goto(view)} isActive={$activeRoute?.uri.endsWith(view)}>
      <Icon {icon} />
      <span>{label}</span>
      {#if stat}
        <span class="text-right grow">{stat}</span>
      {/if}
    </Sidebar.MenuButton>
  </Sidebar.MenuItem>
{/snippet}
<Sidebar.Root variant="inset" bind:this={sidebar}>
  <Sidebar.Header class="relative">
    <div class="flex flex-col gap-2">
      <div class="flex flex-row items-center gap-1">
        <ProjectDropdown
          onSelect={handleProjectSelect}
        />
        <div class="flex-1" ></div>
        <ThemePicker />
      </div>
      <div class="mx-auto">
        {@render primaryAction.snippet?.(sidebar?.isOpen())}
      </div>
    </div>
  </Sidebar.Header>
  <Sidebar.Content>
    <Sidebar.Group>
      <Sidebar.GroupLabel>{$t`Dictionary`}</Sidebar.GroupLabel>
      <Sidebar.GroupContent>
        <Sidebar.Menu>
          <DevContent>
            {@render ViewButton('dashboard', 'i-mdi-view-dashboard', $t`Dashboard`)}
          </DevContent>
          {@render ViewButton('browse', 'i-mdi-book-alphabet', $t`Browse`, formatNumber(stats.current?.totalEntryCount))}
          <DevContent>
            {@render ViewButton('tasks', 'i-mdi-checkbox-marked', $t`Tasks`)}
          </DevContent>
          {#if features.history}
            {@render ViewButton('activity', 'i-mdi-chart-line', $t`Activity`)}
          {/if}
        </Sidebar.Menu>
      </Sidebar.GroupContent>
    </Sidebar.Group>
    <div class="grow"></div>
      <Sidebar.Group>
        <Sidebar.GroupContent>
          <Sidebar.Menu>
              {#if features.sync}
                <SyncDialog bind:this={syncDialog} {syncStatus} />
                <Sidebar.MenuItem>
                  <Sidebar.MenuButton onclick={() => syncDialog?.open()} class="justify-between">
                    {#snippet tooltipContent()}
                      {#if syncStatus === SyncStatus.Offline}
                        <span>{$t`Offline`}</span>
                      {:else if syncStatus === SyncStatus.NotLoggedIn}
                        <span>{$t`Not logged in`}</span>
                      {:else if syncStatus === SyncStatus.NoServer}
                        <span>{$t`No server configured`}</span>
                      {:else if syncStatus === SyncStatus.UnknownError}
                        <span>{$t`Unknown error`}</span>
                      {:else if syncStatus === SyncStatus.Success}
                        <span>{$t`Synced`}</span>
                      {:else}
                        <span>{$t`Error getting sync status`}</span>
                      {/if}
                    {/snippet}
                    <div class="flex items-center gap-2">
                      <Icon icon="i-mdi-sync"/>
                      <span>{$t`Synchronize`}</span>
                    </div>
                    <div
                      class="size-2 rounded-full"
                      class:bg-red-500={syncStatus !== SyncStatus.Success}
                      class:bg-green-500={syncStatus === SyncStatus.Success}
                    ></div>
                  </Sidebar.MenuButton>
                </Sidebar.MenuItem>
              {/if}
            <DevContent>
              <Sidebar.MenuItem>
                <Sidebar.MenuButton>
                  <Icon icon="i-mdi-account" />
                  <span>{$t`Account`}</span>
                </Sidebar.MenuButton>
              </Sidebar.MenuItem>
              <Sidebar.MenuItem>
                <Sidebar.MenuButton>
                  <Icon icon="i-mdi-cog" />
                  <span>{$t`Settings`}</span>
                </Sidebar.MenuButton>
              </Sidebar.MenuItem>
            </DevContent>
          </Sidebar.Menu>
        </Sidebar.GroupContent>
      </Sidebar.Group>

    <Sidebar.Group class="paratext:hidden">
      <Sidebar.Menu>
        <Sidebar.MenuItem>
          <Sidebar.MenuButton onclick={() => navigate('/')}>
            <Icon icon="i-mdi-close" />
            <span>{$t`Close Dictionary`}</span>
          </Sidebar.MenuButton>
        </Sidebar.MenuItem>
      </Sidebar.Menu>
    </Sidebar.Group>

    <Sidebar.Group>
      <Sidebar.Menu>
        {#if supportsTroubleshooting}
          <TroubleshootDialog bind:this={troubleshootDialog} />
          <Sidebar.MenuItem>
            <Sidebar.MenuButton onclick={() => troubleshootDialog?.open()}>
              <Icon icon="i-mdi-help-circle" />
              <span>{$t`Troubleshoot`}</span>
            </Sidebar.MenuButton>
          </Sidebar.MenuItem>
        {/if}
        <Sidebar.MenuItem>
          <Sidebar.MenuButton>
            {#snippet child({ props })}
              <a {...props} href={config.feedbackUrl} target="_blank">
                <Icon icon="i-mdi-message" />
                <span>{$t`Feedback`}</span>
              </a>
            {/snippet}
          </Sidebar.MenuButton>
        </Sidebar.MenuItem>
        <Sidebar.MenuItem>
          <LocalizationPicker/>
        </Sidebar.MenuItem>
      </Sidebar.Menu>
    </Sidebar.Group>
  </Sidebar.Content>
  <Sidebar.Footer>
      <div class="text-xs text-muted-foreground py-2 m-auto">
        <div>{$t`Version ${config.appVersion}`}</div>
        <div>{$t`Made with ❤️ from 🇦🇹 🇹🇭 🇺🇸`}</div>
      </div>
  </Sidebar.Footer>
  <Sidebar.Rail></Sidebar.Rail>
</Sidebar.Root>
