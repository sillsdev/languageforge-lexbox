<script module lang="ts">
  import StompSafeLcmRichTextEditor from '$lib/components/stomp/stomp-safe-lcm-rich-text-editor.svelte';
  import {asString} from '$project/data';
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import {expect, fn, userEvent, within} from 'storybook/test';
  import {tick} from 'svelte';

  let value = $state({
    spans: [
      span('seh definition', 'seh'),
      span('fr definition', 'fr'),
    ],
  });

  function span(text: string, ws = 'seh') {
    return { text, ws };
  }

  const { Story } = defineMeta({
    component: StompSafeLcmRichTextEditor,
    argTypes: {
      readonly: {
        control: { type: 'boolean' },
      },
    },
    args: {
      onchange: fn(),
      value: {
        spans: [span('seh definition', 'seh')],
      },
      readonly: false,
    },
  });

  function focusAtEnd(input: HTMLDivElement) {
    input.focus();
    const range = document.createRange();
    range.selectNodeContents(input);
    range.collapse(false); // Move caret to end
    const sel = window.getSelection()!;
    sel.removeAllRanges();
    sel.addRange(range);
  }
</script>

<Story
  name="Prevents change stomping"
  play={async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const input = await canvas.findByRole<HTMLDivElement>('textbox');
    await expect(input).toBeInTheDocument();
    focusAtEnd(input);
    await userEvent.type(input, ' new text', { delay: 10 }); // dirty => locked
    await tick();
    await expect(input.textContent).toMatch(/new text$/);
    value = { spans: [span('A different value')] }; // parent change while locked
    await tick();
    await expect(input.textContent).toMatch(/new text$/); // ignored
    await expect(asString(value)).toMatch(/new text$/); // ignored
    input.blur(); // commit => unlock
    await tick();
    value = { spans: [span('A different value')] }; // parent change while unlocked
    await tick();
    await expect(input.textContent).toBe('A different value'); // accepted
  }}
>
  {#snippet template({ value: _, ...args })}
    {JSON.stringify($state.snapshot(value), null, 2)}
    <StompSafeLcmRichTextEditor bind:value {...args} />
  {/snippet}
</Story>
<!-- Regression test for https://github.com/sillsdev/languageforge-lexbox/issues/1871 -->
<Story
  name="Can type into empty fields"
  play={async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const input = await canvas.findByRole<HTMLDivElement>('textbox');
    await expect(input).toBeInTheDocument();
    await userEvent.clear(input);
    await expect(input.textContent).toBe('');
    input.click();
    await userEvent.type(input, 'abc', { delay: 10 });
    await expect(input.textContent).toBe('abc');
  }}
>
  {#snippet template({ value: _, ...args })}
    {JSON.stringify($state.snapshot(value), null, 2)}
    <StompSafeLcmRichTextEditor bind:value {...args} />
  {/snippet}
</Story>
