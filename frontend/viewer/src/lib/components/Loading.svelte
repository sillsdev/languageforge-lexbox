<script lang="ts">
  import {Icon} from '$lib/components/ui/icon';
  import {cn} from '$lib/utils';
  import type {HTMLAttributes} from 'svelte/elements';

  type Props = HTMLAttributes<HTMLSpanElement>;

  let {class: className, ...restProps}: Props = $props();
</script>

<Icon icon="i-mdi-loading" class={cn('animate-spin', className)} {...restProps} />
