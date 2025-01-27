<script lang="ts">
  import {Button, Dialog, SelectField} from 'svelte-ux';
  import WritingSystemEditor from '$lib/writing-system/WritingSystemEditor.svelte';
  import {useWritingSystemService} from '$lib/writing-system-service';
  import {type IWritingSystem, WritingSystemType} from '$lib/dotnet-types';
  import {defaultWritingSystem} from '$lib/utils';

  let open = false;
  let loading = false;
  const writingSystemService = useWritingSystemService();
  let vernacular = writingSystemService.vernacular;
  let analysis = writingSystemService.analysis;
  let options: Array<{ value: IWritingSystem, label: string, group: 'Vernacular' | 'Analysis' }> = [];
  let newVernacular = defaultWritingSystem(WritingSystemType.Vernacular);
  let newAnalysis = defaultWritingSystem(WritingSystemType.Analysis);
  $: options = [
    ...vernacular.map(ws => mapOption(ws)),
    {value: newVernacular, label: 'New Vernacular Writing System', group: 'Vernacular'},
    ...analysis.map(ws => mapOption(ws)),
    {value: newAnalysis, label: 'New Analysis Writing System', group: 'Analysis'},
  ];

  function mapOption(ws: IWritingSystem): {
    value: IWritingSystem,
    label: string,
    group: 'Vernacular' | 'Analysis'
  } {
    return {value: ws, label: `${ws.name} (${ws.wsId})`, group: ws.type === WritingSystemType.Vernacular ? 'Vernacular' : 'Analysis'};
  }

  let selectedOption: IWritingSystem = vernacular[0] ?? analysis[0];

  function onCreate(_writingSystem: IWritingSystem) {
    //todo need a better way to invalidate the writing system service
    location.reload();
  }
  function onChange(_writingSystem: IWritingSystem) {
    //todo need a better way to invalidate the writing system service
    location.reload();
  }
</script>
<Button rounded variant="outline" on:click={() => open = true}>
  Edit WS
</Button>
<Dialog bind:open={open} {loading} persistent={loading} style="height: auto">
  <div slot="title">Edit Writing Systems</div>
  <SelectField label="Writing System" bind:value={selectedOption} {options} clearable={false}/>
  <WritingSystemEditor
    writingSystem={selectedOption}
    newWs={selectedOption === newVernacular || selectedOption === newAnalysis}
    on:create={(e) => onCreate(e.detail.writingSystem)}
  on:change={(e) => onChange(e.detail.writingSystem)}/>
  <div slot="actions">
    <Button on:click={() => open = false}>Close</Button>
  </div>
</Dialog>
