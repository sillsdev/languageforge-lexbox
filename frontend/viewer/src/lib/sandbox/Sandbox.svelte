<script lang="ts">
  import OverrideFields from '$lib/OverrideFields.svelte';
  import {Button} from '$lib/components/ui/button';
  import {Checkbox} from '$lib/components/ui/checkbox';
  import {DotnetService, type ISense} from '$lib/dotnet-types';
  import type {FieldIds} from '$lib/entry-editor/field-data';
  import SenseEditor from '$lib/entry-editor/object-editors/SenseEditor.svelte';
  import {InMemoryApiService} from '$lib/in-memory-api-service';
  import {AppNotification} from '$lib/notifications/notifications';
  import OptionSandbox from '$lib/sandbox/OptionSandbox.svelte';
  import {tryUseService} from '$lib/services/service-provider';
  import ButtonListItem from '$lib/utils/ButtonListItem.svelte';
  import {delay} from '$lib/utils/time';
  import {initView, initViewSettings} from '$lib/views/view-service';
  import {initWritingSystemService} from '$lib/writing-system-service';
  import {dndzone} from 'svelte-dnd-action';
  import {Button as UxButton, type MenuOption} from 'svelte-ux';
  import {writable} from 'svelte/store';
  import CrdtMultiOptionField from '../entry-editor/inputs/CrdtMultiOptionField.svelte';
  import * as Resizable from '$lib/components/ui/resizable';
  import LcmRichTextEditor from '$lib/components/lcm-rich-text-editor/lcm-rich-text-editor.svelte';
  import {lineSeparator} from '$lib/components/lcm-rich-text-editor/lcm-rich-text-editor.svelte';
  import type {IRichString} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IRichString';

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

  const inMemoryLexboxApi = InMemoryApiService.setup();
  initWritingSystemService(writable(inMemoryLexboxApi.getWritingSystemsSync()));
  initView();
  initViewSettings();

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
</script>

<div class="p-6">
  <h2 class="mb-4">
    Shadcn Sandbox
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
  <div class="grid grid-cols-3 gap-6">
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
</div>

<hr class="border-t border-gray-200 my-6"/>

<div class="p-6">
  <h2 class="mb-4">
    Svelte-UX Sandbox
  </h2>
  <div class="grid grid-cols-3 gap-6">
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
</div>
