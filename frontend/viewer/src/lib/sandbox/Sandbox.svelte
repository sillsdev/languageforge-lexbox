<script lang="ts">
  import DialogsProvider from '$lib/DialogsProvider.svelte';
  import OverrideFields from '$lib/OverrideFields.svelte';
  import ThemePicker from '$lib/ThemePicker.svelte';
  import {EditorGrid} from '$lib/components/editor';
  import IfOnce from '$lib/components/if-once/if-once.svelte';
  import {Button, buttonVariants} from '$lib/components/ui/button';
  import {Checkbox} from '$lib/components/ui/checkbox';
  import * as Dialog from '$lib/components/ui/dialog';
  import {Switch} from '$lib/components/ui/switch';
  import {DotnetService, type ISense} from '$lib/dotnet-types';
  import type {FieldIds} from '$lib/entry-editor/field-data';
  import SenseEditorPrimitive from '$lib/entry-editor/object-editors/SenseEditorPrimitive.svelte';
  import {InMemoryApiService} from '$lib/in-memory-api-service';
  import {AppNotification} from '$lib/notifications/notifications';
  import {tryUseService} from '$lib/services/service-provider';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import {delay} from '$lib/utils/time';
  import {QueryParamState} from '$lib/utils/url.svelte';
  import {initView, initViewSettings} from '$lib/views/view-service';
  import {dndzone} from 'svelte-dnd-action';
  import {T} from 'svelte-i18n-lingui';
  import {Link} from 'svelte-routing';


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

  //reorderer



  let nameSearchParam = new QueryParamState({key: 'name'});
  let isGood = $state(false);
  let isBetter = $state(false);
  useBackHandler({addToStack: () => isGood, onBack: () => isGood = false});
  useBackHandler({addToStack: () => isBetter, onBack: () => isBetter = false});
  let dialogOpen = $state(false);
  useBackHandler({addToStack: () => dialogOpen, onBack: () => dialogOpen = false});

  let show = $state(false);
  let reseter = $state(0);

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
  <div class="grid grid-cols-3 gap-6">
    <div class="flex flex-col gap-2 border p-4 justify-between">
      <Button onclick={incrementAsync} {loading} icon="i-mdi-ab-testing">Shadcn FTW {count}</Button>
      <label>
        Yes?!
        <Checkbox bind:checked={loading}></Checkbox>
      </label>
    </div>
  </div>
  <div class="flex flex-col gap-2 border p-4 justify-between">
    <h3 class="font-medium">Reorder example</h3>

    <div>

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
</div>

<hr class="border-t border-gray-200 my-6"/>

<div class="p-6 overflow-hidden">
  <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
    <div class="flex flex-col gap-2 border p-4 justify-between">
      <div class="flex flex-col gap-2">
        Notifications
        <Button onclick={() => testingService?.throwException()}>Throw Exception</Button>
        <Button onclick={() => testingService?.throwExceptionAsync()}>Throw Exception Async</Button>
        <Button onclick={() => AppNotification.display('This is a simple notification', 'info')}>Simple
          Notification
        </Button>
        <Button onclick={() => AppNotification.displayAction('This is a notification with an action', 'info', {
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
            <SenseEditorPrimitive
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
