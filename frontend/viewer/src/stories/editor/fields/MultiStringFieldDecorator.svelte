<script lang="ts">
  import type { ComponentProps } from 'svelte';
  import FieldDecorator from './FieldDecorator.svelte';

  let { ...rest }: Omit<ComponentProps<typeof FieldDecorator>, 'subGrid'> = $props();

</script>

<FieldDecorator subGrid {...rest} />
