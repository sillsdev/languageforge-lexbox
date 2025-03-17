<script lang="ts" module>
  export type View = 'dashboard' | 'browse' | 'tasks' | 'activity';
</script>
<script lang="ts">
  import * as Sidebar from '$lib/components/ui/sidebar';
  import * as Resizable from '$lib/components/ui/resizable';
  import { Icon } from '$lib/components/ui/icon';
  import { Button } from '$lib/components/ui/button';
  import type {IconClass} from './icon-class';

  let { projectName, currentView = $bindable() } = $props<{
    projectName: string;
    currentView: View;
  }>();
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
        <Button variant="default" size="sm" class="px-3 max-w-72" icon="i-mdi-plus">Create Entry</Button>
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
