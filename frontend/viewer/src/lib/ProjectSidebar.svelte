<script lang="ts">
  import * as Sidebar from '$lib/components/ui/sidebar';
  import * as Resizable from '$lib/components/ui/resizable';
  import { Icon } from '$lib/components/ui/icon';
  import { Button } from '$lib/components/ui/button';

  let { projectName, currentView = $bindable() } = $props<{
    projectName: string;
    currentView: 'browse' | 'tasks';
  }>();
</script>

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
              <Sidebar.MenuSubItem>
                <Sidebar.MenuSubButton>
                  <Icon icon="i-mdi-view-dashboard" />
                  <span>Dashboard</span>
                </Sidebar.MenuSubButton>
              </Sidebar.MenuSubItem>
              <Sidebar.MenuSubItem>
                <Sidebar.MenuSubButton onclick={() => (currentView = 'browse')} isActive={currentView === 'browse'}>
                  <Icon icon="i-mdi-folder-open" />
                  <span>Browse</span>
                </Sidebar.MenuSubButton>
              </Sidebar.MenuSubItem>
              <Sidebar.MenuSubItem>
                <Sidebar.MenuSubButton onclick={() => (currentView = 'tasks')} isActive={currentView === 'tasks'}>
                  <Icon icon="i-mdi-checkbox-marked" />
                  <span>Tasks</span>
                </Sidebar.MenuSubButton>
              </Sidebar.MenuSubItem>
              <Sidebar.MenuSubItem>
                <Sidebar.MenuSubButton>
                  <Icon icon="i-mdi-chart-line" />
                  <span>Activity</span>
                </Sidebar.MenuSubButton>
              </Sidebar.MenuSubItem>
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
