<script lang="ts" module>
  import type {IconClass} from '$lib/icon-class';
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLAnchorAttributes, HTMLButtonAttributes} from 'svelte/elements';
  import {type VariantProps, tv} from 'tailwind-variants';
  import type {IconProps} from '../icon/icon.svelte';

  export const buttonVariants = tv({
    base: 'ring-offset-background focus-visible:ring-ring inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-md text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 [&_svg]:pointer-events-none [&_svg]:size-4 [&_svg]:shrink-0',
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
        icon: 'h-10 w-10 min-h-10 min-w-10',
        'extended-fab': 'h-14 pl-4 pr-5',
        'fab': 'h-14 px-4'
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
    WithElementRef<HTMLAnchorAttributes> & {
      variant?: ButtonVariant;
      size?: ButtonSize;
      loading?: boolean;
      icon?: IconClass;
      iconProps?: Partial<IconProps>;
    };
</script>

<script lang="ts">
  import {cn} from '$lib/utils.js';
  import {Icon} from '../icon';

  let {
    class: className,
    variant = 'default',
    size = 'default',
    ref = $bindable(null),
    href = undefined,
    type = 'button',
    loading = false,
    icon = undefined,
    iconProps = undefined,
    children,
    ...restProps
  }: ButtonProps = $props();
</script>

{#snippet content()}
  {#if loading}
    <Icon icon="i-mdi-loading" class="animate-spin" />
  {:else if icon}
    <Icon {icon} {...iconProps} />
  {/if}
  {@render children?.()}
{/snippet}

{#if href}
  <a bind:this={ref} class={cn(buttonVariants({ variant, size }), className)} {href} {...restProps}>
    {@render content()}
  </a>
{:else}
  <button
    bind:this={ref}
    class={cn(buttonVariants({ variant, size }), className)}
    {type}
    {...restProps}
    disabled={restProps.disabled || loading}
  >
    {@render content()}
  </button>
{/if}
