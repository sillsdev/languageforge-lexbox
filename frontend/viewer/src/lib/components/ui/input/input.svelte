<script lang="ts" module>
  import type {WithElementRef, WithoutChildren} from 'bits-ui';
  import type {HTMLInputAttributes, HTMLInputTypeAttribute} from 'svelte/elements';
  import {type VariantProps, tv} from 'tailwind-variants';

  export const inputVariants = tv({
    base: 'font-normal',
    variants: {
      variant: {
        default:
          'border-input bg-background ring-offset-background placeholder:text-muted-foreground focus-visible:ring-ring flex h-10 w-full rounded-md border px-3 py-2 text-base file:border-0 file:bg-transparent file:text-sm file:font-medium focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 md:text-sm',
        ghost: 'outline-none min-w-0 bg-transparent',
        // shell variant has a dedicated component
        shell:
          'border-input bg-background ring-offset-background placeholder:text-muted-foreground flex h-10 w-full rounded-md border text-base has-[:disabled]:cursor-not-allowed has-[:disabled]:opacity-50 md:text-sm flex gap-2 items-center justify-between',
      },
    },
    defaultVariants: {
      variant: 'default',
    },
  });

  type InputType = Exclude<HTMLInputTypeAttribute, 'file'>;
  export type InputVariant = VariantProps<typeof inputVariants>['variant'];

  export type InputProps = WithoutChildren<
    WithElementRef<
      Omit<HTMLInputAttributes, 'type'> &
        ({type: 'file'; files?: FileList} | {type?: InputType; files?: undefined}) & {
          variant?: Exclude<InputVariant, 'shell'>; // shell variant has a dedicated component
        }
    >
  >;
</script>

<script lang="ts">
  import {cn} from '$lib/utils.js';

  let {
    ref = $bindable(null),
    value = $bindable(),
    type,
    files = $bindable(),
    class: className,
    variant = 'default',
    ...restProps
  }: InputProps = $props();
</script>

{#if type === 'file'}
  <input
    bind:this={ref}
    class={cn(inputVariants({variant}), className)}
    type="file"
    bind:files
    bind:value
    {...restProps}
  />
{:else}
  <input
    bind:this={ref}
    class={cn(inputVariants({variant}), className)}
    {type}
    bind:value
    spellcheck="false"
    autocapitalize="off"
    {...restProps}
  />
{/if}
