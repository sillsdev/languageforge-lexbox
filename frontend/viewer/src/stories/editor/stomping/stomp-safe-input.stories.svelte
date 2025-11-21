<script module lang="ts">
  import { defineMeta } from '@storybook/addon-svelte-csf';
  import { expect, fn, userEvent, within } from 'storybook/test';
  import {tick} from 'svelte';
  import StompSafeInput from '$lib/components/stomp/stomp-safe-input.svelte';

  let value = $state('A fun example value');

  const { Story } = defineMeta({
    component: StompSafeInput,
    argTypes: {
      readonly: {
        control: { type: 'boolean' },
      },
    },
    args: {
      onchange: fn(),
      readonly: false,
    },
  });
</script>

<Story name="Prevents change stomping" play={async ({ canvasElement }) => {
  const canvas = within(canvasElement);
  const input = await canvas.findByRole<HTMLInputElement>('textbox');
  await expect(input).toBeInTheDocument();
  await userEvent.type(input, ' new text'); // dirty => locked
  await expect(input.value).toMatch(/new text$/);
  value = 'A different value'; // parent change while locked
  await tick();
  await expect(input.value).toMatch(/new text$/); // ignored
  await expect(value).toMatch(/new text$/); // ignored
  input.blur(); // commit => unlock
  value = 'A different value'; // parent change while unlocked
  await tick();
  await expect(input.value).toBe('A different value'); // accepted
}}>
  {#snippet template(args)}
    <StompSafeInput bind:value {...args} />
  {/snippet}
</Story>
