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
        ghost: 'outline-none min-w-0 bg-transparent selection:bg-primary selection:text-primary-foreground',
        // shell variant has a dedicated component. Users should apply the class "real-input" to a wrapped <input> to enable focus styles on the shell.
        shell: cn(
          'border-input bg-background selection:bg-primary dark:bg-input/30 selection:text-primary-foreground ring-offset-background placeholder:text-muted-foreground flex h-10 w-full min-w-0 rounded-md border text-base shadow-xs transition-[color,box-shadow] outline-none has-disabled:cursor-not-allowed has-disabled:opacity-50 md:text-sm',
          'flex gap-2 items-center justify-between',
          'has-[.real-input:focus-visible]:border-ring has-[.real-input:focus-visible]:ring-ring/50 has-[.real-input:focus-visible]:ring-[3px]',
          'has-[.real-input[aria-invalid=true]]:ring-destructive/20 dark:has-[.real-input[aria-invalid=true]]:ring-destructive/40 has-[.real-input[aria-invalid=true]]:border-destructive',
        ),
      },
      visibleFocus: {
        off: '',
        on: 'focus:border-ring focus:ring-ring/50 focus:ring-[3px]',
      },
    },
    defaultVariants: {
      variant: 'default',
      visibleFocus: 'off',
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
    autocapitalize="off"
    {...restProps}
  />
{/if}
