<script lang="ts" module>
  import type {IconClass} from '$lib/icon-class';
  import {mergeProps} from 'bits-ui';
  import type {ComponentProps} from 'svelte';
  import {cn, type WithElementRef} from '$lib/utils.js';
  import type {HTMLAnchorAttributes, HTMLButtonAttributes} from 'svelte/elements';
  import {type VariantProps, tv} from 'tailwind-variants';
  import type {IconProps} from '../icon/icon.svelte';
  import Anchor from '../anchor/anchor.svelte';

  export const buttonVariants = tv({
    base: 'focus-visible:ring-ring/50 aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 inline-flex shrink-0 items-center justify-center gap-2 rounded-md text-sm font-medium whitespace-nowrap transition-all outline-none focus-visible:ring-[3px] disabled:pointer-events-none disabled:[&:not(.loading)]:opacity-50 aria-disabled:pointer-events-none aria-disabled:[&:not(.loading)]:opacity-50 [&_[class*="i-mdi-"]]:pointer-events-none [&>[class*="i-mdi-"]]:shrink-0 [&_[class*="i-mdi-"]:not([class*="size-"])]:size-4',
    variants: {
      variant: {
        default: 'bg-primary text-primary-foreground hover:bg-primary/90 shadow-xs',
        destructive:
          'bg-destructive hover:bg-destructive/90 focus-visible:ring-destructive/20 dark:focus-visible:ring-destructive/40 dark:bg-destructive/60 text-white shadow-xs',
        /* all border classes have been moved here, because they were/are the only variant that actually uses them */
        outline:
          'bg-background hover:bg-accent hover:text-accent-foreground dark:bg-input/30 focus-visible:border-ring border border-input aria-invalid:border-destructive dark:hover:bg-input/50 shadow-xs',
        secondary: 'bg-secondary text-secondary-foreground hover:bg-secondary/80 shadow-xs',
        ghost: 'hover:bg-accent hover:text-accent-foreground dark:hover:bg-accent/50',
        link: 'text-primary underline-offset-4 hover:underline',
      },
      size: {
        // Reduce padding at the start if the button's icon prop is being used OR at the end if an icon was added as children content at the end
        default:
          'h-10 px-4 py-2 has-[>span:first-child>:first-child[class*="i-mdi-"]]:ps-2 has-[>:last-child[class*="i-mdi-"]]:pe-2',
        xs: 'h-8 gap-1.5 rounded-md px-3 has-[>span:first-child>:first-child[class*="i-mdi-"]]:ps-1.5 has-[>:last-child[class*="i-mdi-"]]:pe-1.5',
        sm: 'h-9 gap-1.5 rounded-md px-3 has-[>span:first-child>:first-child[class*="i-mdi-"]]:ps-1.5 has-[>:last-child[class*="i-mdi-"]]:pe-1.5',
        lg: 'h-11 rounded-md px-8 has-[>span:first-child>:first-child[class*="i-mdi-"]]:ps-6 has-[>:last-child[class*="i-mdi-"]]:pe-6',
        'extended-fab': 'h-14 ps-4 pe-5',
        icon: 'h-10 w-10 min-h-10 min-w-10 max-h-10 max-w-10',
        'icon-xs': 'h-8 w-8 min-h-8 min-w-8 max-h-8 max-w-8',
        'icon-sm': 'h-9 w-9 min-h-9 min-w-9 max-h-9 max-w-9',
        'icon-xl': 'h-16 w-16 min-h-16 min-w-16 max-h-16 max-w-16',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'default',
    },
  });

  export type ButtonVariant = VariantProps<typeof buttonVariants>['variant'];
  export type ButtonSize = VariantProps<typeof buttonVariants>['size'];

  export type ButtonProps = WithElementRef<HTMLButtonAttributes> &
    WithElementRef<HTMLAnchorAttributes> &
    Omit<ComponentProps<typeof Anchor>, 'variant'> & {
      variant?: ButtonVariant;
      size?: ButtonSize;
      loading?: boolean;
      icon?: IconClass;
      iconProps?: Partial<IconProps>;
      autofocus?: boolean;
    };
</script>

<script lang="ts">
  import {Icon} from '../icon';
  import {slide} from 'svelte/transition';

  let {
    class: className,
    variant = 'default',
    size = 'default',
    ref = $bindable(null),
    href = undefined,
    type = 'button',
    disabled,
    loading = false,
    icon = undefined,
    iconProps: nullableIconProps = undefined,
    autofocus = false,
    children,
    ...restProps
  }: ButtonProps = $props();

  const iconProps = $derived(
    nullableIconProps && ('icon' in nullableIconProps || 'src' in nullableIconProps)
      ? (nullableIconProps as IconProps)
      : icon
        ? {icon, ...nullableIconProps}
        : undefined,
  );
  $effect(() => {
    if (autofocus && ref) {
      ref.focus();
    }
  });
</script>

{#snippet content()}
  {#if loading || iconProps}
    <span transition:slide={{axis: 'x'}}>
      {#if loading}
        <Icon {...mergeProps({class: 'animate-spin align-middle'}, iconProps ?? {})} icon="i-mdi-loading" />
      {:else if iconProps}
        <Icon {...iconProps} class={cn('align-middle', iconProps.class)} />
      {/if}
    </span>
  {/if}

  {@render children?.()}
{/snippet}

{#if href}
  <Anchor
    bind:ref
    data-slot="button"
    class={cn(buttonVariants({variant, size}), className, loading && 'loading')}
    href={disabled ? undefined : href}
    aria-disabled={disabled}
    role={disabled ? 'link' : undefined}
    tabindex={disabled ? -1 : undefined}
    {...restProps}
  >
    {@render content()}
  </Anchor>
{:else}
  <button
    bind:this={ref}
    data-slot="button"
    class={cn(buttonVariants({variant, size}), className, loading && 'loading')}
    {type}
    disabled={disabled || loading}
    {...restProps}
  >
    {@render content()}
  </button>
{/if}
