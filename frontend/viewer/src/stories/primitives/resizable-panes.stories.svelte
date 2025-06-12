<script module lang="ts">
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import * as Resizable from '$lib/components/ui/resizable';

  // More on how to set up stories at: https://storybook.js.org/docs/writing-stories
  const { Story } = defineMeta({
    title: 'primitives/panes',
    component: Resizable.PaneGroup,
    subcomponents: {
      // eslint-disable-next-line @typescript-eslint/naming-convention
      'Pane': Resizable.Pane,
    },
  });
</script>

<Story name="Resizable Panes" asChild>
  <Resizable.PaneGroup direction="horizontal" class="h-[200px] border rounded-lg">
    <Resizable.Pane defaultSize={20} class="bg-muted p-4">
      Left Pane
    </Resizable.Pane>
    <Resizable.Handle withHandle />
    <Resizable.Pane defaultSize={30} class="bg-muted p-4">
      Middle Pane
    </Resizable.Pane>
    <Resizable.Handle withHandle />
    <Resizable.Pane class="bg-muted p-4">
      Right Pane
    </Resizable.Pane>
  </Resizable.PaneGroup>
</Story>
