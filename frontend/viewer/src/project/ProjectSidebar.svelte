<script lang="ts" module>
  export type View = 'dashboard' | 'browse' | 'tasks' | 'activity';
</script>
<script lang="ts">
  import * as Sidebar from '$lib/components/ui/sidebar';
  import { Icon } from '$lib/components/ui/icon';
  import type {IconClass} from '../lib/icon-class';
  import {useFwLiteConfig} from '../lib/services/service-provider';
  import ProjectDropdown from './ProjectDropdown.svelte';
  import { t } from 'svelte-i18n-lingui';
  import {onDestroy} from 'svelte';
  import ThemePicker from '$lib/ThemePicker.svelte';
  import {navigate} from 'svelte-routing';
  import NewEntryButton from './NewEntryButton.svelte';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';

  let { projectName, currentView = $bindable() } = $props<{
    projectName: string;
    currentView: View;
  }>();

  const config = useFwLiteConfig();
  let isSynchronizing = $state(false);
  let intervalId = setInterval(() => {
    isSynchronizing = !isSynchronizing;
  }, 2000);

  onDestroy(() => {
    clearInterval(intervalId);
  });

  function handleProjectSelect(selectedProjectName: string) {
    console.log('selectedProjectName', selectedProjectName);
  }

  function handleNewEntry() {
    console.log('handleNewEntry');
  }

  let sidebar: Sidebar.Root | undefined = $state();
</script>

{#snippet ViewButton(view: View, icon: IconClass, label: string)}
  <Sidebar.MenuItem>
    <Sidebar.MenuButton onclick={() => (currentView = view)} isActive={currentView === view}>
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
          {projectName}
          onSelect={handleProjectSelect}
        />
        <div class="flex-1" ></div>
        <ThemePicker />
      </div>
      <div class="mx-auto">
        <NewEntryButton active={!IsMobile.value && sidebar?.isOpen()} onclick={handleNewEntry} />
      </div>
    </div>
  </Sidebar.Header>
  <Sidebar.Content>
    <Sidebar.Group>
      <Sidebar.GroupLabel>{$t`Dictionary`}</Sidebar.GroupLabel>
      <Sidebar.GroupContent>
        <Sidebar.Menu>
          {@render ViewButton('dashboard', 'i-mdi-view-dashboard', $t`Dashboard`)}
          {@render ViewButton('browse', 'i-mdi-book-alphabet', $t`Browse`)}
          {@render ViewButton('tasks', 'i-mdi-checkbox-marked', $t`Tasks`)}
          {@render ViewButton('activity', 'i-mdi-chart-line', $t`Activity`)}
        </Sidebar.Menu>
      </Sidebar.GroupContent>
    </Sidebar.Group>
    <div class="grow"></div>
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
        </Sidebar.Menu>
      </Sidebar.GroupContent>
    </Sidebar.Group>

    <Sidebar.Group>
      <Sidebar.Menu>
        <Sidebar.MenuItem>
          <Sidebar.MenuButton>
            <Icon icon="i-mdi-cog" />
            <span>{$t`Settings`}</span>
          </Sidebar.MenuButton>
        </Sidebar.MenuItem>
      </Sidebar.Menu>
    </Sidebar.Group>
    <Sidebar.Group>
      <Sidebar.Menu>
        <Sidebar.MenuItem>
          <Sidebar.MenuButton onclick={() => navigate('/')}>
            <Icon icon="i-mdi-close" />
            <span>{$t`Close Dictionary`}</span>
          </Sidebar.MenuButton>
        </Sidebar.MenuItem>
        <Sidebar.MenuItem>
          <Sidebar.MenuButton>
            <Icon icon="i-mdi-help-circle" />
            <span>{$t`Troubleshoot`}</span>
          </Sidebar.MenuButton>
        </Sidebar.MenuItem>
        <Sidebar.MenuItem>
          <Sidebar.MenuButton>
            <Icon icon="i-mdi-message" />
            <span>{$t`Feedback`}</span>
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
