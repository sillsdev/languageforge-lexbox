<script lang="ts" module>
  import type {StoryContext} from '@storybook/svelte-vite';

  let defaultSize = $state(75);

  const svelteStoryContext = new Context<StoryContext>('StoryContext');

  export function initSvelteStoryContext(storyContext: StoryContext): StoryContext {
    return svelteStoryContext.set(storyContext);
  }

  export function useSvelteStoryContext(): StoryContext {
    return svelteStoryContext.get();
  }
</script>

<script lang="ts">
  import { ResizablePaneGroup, ResizablePane, ResizableHandle } from '$lib/components/ui/resizable';
  import ThemePicker from '$lib/components/ThemePicker.svelte';
  import {initView} from '$lib/views/view-service';
  import {ModeWatcher} from 'mode-watcher';
  import {Context} from 'runed';
  import { type Snippet } from 'svelte';
  import ViewPicker from '../../src/project/browse/EditorViewOptions.svelte';
  import {InMemoryDemoApi} from '$project/demo/in-memory-demo-api';
  import {setupServiceProvider} from '$lib/services/service-provider';
  import {setupDotnetServiceProvider} from '$lib/services/service-provider-dotnet';
  import {XButton} from '$lib/components/ui/button';
  import {extract} from 'runed';
  import {setupGlobalErrorHandlers} from '$lib/errors/global-errors';
  import {TooltipProvider} from '$lib/components/ui/tooltip';
  import {initRootLocation} from '$lib/services/root-location-service';
  import {readable} from 'svelte/store';


  let { children }: { children: Snippet } = $props();

  setupServiceProvider();
  setupDotnetServiceProvider();
  InMemoryDemoApi.setup();
  initView();
  const storyContext = useSvelteStoryContext();
  setupGlobalErrorHandlers();
  initRootLocation(readable());

  const {
    themePicker = true,
    viewPicker = false,
    resizable = true,
    showValue = undefined,
    value: paramValue,
  } = storyContext.parameters.fwlite ?? {};

  const value = $derived(extract(paramValue ?? storyContext.parameters.fwlite?.value ?? storyContext.args?.value));

  export const lineSeparator = '\u2028';

  let hideValue = $state(showValue !== true);
</script>

<TooltipProvider delayDuration={300}>
  <ResizablePaneGroup direction="horizontal" class="!overflow-visible">
    <ResizablePane class="!overflow-visible" defaultSize={resizable ? defaultSize : 100} onResize={(size) => defaultSize = size}>
      {@render children()}
    </ResizablePane>
    {#if resizable}
      <!-- looks cool 🤷 https://github.com/huntabyte/shadcn-svelte/blob/bcbe10a4f65d244a19fb98ffb6a71d929d9603bc/sites/docs/src/lib/components/docs/block-preview.svelte#L65 -->
      <ResizableHandle
        class="after:bg-border relative w-3 bg-transparent p-0 after:absolute after:right-0 after:top-1/2 after:h-8 after:w-[6px] after:-translate-y-1/2 after:translate-x-[-1px] after:rounded-full after:transition-all after:hover:h-10"
      />
      <ResizablePane class="px-2">
        {#if showValue === true || value && showValue !== false}
          <div class="relative">
            {#if !hideValue}
              <pre class="max-h-[calc(100vh-2rem)] fixed overflow-auto whitespace-pre-wrap bg-background">{
              JSON.stringify(value, null, 2)?.replaceAll(lineSeparator, '\n') ?? 'undefined'}
              </pre>
            {/if}
            <XButton class="[&:not(:hover)]:opacity-30 fixed top-4 right-4" icon="i-mdi-eye" onclick={() => hideValue = !hideValue} />
          </div>
        {/if}
      </ResizablePane>
    {/if}
  </ResizablePaneGroup>

  <div class="fixed bottom-4 right-4 inline-flex justify-end gap-2">
    {#if viewPicker}
      <ViewPicker />
    {/if}
    {#if themePicker}
      <ThemePicker />
    {/if}
    <ModeWatcher />
  </div>
</TooltipProvider>
