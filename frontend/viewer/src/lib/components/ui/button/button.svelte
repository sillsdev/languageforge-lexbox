<script lang="ts" module>
  import type {IconClass} from '$lib/icon-class';
  import {mergeProps, type WithElementRef} from 'bits-ui';
  import type {ComponentProps} from 'svelte';
  import type {HTMLAnchorAttributes, HTMLButtonAttributes} from 'svelte/elements';
  import {type VariantProps, tv} from 'tailwind-variants';
  import type {IconProps} from '../icon/icon.svelte';

  export const buttonVariants = tv({
    base: 'ring-offset-background focus-visible:ring-ring inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-md text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:[&:not(.loading)]:opacity-50 [&_svg]:pointer-events-none [&_svg]:size-4 [&_svg]:shrink-0',
    variants: {
      variant: {
        default: 'bg-primary text-primary-foreground hover:bg-primary/90',
        destructive: 'bg-destructive text-destructive-foreground hover:bg-destructive/90',
        outline: 'border-input bg-background hover:bg-accent hover:text-accent-foreground border',
        secondary: 'bg-secondary text-secondary-foreground hover:bg-secondary/80',
        ghost: 'hover:bg-accent hover:text-accent-foreground',
        link: 'text-primary underline-offset-4 hover:underline',
      },
      size: {
        default: 'h-10 px-4 py-2',
        xs: 'h-8 rounded-md px-2',
        sm: 'h-9 rounded-md px-3',
        lg: 'h-11 rounded-md px-8',
        'badge-icon': 'h-5 w-5 min-h-5 min-w-5 max-h-5 max-w-5',
        'xs-icon': 'h-8 w-8 min-h-8 min-w-8 max-h-8 max-w-8',
        'sm-icon': 'h-9 w-9 min-h-9 min-w-9 max-h-9 max-w-9',
        icon: 'h-10 w-10 min-h-10 min-w-10 max-h-10 max-w-10',
        'extended-fab': 'h-14 pl-4 pr-5',
        fab: 'h-14 px-4',
        'xl-icon': 'h-16 w-16 min-h-16 min-w-16 max-h-16 max-w-16',
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
  import {cn} from '$lib/utils.js';
  import {Icon} from '../icon';
  import {slide} from 'svelte/transition';
  import Anchor from '../anchor/anchor.svelte';

  let {
    class: className,
    variant = 'default',
    size = 'default',
    ref = $bindable(null),
    href = undefined,
    type = 'button',
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
  <Anchor bind:ref class={cn(buttonVariants({variant, size}), className, loading && 'loading')} {href} {...restProps}>
    {@render content()}
  </Anchor>
{:else}
  <button
    bind:this={ref}
    class={cn(buttonVariants({variant, size}), className, loading && 'loading')}
    {type}
    {...restProps}
    disabled={restProps.disabled || loading}
  >
    {@render content()}
  </button>
{/if}
