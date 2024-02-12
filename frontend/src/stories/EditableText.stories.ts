import EditableText from '$lib/components/EditableText.svelte';
import type { Meta, StoryObj } from '@storybook/svelte';

// More on how to set up stories at: https://storybook.js.org/docs/writing-stories
const meta = {
  title: 'Example/EditableText',
  component: EditableText,
  tags: ['autodocs'],
  argTypes: {
    value: { control: 'Project name' },
  },
  parameters: {
    sveltekit_experimental: {
      stores: {
        page: {
        },
      },
      forms: {
        enhance: function () { console.log(arguments);
      },
      }
    },
  },
} satisfies Meta<EditableText>;

export default meta;
type Story = StoryObj<typeof meta>;

// More on writing stories with args: https://storybook.js.org/docs/writing-stories/args
export const input: Story = {
  args: {
    saveHandler: (value: string): Promise<void> => {
      console.log('saveHandler', value);
      return Promise.resolve();
    }
  },
};

// More on writing stories with args: https://storybook.js.org/docs/writing-stories/args
export const textArea: Story = {
  args: {
    multiline: true,
    saveHandler: (value: string): Promise<void> => {
      console.log('saveHandler', value);
      return Promise.resolve();
    }
  },
};
