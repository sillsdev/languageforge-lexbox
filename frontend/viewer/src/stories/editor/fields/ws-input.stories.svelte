<script module lang="ts">
  import { writingSystems } from '$project/demo/demo-entry-data';
  import { defineMeta } from '@storybook/addon-svelte-csf';
  import { expect, fn, userEvent, within } from 'storybook/test';
  import FieldDecorator from './FieldDecorator.svelte';
  import WsInputWrapper from './WsInputWrapper.svelte';
  import {tick} from 'svelte';

  let value = $state({ current: 'A fun example value' });

  const { Story } = defineMeta({
    component: WsInputWrapper,
    argTypes: {
      readonly: {
        control: { type: 'boolean' },
      },
    },
    args: {
      onchange: fn(),
      value,
      readonly: false,
      writingSystem: Object.freeze(writingSystems.analysis[1]),
    },
  });
</script>

<Story
  name="In editor"
  decorators={[
    /* @ts-expect-error Bug in Storybook https://github.com/storybookjs/storybook/issues/29951 */
    () => FieldDecorator,
  ]}
  play={async ({ canvasElement, args }) => {
    const canvas = within(canvasElement);
    const porInput = await canvas.findByLabelText('Demo field');
    await expect(porInput).toBeInTheDocument();
    await userEvent.type(porInput, ' new text');
    porInput.blur();
    await tick();
    await expect(args.onchange).toHaveBeenCalledOnce();
  }}
/>

<Story name="Raw" />
