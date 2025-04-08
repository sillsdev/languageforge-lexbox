<script lang="ts" module>
  import type {IconClass} from '$lib/icon-class';
  import {cn} from '$lib/utils';
  import type {WithElementRef, WithoutChildren} from 'bits-ui';
  import type {HTMLAttributes} from 'svelte/elements';

  export type IconProps = WithoutChildren<WithElementRef<HTMLAttributes<HTMLSpanElement>>> & {
    icon: IconClass;
  };
</script>

<script lang="ts">
  const { icon, class: className, ...restProps }: IconProps = $props();
</script>

<span {...restProps} class={cn('size-6 inline-block shrink-0', className, icon)}></span>
