<script module lang="ts">
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import HomeViewStory, {fakeCalls} from './HomeViewStory.svelte';
  import {expect, userEvent, waitFor, within} from 'storybook/test';

  // The home screen has its own theme picker in the app bar, so the decorator's is hidden.
  // (addon-svelte-csf needs `parameters` to be an object literal, hence no fwliteStoryParameters.)
  const {Story} = defineMeta({
    title: 'home/home-view',
    parameters: {fwlite: {themePicker: false, resizable: false}},
  });

  function lexbox(canvasElement: HTMLElement) {
    return within(canvasElement.querySelector<HTMLElement>('[id="lexbox"]')!);
  }

  // Rows crossfade between sections, so a list that rebuilds or hides its rows makes them all fly around.
  async function expectRowsStay(canvasElement: HTMLElement, texts: string[], act: () => Promise<void>, done: () => Promise<void>) {
    const canvas = within(canvasElement);
    const rows = await Promise.all(texts.map(text => canvas.findByText(text)));
    const detached: string[] = [];
    function collect(records: MutationRecord[]) {
      for (const node of records.flatMap(r => [...r.removedNodes])) {
        for (const row of rows) if (node.contains(row)) detached.push(row.textContent);
      }
    }
    const observer = new MutationObserver(collect);
    observer.observe(canvasElement, {childList: true, subtree: true});

    await act();
    await waitFor(async () => {
      await done();
      await expect(fakeCalls.pending).toBe(0);
    }, {timeout: 3000});
    collect(observer.takeRecords());
    observer.disconnect();

    await expect(detached).toEqual([]);
    // Catches a rebuilt row even when the observer doesn't: its old copy is still fading out.
    for (const [i, text] of texts.entries()) await expect(canvas.getAllByText(text)).toEqual([rows[i]]);
  }
</script>

<Story name="Logged in">
  <HomeViewStory loggedIn />
</Story>

<Story name="Logged out">
  <HomeViewStory loggedIn={false} />
</Story>

<Story name="Nothing downloaded">
  <HomeViewStory loggedIn localProjects={[]} />
</Story>

<Story name="Download keeps other rows" play={async ({canvasElement}) => {
  const canvas = within(canvasElement);
  await expectRowsStay(canvasElement, ['Sena 3', 'Training Project', 'Login to see projects'],
    () => userEvent.click(canvas.getByText('Example Dictionary')),
    () => expect(canvas.getByRole('link', {name: /Example Dictionary/})).toBeInTheDocument());
}}>
  <HomeViewStory loggedIn otherServer />
</Story>

<Story name="Server refresh keeps rows" play={async ({canvasElement}) => {
  await expectRowsStay(canvasElement, ['Sena 3', 'Example Dictionary', 'Training Project', 'Login to see projects'],
    () => userEvent.click(lexbox(canvasElement).getByRole('button', {name: 'Refresh Projects'})),
    () => Promise.resolve());
}}>
  <HomeViewStory loggedIn otherServer />
</Story>

<Story name="Delete keeps other rows" play={async ({canvasElement}) => {
  const canvas = within(canvasElement);
  const page = within(document.body);
  await expectRowsStay(canvasElement, ['Example Dictionary', 'Training Project', 'Login to see projects'],
    async () => {
      await userEvent.pointer({keys: '[MouseRight]', target: await canvas.findByRole('link', {name: /Sena 3/})});
      await userEvent.click(await page.findByRole('menuitem', {name: 'Delete'}));
      await userEvent.click(await page.findByRole('switch'));
      await userEvent.click(page.getByRole('button', {name: 'Delete Project'}));
    },
    () => expect(lexbox(canvasElement).getByText('Sena 3')).toBeInTheDocument());
}}>
  <HomeViewStory loggedIn otherServer />
</Story>
