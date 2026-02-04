<script lang="ts" module>
  import {type VariantProps, tv} from 'tailwind-variants';

  export const anchorVariants = tv({
    base: '',
    variants: {
      variant: {
        text: 'inline-flex items-center flex-nowrap gap-1 font-medium underline underline-offset-4 hover:text-primary',
      },
    },
  });

  export type AnchorVariant = VariantProps<typeof anchorVariants>['variant'];
</script>

<script lang="ts">
  import type {Snippet} from 'svelte';
  import {Link} from 'svelte-routing';
  import type {HTMLAnchorAttributes} from 'svelte/elements';
  import {cn} from '$lib/utils.js';

  type Props = HTMLAnchorAttributes & {
    children?: Snippet;
    ref?: HTMLElement | null;
    external?: boolean;
    variant?: AnchorVariant;
  };

  let {href, target, ref = $bindable(null), children, external, variant, class: className, ...rest}: Props = $props();
</script>

{#if target === '_blank' || external || !href}
  <!-- eslint-disable-next-line svelte/no-navigation-without-resolve -->
  <a bind:this={ref} {href} {target} class={cn(anchorVariants({variant}), className)} {...rest}>
    {@render children?.()}
  </a>
{:else}
  <div bind:this={ref} class="contents">
    <Link to={href} {target} class={cn(anchorVariants({variant}), className)} {...rest}>
      {@render children?.()}
    </Link>
  </div>
{/if}
