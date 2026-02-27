<script lang="ts" module>
  import {cn, type WithElementRef, type WithoutChildren} from '$lib/utils.js';
  import type {HTMLInputAttributes, HTMLInputTypeAttribute} from 'svelte/elements';
  import {type VariantProps, tv} from 'tailwind-variants';

  export const inputVariants = tv({
    base: 'font-normal',
    variants: {
      variant: {
        default: cn(
          'border-input bg-background selection:bg-primary dark:bg-input/30 selection:text-primary-foreground ring-offset-background placeholder:text-muted-foreground flex h-10 w-full min-w-0 rounded-md border px-3 py-2 text-base shadow-xs transition-[color,box-shadow] outline-none disabled:cursor-not-allowed disabled:opacity-50 md:text-sm',
          'focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px]',
          'aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 aria-invalid:border-destructive',
        ),
        file: cn(
          'selection:bg-primary dark:bg-input/30 selection:text-primary-foreground border-input ring-offset-background placeholder:text-muted-foreground flex h-10 w-full min-w-0 rounded-md border bg-transparent px-3 pt-2 text-sm font-medium shadow-xs transition-[color,box-shadow] outline-none disabled:cursor-not-allowed disabled:opacity-50',
          'focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px]',
          'aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 aria-invalid:border-destructive',
        ),
        ghost: 'outline-none min-w-0 bg-transparent',
        // shell variant has a dedicated component
        shell:
          'border-input bg-background ring-offset-background placeholder:text-muted-foreground flex h-10 w-full rounded-md border text-base has-disabled:cursor-not-allowed has-disabled:opacity-50 md:text-sm flex gap-2 items-center justify-between',
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
  let {
    ref = $bindable(null),
    value = $bindable(),
    type,
    files = $bindable(),
    class: className,
    variant: specifiedVariant,
    'data-slot': dataSlot = 'input',
    ...restProps
  }: InputProps = $props();

  const variant = $derived(specifiedVariant ?? (type === 'file' ? 'file' : 'default'));
</script>

{#if type === 'file'}
  <input
    bind:this={ref}
    data-slot={dataSlot}
    class={cn(inputVariants({variant}), className)}
    type="file"
    bind:files
    bind:value
    {...restProps}
  />
{:else}
  <input
    bind:this={ref}
    data-slot={dataSlot}
    class={cn(inputVariants({variant}), className)}
    {type}
    bind:value
    spellcheck="false"
    {...restProps}
  />
{/if}
