<script module lang="ts">
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import Delayed from '$lib/components/Delayed.svelte';
  import Loading from '$lib/components/Loading.svelte';
  import {delay} from '$lib/utils/time';
  import {VList} from 'virtua/svelte';
  import {SvelteMap} from 'svelte/reactivity';

  const { Story } = defineMeta({
    component: Delayed,
    title: 'Primitives/Delayed',
    args: {
      delay: 1000,
    }
  });

  // Mock service for complex story
  const cache = new SvelteMap<number, string>();
  async function loadItem(id: number) {
    const cached = cache.get(id);
    if (cached) return cached;
    await delay(300 + Math.random() * 400);
    const val = `Loaded Item #${id}`;
    cache.set(id, val);
    return val;
  }

  const items = Array.from({ length: 1000 }, (_, i) => i);
</script>

<Story name="Uncached (Delayed)">
  {#snippet template(args)}
    {@const simulatedLoadTime = 500}
    <div class="p-4 border rounded bg-card">
      <Delayed
        load={async () => {
          await delay(simulatedLoadTime);
          return '😎🎈';
        }}
        delay={args.delay}
      >
        {#snippet children(state)}
        {@const start = Date.now()}
          {#if state.loading}
            <div class="flex items-center gap-2 text-muted-foreground italic">
              <Loading />
              Waiting {args.delay}ms before loading and {simulatedLoadTime}ms to simulate load...
            </div>
          {:else if state.error}
            <div class="text-destructive">Error: {state.error}</div>
          {:else}
            {@const end = Date.now()}
            <div class="text-primary font-bold">Loaded: {state.current} (in {end - start}ms)</div>
          {/if}
        {/snippet}
      </Delayed>
    </div>
  {/snippet}
</Story>

<Story name="Cached (Immediate)">
  {#snippet template(args)}
    <div class="p-4 border rounded bg-card">
      <Delayed
        load={() => Promise.resolve('Should not be called')}
        getCached={() => 'Instant Cached Data'}
        delay={args.delay}
      >
        {#snippet children(state)}
          {#if state.loading}
            <div class="text-muted-foreground italic">If you see this, cache didn't work...</div>
          {:else}
            <div class="text-primary font-bold">From Cache: {state.current}</div>
          {/if}
        {/snippet}
      </Delayed>
    </div>
  {/snippet}
</Story>

<Story name="Error State">
  {#snippet template(args)}
    <div class="p-4 border rounded bg-card">
      <Delayed
        load={async () => {
          await delay(500);
          throw new Error('Failed to fetch data!');
        }}
        delay={args.delay}
      >
        {#snippet children(state)}
          {#if state.loading}
            <Loading />
          {:else if state.error}
            <div class="text-destructive font-bold flex items-center gap-1">
              <div class="i-mdi-alert-circle"></div>
              {(state.error as Error).message}
            </div>
          {:else}
            <div>{state.current}</div>
          {/if}
        {/snippet}
      </Delayed>
    </div>
  {/snippet}
</Story>

<Story name="Virtual List (Stress Test)" args={{ delay: 250 }}>
  {#snippet template(args)}
    <div class="h-[400px] border rounded bg-background overflow-hidden flex flex-col">
      <div class="p-2 border-b bg-muted text-xs font-medium">
        Scroll quickly to see placeholders. Items are cached after loading.
      </div>
      <VList data={items} class="flex-1">
        {#snippet children(id: number)}
          <div class="h-10 border-b flex items-center px-4 hover:bg-accent/50 transition-colors">
            <Delayed
              load={() => loadItem(id)}
              getCached={() => cache.get(id)}
              delay={args.delay}
            >
              {#snippet children(state)}
                {#if state.loading}
                  <div class="flex items-center gap-2 text-muted-foreground text-sm italic">
                    <Loading class="size-3" />
                    <span>Loading #{id}...</span>
                  </div>
                {:else if state.error}
                  <span class="text-destructive text-sm italic">Error loading #{id}</span>
                {:else}
                  <span class="text-sm font-medium">{state.current}</span>
                {/if}
              {/snippet}
            </Delayed>
          </div>
        {/snippet}
      </VList>
    </div>
  {/snippet}
</Story>
