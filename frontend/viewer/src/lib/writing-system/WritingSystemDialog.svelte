<script lang="ts">
  import WritingSystemEditor from '$lib/writing-system/WritingSystemEditor.svelte';
  import {useWritingSystemService} from '$project/data';
  import {type IWritingSystem, WritingSystemType} from '$lib/dotnet-types';
  import {defaultWritingSystem} from '$lib/utils';
  import {Button} from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import Select from '$lib/components/field-editors/select.svelte';
  import {Label} from '$lib/components/ui/label';

  interface Props {
    open?: boolean;
  }

  let { open = $bindable(false) }: Props = $props();

  const writingSystemService = useWritingSystemService();
  let vernacular = writingSystemService.vernacular;
  let analysis = writingSystemService.analysis;
  let newVernacular = defaultWritingSystem(WritingSystemType.Vernacular);
  let newAnalysis = defaultWritingSystem(WritingSystemType.Analysis);
  let options: Array<{ value: IWritingSystem, label: string, group: 'Vernacular' | 'Analysis' }> = $derived([
      ...vernacular.map(ws => mapOption(ws)),
      {value: newVernacular, label: 'New Vernacular Writing System', group: 'Vernacular'},
      ...analysis.map(ws => mapOption(ws)),
      {value: newAnalysis, label: 'New Analysis Writing System', group: 'Analysis'},
    ]);

  function mapOption(ws: IWritingSystem): {
    value: IWritingSystem,
    label: string,
    group: 'Vernacular' | 'Analysis'
  } {
    return {value: ws, label: `${ws.name} (${ws.wsId})`, group: ws.type === WritingSystemType.Vernacular ? 'Vernacular' : 'Analysis'};
  }

  let selectedOption: IWritingSystem = $state(vernacular[0] ?? analysis[0]);

  function onCreate(_writingSystem: IWritingSystem) {
    //todo need a better way to invalidate the writing system service
    location.reload();
  }
  function onChange(_writingSystem: IWritingSystem) {
    //todo need a better way to invalidate the writing system service
    location.reload();
  }
</script>

<Dialog.Root bind:open={open}>
  <Dialog.Content>
    <Dialog.Header>
      <Dialog.Title>Edit Writing Systems</Dialog.Title>
    </Dialog.Header>

    <Label>
      Writing System
      <Select bind:value={selectedOption} idSelector="id" labelSelector={(ws) => `${ws.wsId} - ${ws.name} (${ws.type})`} options={options.map(option => option.value)} />
    </Label>
    <WritingSystemEditor
      writingSystem={selectedOption}
      newWs={selectedOption === newVernacular || selectedOption === newAnalysis}
      oncreate={onCreate}
      onchange={onChange}
    />
    <Dialog.Footer>
      <Button onclick={() => open = false}>Close</Button>
    </Dialog.Footer>
  </Dialog.Content>
</Dialog.Root>
