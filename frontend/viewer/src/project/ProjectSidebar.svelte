<script lang="ts" module>
  export type View = 'dashboard' | 'browse' | 'tasks' | 'activity';
</script>
<script lang="ts">
  import * as Sidebar from '$lib/components/ui/sidebar';
  import * as Resizable from '$lib/components/ui/resizable';
  import { Icon } from '$lib/components/ui/icon';
  import { Button } from '$lib/components/ui/button';
  import type {IconClass} from '../lib/icon-class';
  import {useFwLiteConfig} from '../lib/services/service-provider';

  let { projectName, currentView = $bindable() } = $props<{
    projectName: string;
    currentView: View;
  }>();


  const config = useFwLiteConfig();
  let isSynchronizing = $state(false);
  setInterval(() => {
    isSynchronizing = !isSynchronizing;
  }, 2000);
</script>

{#snippet ViewButton(view: View, icon: IconClass, label: string)}

<Sidebar.MenuSubItem>
  <Sidebar.MenuSubButton onclick={() => (currentView = view)} isActive={currentView === view}>
    <Icon {icon} />
    <span>{label}</span>
  </Sidebar.MenuSubButton>
</Sidebar.MenuSubItem>
{/snippet}

<Resizable.Pane>
  <Sidebar.Root collapsible="none">
    <Sidebar.Header>
      <div class="flex flex-col gap-2">
        <div class="flex items-center gap-2">
          <Icon icon="i-mdi-book" class="size-6" />
          <span class="font-semibold">{projectName}</span>
        </div>
        <Button variant="default" size="sm" class="px-3 max-w-72 m-auto" icon="i-mdi-plus">Create Entry</Button>
      </div>
    </Sidebar.Header>
    <Sidebar.Content>
      <Sidebar.Group>
        <Sidebar.Menu>
          <Sidebar.MenuItem>
            <Sidebar.MenuButton>
              <span>Dictionary</span>
            </Sidebar.MenuButton>
            <Sidebar.MenuSub>
              {@render ViewButton('dashboard', 'i-mdi-view-dashboard', 'Dashboard')}
              {@render ViewButton('browse', 'i-mdi-folder-open', 'Browse')}
              {@render ViewButton('tasks', 'i-mdi-checkbox-marked', 'Tasks')}
              {@render ViewButton('activity', 'i-mdi-chart-line', 'Activity')}
            </Sidebar.MenuSub>
          </Sidebar.MenuItem>
        </Sidebar.Menu>
      </Sidebar.Group>
    </Sidebar.Content>
    <Sidebar.Footer>
      <Sidebar.Group>
        <Sidebar.Menu>
          <Sidebar.MenuItem>
            <Sidebar.MenuButton>
              <div class="flex items-center gap-2">
                <Icon icon="i-mdi-sync" />
                <span>Synchronize</span>
                <div class="size-2 rounded-full mt-1" class:bg-red-500={isSynchronizing} class:bg-green-500={!isSynchronizing}></div>
              </div>
            </Sidebar.MenuButton>
          </Sidebar.MenuItem>
        </Sidebar.Menu>
      </Sidebar.Group>
      <Sidebar.Group>
        <Sidebar.Menu>
          <Sidebar.MenuItem>
            <Sidebar.MenuButton>
              <Icon icon="i-mdi-help-circle" />
              <span>Troubleshoot</span>
            </Sidebar.MenuButton>
          </Sidebar.MenuItem>
          <Sidebar.MenuItem>
            <Sidebar.MenuButton>
              <Icon icon="i-mdi-message" />
              <span>Feedback</span>
            </Sidebar.MenuButton>
          </Sidebar.MenuItem>
        </Sidebar.Menu>
        <div class="text-xs text-muted-foreground py-2 m-auto">
          <div>Version {config.appVersion}</div>
          <div>Made with ❤️ from Thailand</div>
        </div>
      </Sidebar.Group>


      <Sidebar.Group>
        <Sidebar.Menu>
          <Sidebar.MenuItem>
            <Button variant="default" size="sm" class="px-3" icon="i-mdi-close">Close Dictionary</Button>
          </Sidebar.MenuItem>
          <Sidebar.MenuItem>
            <Sidebar.MenuButton>
              <Icon icon="i-mdi-cog" />
              <span>Settings</span>
            </Sidebar.MenuButton>
          </Sidebar.MenuItem>
        </Sidebar.Menu>
      </Sidebar.Group>
    </Sidebar.Footer>
  </Sidebar.Root>
</Resizable.Pane>
<Resizable.Handle withHandle />
