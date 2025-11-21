<script module lang="ts">
  import { writingSystems } from '$project/demo/demo-entry-data';
  import { defineMeta } from '@storybook/addon-svelte-csf';
  import { expect, fn, userEvent, within } from 'storybook/test';
  import MultiStringFieldDecorator from './MultiStringFieldDecorator.svelte';
  import { MultiWsInput } from '$lib/components/field-editors';
  import {tick} from 'svelte';

  const value = $state({
    pt: 'Example value in Portuguese',
  });

  const { Story } = defineMeta({
    component: MultiWsInput,
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
    await userEvent.type(porInput, ' new text');
    porInput.blur();
    await tick();
    await expect(args.onchange).toHaveBeenCalledOnce();
  }}
/>

<Story name="Raw" />
