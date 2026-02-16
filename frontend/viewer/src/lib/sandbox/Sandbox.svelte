<script lang="ts">
  import OverrideFields from '$lib/views/OverrideFields.svelte';
  import {Button, buttonVariants} from '$lib/components/ui/button';
  import {Checkbox} from '$lib/components/ui/checkbox';
  import {DotnetService, type IEntry, type ISense} from '$lib/dotnet-types';
  import type {FieldId} from '$lib/views/fields';
  import SenseEditorPrimitive from '$lib/entry-editor/object-editors/SenseEditorPrimitive.svelte';
  import {InMemoryDemoApi} from '$project/demo/in-memory-demo-api';
  import {AppNotification} from '$lib/notifications/notifications';
  import {tryUseService} from '$lib/services/service-provider';
  import {delay} from '$lib/utils/time';
  import {initView, initViewSettings} from '$lib/views/view-service';
  import {dndzone} from 'svelte-dnd-action';
  import * as Resizable from '$lib/components/ui/resizable';
  import LcmRichTextEditor, {lineSeparator} from '$lib/components/lcm-rich-text-editor/lcm-rich-text-editor.svelte';
  import type {IRichString} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IRichString';
  import ThemePicker from '$lib/components/ThemePicker.svelte';
  import {EditorGrid} from '$lib/components/editor';
  import EditorSandbox from './EditorSandbox.svelte';
  import EntryOrSensePicker, {type EntrySenseSelection} from '$lib/entry-editor/EntryOrSensePicker.svelte';
  import {useWritingSystemService} from '$project/data';
  import DialogsProvider from '$lib/DialogsProvider.svelte';
  import {Tabs, TabsList, TabsTrigger} from '$lib/components/ui/tabs';
  import {Reorderer} from '$lib/components/reorderer/index.js';
  import {Switch} from '$lib/components/ui/switch';
  import {QueryParamState} from '$lib/utils/url.svelte';
  import {Link} from 'svelte-routing';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import * as Dialog from '$lib/components/ui/dialog';
  import {T} from 'svelte-i18n-lingui';
  import IfOnce from '$lib/components/if-once/if-once.svelte';
  import LocalizationPicker from '$lib/i18n/LocalizationPicker.svelte';
  import {formatDate, FormatDate, formatNumber} from '$lib/components/ui/format';
  import {SvelteDate} from 'svelte/reactivity';
  import {RichTextToggle} from '$lib/dotnet-types/generated-types/MiniLcm/Models/RichTextToggle';
  import {FFmpegApi} from '$lib/components/audio/ffmpeg';
  import type {View} from '$lib/views/view-data';
  import {useDialogsService} from '$lib/services/dialogs-service';

  const testingService = tryUseService(DotnetService.TestingService);

  function triggerNotificationWithLargeDetail() {
    let description = '';
    for (let i = 0; i < 100; i++) {
      if (i % 10 === 0) description += '='.repeat(20);
      description += `This is line ${i + 1} of the detail\n`;
    }
    AppNotification.error('This is a notification with a large detail', description);
  }

  InMemoryDemoApi.setup();
  initView();
  initViewSettings();
  const writingSystemService = useWritingSystemService();

  function makeSense(s: ISense) {
    return s;
  }

  let senseFields: ({ id: FieldId })[] = $state([{id: 'gloss'}, {id: 'definition'}]);
  let overrides = $state<View['overrides']>({});

  function updateFields(e: CustomEvent<{ items: ({ id: FieldId })[] }>) {
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

  const originalRichString: IRichString = {
    spans: [{text: 'Hello', ws: 'en'}, {text: ' World', ws: 'js'}, {text: ` type ${lineSeparator}script`, ws: 'en', bold: RichTextToggle.On}],
  };
  let richString: IRichString | undefined = $state(originalRichString);
  let readonly = $state(false);
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

  //reorderer
  let direction: 'vertical' | 'horizontal' = $state('vertical');
  let items: string[] = $state(['1', '2', '3', '4', '5', '6', '7', '8', '9', '10']);
  let currentItem = $state('1');
  let display: 'x100' | 'x1' = $state('x1');
  function displayFunc(item: string) {
    return (Number(item) * (display === 'x100' ? 100 : 1)).toString();
  }
  function randomItems() {
    items = Array.from({length: 10}, () => (Math.random() * 100).toFixed(0));
    currentItem = items[0];
  }



  let nameSearchParam = new QueryParamState({key: 'name'});
  let isGood = $state(false);
  let isBetter = $state(false);
  useBackHandler({addToStack: () => isGood, onBack: () => isGood = false, key: 'sandbox-good'});
  useBackHandler({addToStack: () => isBetter, onBack: () => isBetter = false, key: 'sandbox-better'});
  let dialogOpen = $state(false);
  useBackHandler({addToStack: () => dialogOpen, onBack: () => dialogOpen = false, key: 'sandbox-dialog'});

  // Dialogs service demo
  const dialogsService = useDialogsService();
  let deleteResult: string | undefined = $state(undefined);
  async function showDelete() {
    const res = await dialogsService.promptDelete('Example item', 'Normal example');
    deleteResult = res ? 'Confirmed delete' : 'Cancelled delete';
  }
  async function showDangerous() {
    const res = await dialogsService.promptDelete('Dangerous item', undefined, {
      isDangerous: true,
      details: 'This is irreversible',
    });
    deleteResult = res ? 'Confirmed dangerous delete' : 'Cancelled dangerous delete';
  }

  const variants = Object.keys(buttonVariants.variants.variant) as unknown as (keyof typeof buttonVariants.variants.variant)[];
  const sizes = Object.keys(buttonVariants.variants.size) as unknown as (keyof typeof buttonVariants.variants.size)[];

  let buttonsLoading = $state(false);
  function testLoading() {
    buttonsLoading = true;
    setTimeout(() => {
      buttonsLoading = false;
    }, 1000);
  }

  let show = $state(false);
  let reseter = $state(0);

  let currentDate = new SvelteDate();

  async function preloadFFmpeg() {
    console.log('Loading FFmpeg...');
    let ffmpeg = await FFmpegApi.create();
    console.log('FFmpeg loaded:', ffmpeg);
  }
</script>
<DialogsProvider/>
<div class="p-6 shadcn-root">
  <h2 class="mb-4 flex gap-8 items-center">
    <T msg="Shadcn Sandbox # #">
      <ThemePicker />
      {#snippet second()}
        🤠
      {/snippet}
    </T>
  </h2>
  <Button onclick={preloadFFmpeg}>Load FFmpeg</Button>
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
    <div>
      <Button onclick={() => richString = {spans: [{text: 'test', ws: 'en'}]}}>Replace Rich Text</Button>
      <Button onclick={() => richString = undefined}>Set undefined</Button>
      <Button onclick={() => richString = originalRichString}>Reset</Button>
      <label>
        <Checkbox bind:checked={readonly}/> Readonly
      </label>
    </div>
    <LcmRichTextEditor label="Test Rich Text Editor" bind:value={richString} {readonly} normalWs="en"
       onchange={() => richString = JSON.parse(JSON.stringify($state.snapshot(richString)))} />
    <pre>{JSON.stringify(richString, null, 2)?.replaceAll(lineSeparator, '\n') ?? 'undefined'}</pre>
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
      {#each selectedEntryHistory as selected (selected.entry.id)}
        <p>
          Entry: {writingSystemService.headword(selected.entry)}
          {#if selected.sense}
            Sense: {writingSystemService.firstGloss(selected.sense)}
          {/if}
        </p>
      {/each}
    </div>
  </div>
  <div class="flex flex-col gap-2 border p-4 justify-between">
    <h3 class="font-medium">Reorder example</h3>
    <Tabs bind:value={direction} class="mb-1">
      <TabsList>
        <TabsTrigger value="vertical">Vertical</TabsTrigger>
        <TabsTrigger value="horizontal">Horizontal</TabsTrigger>
      </TabsList>
    </Tabs>
    <Tabs bind:value={display} class="mb-1">
      <TabsList>
        <TabsTrigger value="x1">Times 1</TabsTrigger>
        <TabsTrigger value="x100">Times 100</TabsTrigger>
      </TabsList>
    </Tabs>
    <Button onclick={randomItems}>Randomize items</Button>
    <div>
      <p>Current item: {currentItem}</p>
      <Reorderer {direction}
                 item={currentItem}
                 bind:items
                 getDisplayName={displayFunc}/>
    </div>
    <div>
      <code>{JSON.stringify(items)}</code>
    </div>
  </div>
  <div class="flex flex-col gap-2 border p-4 justify-between">
    <h3 class="font-medium">Search Params binding</h3>
    <input bind:value={nameSearchParam.current}/>
    <Link to="/sandbox?name=batman" class={buttonVariants({variant: 'default'})} preserveScroll>Go to Batman</Link>
    <p>These switches should respect the back button but only for being turned off</p>
    <Switch label="Good" bind:checked={isGood}/>
    <Switch label="Better" bind:checked={isBetter}/>
    <Dialog.Root bind:open={dialogOpen}>
      <Dialog.Trigger class={buttonVariants({variant: 'default'})}>Show Dialog</Dialog.Trigger>
      <Dialog.Content>
        <div>
          <Link to="/testing/project-view" class={buttonVariants({variant: 'default'})}>Goto Project view</Link>
        </div>
      </Dialog.Content>
    </Dialog.Root>
  </div>
  <div class="flex flex-col gap-2 border p-4 justify-between">
    <h3 class="font-medium">Delete dialog example</h3>
    <div class="flex gap-2 flex-wrap">
      <Button onclick={showDelete}>Show Delete Dialog</Button>
      <Button variant="destructive" onclick={showDangerous}>Show Dangerous Delete Dialog</Button>
    </div>
    {#if deleteResult}
      <div>Result: {deleteResult}</div>
    {/if}
  </div>
</div>

<hr class="border-t border-gray-200 my-6"/>

<div class="p-6 overflow-hidden">
  <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
    <div class="flex flex-col gap-2 border p-4 justify-between">
      <div class="flex flex-col gap-2">
        Notifications
        <Button onclick={() => {if (testingService) void testingService.throwException(); else throw new Error('Failed to throw exception')}}>Throw Exception</Button>
        <Button onclick={() => {if (testingService) void testingService.throwExceptionAsync(); else throw new Error('Failed to throw exception async')}}>Throw Exception Async</Button>
        <Button onclick={() => AppNotification.display('This is a simple notification')}>Simple
          Notification
        </Button>
        <Button onclick={() => AppNotification.displayAction('This is a notification with an action', {
          label: 'Action',
          callback: () => alert('Action clicked')
        })}>Notification with action
        </Button>
        <Button onclick={() => triggerNotificationWithLargeDetail()}>Notification with a large detail
        </Button>
      </div>
    </div>
    <div class="flex flex-col gap-2 border p-4 justify-between">
      <div class="flex flex-col gap-2">
        Button
        <Button disabled={loading} {loading} onclick={incrementAsync}>
          Increment Async
        </Button>
        click count: {count}
      </div>
    </div>
    <div class="flex flex-col gap-2 border p-4 justify-between">
      <div class="flex flex-col gap-2">
        IfOnce
        {#key reseter}
          <IfOnce show={show}>
            content
          </IfOnce>
        {/key}
        <label>
          <Checkbox bind:checked={show}/>
          Show
        </label>
        <Button onclick={() => reseter++}>Reset</Button>
      </div>
    </div>
    <div class="border grid" style="grid-template-columns: auto 1fr">
      <div class="col-span-2">
        <h3>Override Fields</h3>
      </div>
      <div>
        <Button onclick={() => overrides = {vernacularWritingSystems: ['en'], analysisWritingSystems: ['en']}}>Set en only</Button>
        <Button onclick={() => overrides = {}}>Reset</Button>
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
          <OverrideFields shownFields={senseFields.map(f => f.id)} respectOrder {overrides}>
            <SenseEditorPrimitive
              sense={makeSense({id: '1', gloss: {'en': 'Hello'}, entryId: 'e1', definition: {}, semanticDomains: [], exampleSentences: []})}/>
          </OverrideFields>
        </EditorGrid>
        {#snippet failed(error)}
          Error opening override fields {error}
        {/snippet}
      </svelte:boundary>
    </div>

    <div class="flex flex-col gap-2 border p-4 justify-between">
      <div>
        <h3>Buttons</h3>
      </div>
      <div><Button onclick={testLoading}>Test loading</Button></div>
      <div class="flex gap-2 flex-wrap">
        {#each variants as variant (variant)}
          <Button loading={buttonsLoading} {variant}>{variant} button</Button>
        {/each}
        {#each variants as variant (variant)}
          <Button loading={buttonsLoading} {variant} icon="i-mdi-auto-fix"></Button>
        {/each}
        {#each sizes as size (size)}
          <Button loading={buttonsLoading} {size}>Size: {size}</Button>
        {/each}
        {#each sizes as size (size)}
          <Button loading={buttonsLoading} {size} icon="i-mdi-auto-fix"/>
        {/each}
      </div>
    </div>
    <div class="flex flex-col gap-2 border p-4 justify-between">
      <div>
        <h3>Formatters</h3>
      </div>
      <div>
        <LocalizationPicker/>
      </div>
      <div class="grid grid-cols-2 gap-x-4 gap-y-2 items-center">
        <div class="font-medium">Date component:</div>
        <div><FormatDate date={currentDate} options={{timeStyle: 'medium'}}/></div>
        <div class="font-medium">Date function:</div>
        <div>{formatDate(currentDate, {timeStyle: 'medium'})}</div>
        <div class="font-medium">Number function:</div>
        <div>{formatNumber(currentDate.getTime())}</div>
      </div>
    </div>
  </div>
</div>
<div id="bottom" class="fixed bottom-0 left-1/2"></div>
