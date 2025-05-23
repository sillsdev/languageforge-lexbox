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

  const config = useFwLiteConfig();
  let isSynchronizing = $state(false);

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
</script>

{#snippet ViewButton(view: View, icon: IconClass, label: string)}
  <Sidebar.MenuItem>
    <Sidebar.MenuButton onclick={() => goto(view)} isActive={$activeRoute?.uri.endsWith(view)}>
      <Icon {icon} />
      <span>{label}</span>
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
          {@render ViewButton('browse', 'i-mdi-book-alphabet', $t`Browse`)}
          <DevContent>
            {@render ViewButton('tasks', 'i-mdi-checkbox-marked', $t`Tasks`)}
          </DevContent>
          {@render ViewButton('activity', 'i-mdi-chart-line', $t`Activity`)}
        </Sidebar.Menu>
      </Sidebar.GroupContent>
    </Sidebar.Group>
    <div class="grow"></div>
    <DevContent>
      <Sidebar.Group>
        <Sidebar.GroupContent>
          <Sidebar.Menu>
            <Sidebar.MenuItem>
              <Sidebar.MenuButton class="justify-between">
                <div class="flex items-center gap-2">
                  <Icon icon="i-mdi-sync" />
                  <span>{$t`Synchronize`}</span>
                </div>
                <div
                  class="size-2 rounded-full"
                  class:bg-red-500={isSynchronizing}
                  class:bg-green-500={!isSynchronizing}
                ></div>
              </Sidebar.MenuButton>
            </Sidebar.MenuItem>
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
          </Sidebar.Menu>
        </Sidebar.GroupContent>
      </Sidebar.Group>
    </DevContent>

    <Sidebar.Group>
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
