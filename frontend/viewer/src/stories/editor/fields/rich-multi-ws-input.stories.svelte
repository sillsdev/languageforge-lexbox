<script module lang="ts">
  import RichMultiWsInput from '$lib/components/field-editors/rich-multi-ws-input.svelte';
  import { writingSystems } from '$project/demo/demo-entry-data';
  import { defineMeta } from '@storybook/addon-svelte-csf';
  import { expect, fn, userEvent, within } from 'storybook/test';
  import MultiStringFieldDecorator from './MultiStringFieldDecorator.svelte';
  import {tick} from 'svelte';

  const value = $state({
    pt: {
      spans: [
        { text: 'seh definition', ws: 'seh' },
        { text: 'fr definition', ws: 'fr' },
      ],
    }
  });

  const { Story } = defineMeta({
    component: RichMultiWsInput,
    argTypes: {
      readonly: {
        control: { type: 'boolean' },
      },
    },
    args: {
      onchange: fn(),
      value,
      readonly: false,
      writingSystems: Object.freeze(writingSystems.analysis),
    },
  });
</script>

<Story
  name="In editor"
  decorators={[
    /* @ts-expect-error Bug in Storybook https://github.com/storybookjs/storybook/issues/29951 */
    () => MultiStringFieldDecorator,
  ]}
  play={async ({ canvasElement, args }) => {
    const canvas = within(canvasElement);
    const porInput = await canvas.findByLabelText('Demo field Por');
    await expect(porInput).toBeInTheDocument();
    porInput.focus(); // Firefox needs this
    await userEvent.type(porInput, ' new text');
    porInput.blur();
    await tick();
    await expect(args.onchange).toHaveBeenCalledOnce();
  }}
/>

<Story name="Raw" />
