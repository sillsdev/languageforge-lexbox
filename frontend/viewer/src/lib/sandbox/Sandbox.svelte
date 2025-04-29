<script lang="ts">
  import OverrideFields from '$lib/OverrideFields.svelte';
  import {Button} from '$lib/components/ui/button';
  import {Checkbox} from '$lib/components/ui/checkbox';
  import {DotnetService, type IEntry, type ISense} from '$lib/dotnet-types';
  import type {FieldIds} from '$lib/entry-editor/field-data';
  import SenseEditor from '$lib/entry-editor/object-editors/SenseEditor.svelte';
  import {InMemoryApiService} from '$lib/in-memory-api-service';
  import {AppNotification} from '$lib/notifications/notifications';
  import OptionSandbox from '$lib/sandbox/OptionSandbox.svelte';
  import {tryUseService} from '$lib/services/service-provider';
  import ButtonListItem from '$lib/utils/ButtonListItem.svelte';
  import {delay} from '$lib/utils/time';
  import {initView, initViewSettings} from '$lib/views/view-service';
  import {dndzone} from 'svelte-dnd-action';
  import {Button as UxButton, type MenuOption} from 'svelte-ux';
  import CrdtMultiOptionField from '../entry-editor/inputs/CrdtMultiOptionField.svelte';
  import * as Resizable from '$lib/components/ui/resizable';
  import LcmRichTextEditor from '$lib/components/lcm-rich-text-editor/lcm-rich-text-editor.svelte';
  import {lineSeparator} from '$lib/components/lcm-rich-text-editor/lcm-rich-text-editor.svelte';
  import type {IRichString} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IRichString';
  import ThemePicker from '$lib/ThemePicker.svelte';
  import {EditorGrid} from '$lib/components/editor';
  import EditorSandbox from './EditorSandbox.svelte';
  import EntryOrSensePicker, {type EntrySenseSelection} from '$lib/entry-editor/EntryOrSensePicker.svelte';
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';
  import DialogsProvider from '$lib/DialogsProvider.svelte';
  import {TabsList, TabsTrigger, Tabs} from '$lib/components/ui/tabs';

  const crdtOptions: MenuOption[] = [
    {value: 'a', label: 'Alpha'},
    {value: 'b', label: 'Beta'},
    {value: 'c', label: 'Charlie'},
  ];

  let crdtValue = $state(['a']);

  const testingService = tryUseService(DotnetService.TestingService);

  function triggerNotificationWithLargeDetail() {
    let detail = '';
    for (let i = 0; i < 100; i++) {
      if (i % 10 === 0) detail += '='.repeat(20);
      detail += `This is line ${i + 1} of the detail\n`;
    }
    AppNotification.display('This is a notification with a large detail', 'info', undefined, detail);
  }

  InMemoryApiService.setup();
  initView();
  initViewSettings();
  const writingSystemService = useWritingSystemService();

  function makeSense(s: ISense) {
    return s;
  }

  let senseFields: ({ id: FieldIds })[] = $state([{id: 'gloss'}, {id: 'definition'}]);

  function updateFields(e: CustomEvent<{ items: ({ id: FieldIds })[] }>) {
    senseFields = e.detail.items;
  }
  let count = $state(0);
  let loading = $state(false);
  async function incrementAsync() {
    loading = true;
    await delay(1000);
    count++;
    loading = false;
  }

  let richString: IRichString = $state({
    spans: [{text: 'Hello', ws: 'en'}, {text: ' World', ws: 'js'}, {text: ` type ${lineSeparator}script`, ws: 'ts'}],
  });
  let selectedEntryHistory: EntrySenseSelection[] = $state([]);
  let openPicker = $state(false);
  let pickerMode: 'entries-and-senses' | 'only-entries' = $state('only-entries');

  function disableEntry(entry: IEntry): false | { reason: string, disableSenses?: true } {
    const selected = selectedEntryHistory.some(e => e.entry.id === entry.id);
    if (!selected) return false;
    return {
      reason: 'You cannot select an entry that you have already selected',
      disableSenses: true
    };
  }
</script>
<DialogsProvider/>
<div class="p-6 shadcn-root">
  <h2 class="mb-4 flex gap-8 items-center">
    Shadcn Sandbox <ThemePicker />
  </h2>
  <div class="grid grid-cols-3 gap-6">
    <div class="flex flex-col gap-2 border p-4 justify-between">
      <Button onclick={incrementAsync} {loading} icon="i-mdi-ab-testing">Shadcn FTW {count}</Button>
      <label>
        Yes?!
        <Checkbox bind:checked={loading}></Checkbox>
      </label>
    </div>
  </div>
  <EditorSandbox />
  <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
    <Button onclick={() => richString = {spans: [{text: 'test', ws: 'en'}]}}>Replace Rich Text</Button>
    <LcmRichTextEditor label="Test Rich Text Editor" bind:value={richString}/>
    <pre>{JSON.stringify(richString, null, 2).replaceAll(lineSeparator, '\n')}</pre>
  </div>
  <div class="flex flex-col gap-2 border p-4 justify-between">
    <h3 class="font-medium">Resizable Example</h3>
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
  </div>
  <div class="flex flex-col gap-2 border p-4 justify-between">
    <h3 class="font-medium">Entry picker example</h3>

    <Tabs bind:value={pickerMode} class="mb-1">
      <TabsList>
        <TabsTrigger value="only-entries">Entry only</TabsTrigger>
        <TabsTrigger value="entries-and-senses">Entry or Sense</TabsTrigger>
      </TabsList>
    </Tabs>
    <Button onclick={() => openPicker = true}>Open picker</Button>
    <EntryOrSensePicker title="Test selecting something"
                        bind:open={openPicker}
                        disableEntry={disableEntry}
                        mode={pickerMode}
                        pick={(e) => selectedEntryHistory.push(e)}/>
    <div>
      {#each selectedEntryHistory as selected}
        <p>
          Entry: {writingSystemService.headword(selected.entry)}
          {#if selected.sense}
            Sense: {writingSystemService.firstGloss(selected.sense)}
          {/if}
        </p>
      {/each}
    </div>
  </div>
</div>

<hr class="border-t border-gray-200 my-6"/>

<div class="p-6 overflow-hidden">
  <h2 class="mb-4">
    Svelte-UX Sandbox
  </h2>
  <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
    <div class="flex flex-col gap-2 border p-4 justify-between">
      MultiOptionEditor configurations
      <svelte:boundary>
        <OptionSandbox/>
        {#snippet failed(error)}
          Error opening options sandbox {error}
        {/snippet}
      </svelte:boundary>
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
        <UxButton variant="fill" on:click={() => crdtValue = ['c']}>Select Charlie only</UxButton>
      </div>
    </div>

    <div class="flex flex-col gap-2 border p-4 justify-between">
      <div class="flex flex-col gap-2">
        Notifications
        <UxButton variant="fill" on:click={() => testingService?.throwException()}>Throw Exception</UxButton>
        <UxButton variant="fill" on:click={() => testingService?.throwExceptionAsync()}>Throw Exception Async</UxButton>
        <UxButton variant="fill" on:click={() => AppNotification.display('This is a simple notification', 'info')}>Simple
          Notification
        </UxButton>
        <UxButton variant="fill" on:click={() => AppNotification.displayAction('This is a notification with an action', 'info', {
          label: 'Action',
          callback: () => alert('Action clicked')
        })}>Notification with action
        </UxButton>
        <UxButton variant="fill" on:click={() => triggerNotificationWithLargeDetail()}>Notification with a large detail
        </UxButton>
      </div>
    </div>
    <div class="flex flex-col gap-2 border p-4 justify-between">
      <div class="flex flex-col gap-2">
        Button
        <UxButton variant="fill" disabled={loading} {loading} on:click={incrementAsync}>
          Increment Async
        </UxButton>
        click count: {count}
      </div>
      <div class="flex flex-col gap-2">
        ButtonListItem
        <ButtonListItem disabled={loading} on:click={incrementAsync}>
          Increment Async
        </ButtonListItem>
        click count: {count}
      </div>
    </div>
    <div class="border grid" style="grid-template-columns: auto 1fr">
      <div class="col-span-2">
        <h3>Override Fields</h3>
      </div>
      <div>
        <p>Shown:</p>
        <div class="p-2" use:dndzone={{items: senseFields, flipDurationMs: 200}} onconsider={updateFields}
            onfinalize={updateFields}>
          {#each senseFields as field (field)}
            <div class="p-2 border m-3">{field.id}</div>
          {/each}
        </div>
      </div>
      <svelte:boundary>
        <EditorGrid class="border p-4">
          <OverrideFields shownFields={senseFields.map(f => f.id)} respectOrder>
            <SenseEditor
              sense={makeSense({id: '1', gloss: {'en': 'Hello'}, entryId: 'e1', definition: {}, semanticDomains: [], exampleSentences: []})}/>
          </OverrideFields>
        </EditorGrid>
        {#snippet failed(error)}
          Error opening override fields {error}
        {/snippet}
      </svelte:boundary>
    </div>

  </div>
</div>
<div id="bottom" class="fixed bottom-0 left-1/2"></div>
