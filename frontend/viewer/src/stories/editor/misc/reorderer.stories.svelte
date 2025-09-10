<script module lang="ts">
  import {Reorderer} from '$lib/components/reorderer';
  import {Button} from '$lib/components/ui/button';
  import {Tabs, TabsList, TabsTrigger} from '$lib/components/ui/tabs';
  import {cn} from '$lib/utils';
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import {expect, fn, userEvent, waitFor, within} from 'storybook/test';

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
    component: Reorderer<string>,
    args: {
      onchange: fn(),
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

<Story name="Standard" play={async ({ canvasElement, args }) => {
  // Open and get menu items
  const trigger = canvasElement.querySelector('[data-dropdown-menu-trigger]')!;
  await expect(trigger).toBeInTheDocument();
  await userEvent.click(trigger);
  const doc = within(document.documentElement);
  let menuItems = await doc.findAllByRole('menuitem');
  await expect(menuItems.length).toBe(items.length);
  await expect(menuItems[0]).toHaveAttribute('data-current');
  await expect(menuItems[0]).toHaveTextContent('1');
  await expect(menuItems[1]).toHaveTextContent('2');
  await expect(menuItems[2]).toHaveTextContent('3');

  // Test hovering
  await userEvent.hover(menuItems[1]);
  await expect(menuItems[1]).toHaveAttribute('data-highlighted');
  await userEvent.hover(menuItems[2]);
  await expect(menuItems[2]).toHaveAttribute('data-highlighted');

  // Move to new position: 1 -> 3
  await userEvent.click(menuItems[2]);
  await waitFor(async () => expect(doc.queryByRole('menuitem')).toBeNull()); // menu closed
  await expect(args.onchange).toHaveBeenCalledOnce();
  await expect(args.onchange).toHaveBeenLastCalledWith(expect.anything(), 0, 2);
  await expect(items[2]).toBe('1');

  // Move back to top: 3 -> 1
  await userEvent.click(trigger);
  menuItems = await doc.findAllByRole('menuitem');
  await expect(menuItems.length).toBe(items.length);
  await expect(menuItems[2]).toHaveAttribute('data-current');
  await userEvent.click(menuItems[0]);

  await expect(args.onchange).toHaveBeenCalledTimes(2);
  await expect(args.onchange).toHaveBeenLastCalledWith(expect.anything(), 2, 0);
  await expect(items[0]).toBe('1');
}}>
  {#snippet template(args)}
    <Reorderer {...args} item={currentItem} bind:items />
  {/snippet}
</Story>
