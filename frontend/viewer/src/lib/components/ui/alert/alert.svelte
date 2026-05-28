<script lang="ts" module>
  import {type VariantProps, tv} from 'tailwind-variants';

  export const alertVariants = tv({
    base: 'relative grid w-full grid-cols-[0_1fr] items-start gap-y-0.5 rounded-lg border px-4 py-3 text-sm has-[>[class*="i-mdi-"]]:grid-cols-[calc(var(--spacing)*4)_1fr] has-[>[class*="i-mdi-"]]:gap-x-3 [&>[class*="i-mdi-"]]:size-4 [&>[class*="i-mdi-"]]:translate-y-0.5 [&>[class*="i-mdi-"]]:text-current',
    variants: {
      variant: {
        default: 'bg-card text-card-foreground',
        destructive: 'text-destructive-foreground bg-destructive *:data-[slot=alert-description]:text-current',
      },
    },
    defaultVariants: {
      variant: 'default',
    },
  });

  export type AlertVariant = VariantProps<typeof alertVariants>['variant'];
</script>

<script lang="ts">
  import type {HTMLAttributes} from 'svelte/elements';
  import {cn, type WithElementRef} from '$lib/utils.js';

  let {
    ref = $bindable(null),
    class: className,
    variant = 'default',
    children,
    ...restProps
  }: WithElementRef<HTMLAttributes<HTMLDivElement>> & {
    variant?: AlertVariant;
  } = $props();
</script>

<div bind:this={ref} data-slot="alert" role="alert" class={cn(alertVariants({variant}), className)} {...restProps}>
  {@render children?.()}
</div>
