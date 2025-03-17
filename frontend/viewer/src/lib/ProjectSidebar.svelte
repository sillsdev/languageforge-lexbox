<script lang="ts">
  import * as Sidebar from '$lib/components/ui/sidebar';
  import * as Resizable from '$lib/components/ui/resizable';
  import { Icon } from '$lib/components/ui/icon';
  import { Button } from '$lib/components/ui/button';

  export let projectName: string;

</script>

<Resizable.Pane>
  <Sidebar.Root collapsible="none">
    <Sidebar.Header>
      <div class="flex flex-col gap-2">
        <div class="flex items-center gap-2">
          <Icon icon="i-mdi-book" class="size-6" />
          <span class="font-semibold">{projectName}</span>
        </div>
        <Button variant="default" size="sm" class="px-3 max-w-72" icon="i-mdi-plus">
          Create Entry
        </Button>
      </div>
    </Sidebar.Header>
    <Sidebar.Content>
      <Sidebar.Group>
        <Sidebar.Menu>
          <Sidebar.MenuItem>
            <Sidebar.MenuButton>
              <Icon icon="i-mdi-book-open" />
              <span>Dictionary</span>
            </Sidebar.MenuButton>
          </Sidebar.MenuItem>
          <Sidebar.MenuItem>
            <Sidebar.MenuButton>
              <Icon icon="i-mdi-view-dashboard" />
              <span>Dashboard</span>
            </Sidebar.MenuButton>
          </Sidebar.MenuItem>
          <Sidebar.MenuItem>
            <Sidebar.MenuButton>
              <Icon icon="i-mdi-folder-open" />
              <span>Browse</span>
            </Sidebar.MenuButton>
          </Sidebar.MenuItem>
          <Sidebar.MenuItem>
            <Sidebar.MenuButton>
              <Icon icon="i-mdi-checkbox-marked" />
              <span>Tasks</span>
            </Sidebar.MenuButton>
          </Sidebar.MenuItem>
          <Sidebar.MenuItem>
            <Sidebar.MenuButton>
              <Icon icon="i-mdi-chart-line" />
              <span>Activity</span>
            </Sidebar.MenuButton>
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
