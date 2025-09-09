<script module lang="ts">
  import { defineMeta } from '@storybook/addon-svelte-csf';
  import { expect, fn, userEvent, within } from 'storybook/test';
  import { Button, buttonVariants, type ButtonSize, type ButtonVariant } from '$lib/components/ui/button';

  // More on how to set up stories at: https://storybook.js.org/docs/writing-stories
  const { Story } = defineMeta({
    component: Button,
    // tags: ['autodocs'],
    argTypes: {
      disabled: {
        control: { type: 'boolean' },
      },
    },
    args: {
      onclick: fn(),
      disabled: false,
      loading: false,
    },
  });

  const variants = Object.keys(buttonVariants.variants.variant) as ButtonVariant[];
  const sizes = Object.keys(buttonVariants.variants.size) as ButtonSize[];
</script>

<Story name="All">
  {#snippet template(args)}
    <div class="flex gap-4 flex-col">
      <div>
        Variants:
        <div class="flex gap-2 flex-wrap">
          {#each variants as variant (variant)}
            <Button {variant} {...args}>{variant} button</Button>
          {/each}
        </div>
      </div>
      <div>
        Variants with icon:
        <div class="flex gap-2 flex-wrap">
          {#each variants as variant (variant)}
            <Button {variant} icon="i-mdi-auto-fix" {...args}></Button>
          {/each}
        </div>
      </div>
      <div>
        Sizes:
        <div class="flex gap-2 flex-wrap">
          {#each sizes as size (size)}
            <Button {size} {...args}>{size}</Button>
          {/each}
        </div>
      </div>
      <div>
        Sizes with icon:
        <div class="flex gap-2 flex-wrap">
          {#each sizes as size (size)}
            <Button {size} icon="i-mdi-auto-fix" {...args} />
          {/each}
        </div>
      </div>
    </div>
  {/snippet}
</Story>

<Story
  name="Standard"
  play={async ({ canvasElement, args }) => {
    const canvas = within(canvasElement);
    const loadingButton = await canvas.findByRole('button', { name: 'Standard' });
    await expect(loadingButton).toBeInTheDocument();
    await userEvent.click(loadingButton);
    await expect(args.onclick).toHaveBeenCalled();
  }}>Standard</Story
>

<Story
  name="Loading"
  args={{ loading: true }}
  play={async ({ canvasElement, args }) => {
    const canvas = within(canvasElement);
    const loadingButton = await canvas.findByRole('button', { name: 'Loading' });
    await expect(loadingButton).toBeInTheDocument();
    await userEvent.click(loadingButton, { pointerEventsCheck: 0 });
    await expect(args.onclick).not.toHaveBeenCalled();
  }}>Loading</Story
>

<Story
  name="Disabled"
  args={{ disabled: true }}
  play={async ({ canvasElement, args }) => {
    const canvas = within(canvasElement);
    const loadingButton = await canvas.findByRole('button', { name: 'Disabled' });
    await expect(loadingButton).toBeInTheDocument();
    await userEvent.click(loadingButton, { pointerEventsCheck: 0 });
    await expect(args.onclick).not.toHaveBeenCalled();
  }}>Disabled</Story
>
