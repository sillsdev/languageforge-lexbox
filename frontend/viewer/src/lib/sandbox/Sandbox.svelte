<script lang="ts">
  import {Button, type MenuOption} from 'svelte-ux';
  import CrdtMultiOptionField from '../entry-editor/inputs/CrdtMultiOptionField.svelte';
  import {DotnetService, type ISense} from '$lib/dotnet-types';
  import {tryUseService} from '$lib/services/service-provider';
  import {AppNotification} from '$lib/notifications/notifications';
  import SenseEditor from '$lib/entry-editor/object-editors/SenseEditor.svelte';
  import {InMemoryApiService} from '$lib/in-memory-api-service';
  import OptionSandbox from '$lib/sandbox/OptionSandbox.svelte';
  import {initWritingSystemService} from '$lib/writing-system-service';
  import {writable} from 'svelte/store';
  import {initView, initViewSettings} from '$lib/views/view-service';
  import OverrideFields from '$lib/OverrideFields.svelte';
  import type {FieldIds} from '$lib/entry-editor/field-data';
  import {dndzone} from 'svelte-dnd-action';


  const crdtOptions: MenuOption[] = [
    {value: 'a', label: 'Alpha'},
    {value: 'b', label: 'Beta'},
    {value: 'c', label: 'Charlie'},
  ];

  let crdtValue = ['a'];

  const testingService = tryUseService(DotnetService.TestingService);

  function triggerNotificationWithLargeDetail() {
    let detail = '';
    for (let i = 0; i < 100; i++) {
      if (i % 10 === 0) detail += '='.repeat(20);
      detail += `This is line ${i + 1} of the detail\n`;
    }
    AppNotification.display('This is a notification with a large detail', 'info', undefined, detail);
  }

  const inMemoryLexboxApi = InMemoryApiService.setup();
  initWritingSystemService(writable(inMemoryLexboxApi.getWritingSystemsSync()));
  initView();
  initViewSettings();

  function makeSense(s: ISense) {
    return s;
  }

  let senseFields: ({ id: FieldIds })[] = [{id: 'gloss'}, {id: 'definition'}];

  function updateFields(e: CustomEvent<{ items: ({ id: FieldIds })[] }>) {
    senseFields = e.detail.items;
  }
</script>


<div class="grid grid-cols-3 gap-6 p-6">
  <div class="flex flex-col gap-2 border p-4 justify-between">
    MultiOptionEditor configurations
    <OptionSandbox/>
  </div>

  <div class="flex flex-col gap-2 border p-4 justify-between">
    <div class="flex flex-col gap-2">
      Lower level editor
      <div class="mb-4">
        String values and MenuOptions
        <CrdtMultiOptionField bind:value={crdtValue} options={crdtOptions}/>
      </div>
    </div>
    <div class="flex flex-col">
      <p>selected: {crdtValue.join('|')}</p>
      <Button variant="fill" on:click={() => crdtValue = ['c']}>Select Charlie only</Button>
    </div>
  </div>

  <div class="flex flex-col gap-2 border p-4 justify-between">
    <div class="flex flex-col gap-2">
      Notifications
      <Button variant="fill" on:click={() => testingService?.throwException()}>Throw Exception</Button>
      <Button variant="fill" on:click={() => testingService?.throwExceptionAsync()}>Throw Exception Async</Button>
      <Button variant="fill" on:click={() => AppNotification.display('This is a simple notification', 'info')}>Simple
        Notification
      </Button>
      <Button variant="fill" on:click={() => AppNotification.displayAction('This is a notification with an action', 'info', {
        label: 'Action',
        callback: () => alert('Action clicked')
      })}>Notification with action
      </Button>
      <Button variant="fill" on:click={() => triggerNotificationWithLargeDetail()}>Notification with a large detail
      </Button>
    </div>
  </div>
  <div class="border grid" style="grid-template-columns: auto 1fr">
    <div class="col-span-2">
      <h3>Override Fields</h3>
    </div>
    <div>
      <p>Shown:</p>
      <div class="p-2" use:dndzone={{items: senseFields, flipDurationMs: 200}} on:consider={updateFields}
           on:finalize={updateFields}>
        {#each senseFields as field (field)}
          <div class="p-2 border m-3">{field.id}</div>
        {/each}
      </div>
    </div>
    <div class="editor-grid border p-4">
      <OverrideFields shownFields={senseFields.map(f => f.id)} respectOrder>
        <SenseEditor
          sense={makeSense({id: '1', gloss: {'en': 'Hello'}, entryId: 'e1', definition: {}, semanticDomains: [], exampleSentences: []})}/>
      </OverrideFields>
    </div>
  </div>

</div>
