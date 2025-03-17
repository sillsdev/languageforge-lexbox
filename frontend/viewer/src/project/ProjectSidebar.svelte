<script lang="ts" module>
  export type View = 'dashboard' | 'browse' | 'tasks' | 'activity';
</script>
<script lang="ts">
  import * as Sidebar from '$lib/components/ui/sidebar';
  import { Icon } from '$lib/components/ui/icon';
  import { Button } from '$lib/components/ui/button';
  import type {IconClass} from '../lib/icon-class';
  import {useFwLiteConfig} from '../lib/services/service-provider';
  import ProjectDropdown from './ProjectDropdown.svelte';
  import { t } from 'svelte-i18n-lingui';

  let { projectName, currentView = $bindable() } = $props<{
    projectName: string;
    currentView: View;
  }>();

  const config = useFwLiteConfig();
  let isSynchronizing = $state(false);
  setInterval(() => {
    isSynchronizing = !isSynchronizing;
  }, 2000);

  function handleProjectSelect(selectedProjectName: string) {
    console.log('selectedProjectName', selectedProjectName);
  }
</script>

{#snippet ViewButton(view: View, icon: IconClass, label: string)}
  <Sidebar.MenuSubItem>
    <Sidebar.MenuSubButton onclick={() => (currentView = view)} isActive={currentView === view}>
      <Icon {icon} />
      <span>{label}</span>
    </Sidebar.MenuSubButton>
  </Sidebar.MenuSubItem>
{/snippet}

<Sidebar.Root collapsible="icons">
  <Sidebar.Header>
    <div class="flex flex-col gap-2">
      <ProjectDropdown
        {projectName}
        onSelect={handleProjectSelect}
      />
      <Button variant="default" size="sm" class="px-3 max-w-72 m-auto" icon="i-mdi-plus">{$t`Create Entry`}</Button>
    </div>
  </Sidebar.Header>
  <Sidebar.Content>
    <Sidebar.Group>
      <Sidebar.Menu>
        <Sidebar.MenuItem>
          <Sidebar.MenuButton>
            <span>{$t`Dictionary`}</span>
          </Sidebar.MenuButton>
          <Sidebar.MenuSub>
            {@render ViewButton('dashboard', 'i-mdi-view-dashboard', $t`Dashboard`)}
            {@render ViewButton('browse', 'i-mdi-book-alphabet', $t`Browse`)}
            {@render ViewButton('tasks', 'i-mdi-checkbox-marked', $t`Tasks`)}
            {@render ViewButton('activity', 'i-mdi-chart-line', $t`Activity`)}
          </Sidebar.MenuSub>
        </Sidebar.MenuItem>
      </Sidebar.Menu>
    </Sidebar.Group>
  </Sidebar.Content>
  <Sidebar.Footer>
    <Sidebar.Group>
      <Sidebar.Menu>
        <Sidebar.MenuItem>
          <Sidebar.MenuButton class="justify-between">
            <div class="flex items-center gap-2">
              <Icon icon="i-mdi-sync" />
              <span>{$t`Synchronize`}</span>
            </div>
            <div class="size-2 rounded-full" class:bg-red-500={isSynchronizing} class:bg-green-500={!isSynchronizing}></div>
          </Sidebar.MenuButton>
        </Sidebar.MenuItem>
        <Sidebar.MenuItem>
          <Sidebar.MenuButton>
            <Icon icon="i-mdi-account" />
            <span>{$t`Account`}</span>
          </Sidebar.MenuButton>
        </Sidebar.MenuItem>
      </Sidebar.Menu>
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
          <Sidebar.MenuButton>
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
      <div class="text-xs text-muted-foreground py-2 m-auto">
        <div>{$t`Version ${config.appVersion}`}</div>
        <div>{$t`Made with ❤️ from 🇦🇹 🇹🇭 🇺🇸`}</div>
      </div>
    </Sidebar.Group>
  </Sidebar.Footer>
</Sidebar.Root>
