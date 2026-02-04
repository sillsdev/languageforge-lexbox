<script lang="ts" module>
  import type {IconClass} from '$lib/icon-class';
  import {cn} from '$lib/utils';
  import type {WithElementRef, WithoutChildren} from 'bits-ui';
  import type {HTMLAttributes, HTMLImgAttributes} from 'svelte/elements';
  import {tv} from 'tailwind-variants';

  export const iconVariants = tv({
    base: 'inline-block shrink-0',
    variants: {
      size: {
        default: 'size-6',
      },
    },
    defaultVariants: {
      size: 'default',
    },
  });

  export type IconProps = WithoutChildren<WithElementRef<HTMLAttributes<HTMLSpanElement>>> &
    ({icon: IconClass} | ({src: string; alt: string} & HTMLImgAttributes));
</script>

<script lang="ts">
  const {class: className, ...restProps}: IconProps = $props();
</script>

{#if 'src' in restProps}
  <img {...restProps} class={cn(iconVariants(), className)} />
{:else}
  <span {...restProps} class={cn(iconVariants(), className, restProps.icon)}>
    <!-- ensures that baseline alignment works for consumers of this component -->
    &nbsp;
  </span>
{/if}
