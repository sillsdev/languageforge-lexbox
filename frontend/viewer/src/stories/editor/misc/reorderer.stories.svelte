<script module lang="ts">
  import {Reorderer} from '$lib/components/reorderer';
  import {Button} from '$lib/components/ui/button';
  import {Tabs, TabsList, TabsTrigger} from '$lib/components/ui/tabs';
  import {cn} from '$lib/utils';
  import {defineMeta} from '@storybook/addon-svelte-csf';

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

  const { Story } = defineMeta({
    title: 'editor/misc/reorderer',
    component: Reorderer<string>,
    args: {
      getDisplayName: displayFunc,
    }
  });
</script>

<div class="flex gap-4 flex-wrap">
  <Tabs bind:value={display} class="mb-1">
    <TabsList>
      <TabsTrigger value="x1">Times 1</TabsTrigger>
      <TabsTrigger value="x100">Times 100</TabsTrigger>
    </TabsList>
  </Tabs>
  <Button onclick={randomItems}>Randomize items</Button>
</div>

<code>
  {#each items as item (item)}
    <span class={cn('p-2 inline-block', item === currentItem && 'text-primary font-bold')}>
      {displayFunc(item)}
    </span>
  {/each}
</code>

<hr class="mb-4">

<Story name="Standard">
  {#snippet template(args)}
    <Reorderer {...args} item={currentItem} bind:items />
  {/snippet}
</Story>
