<script lang="ts">
  import type { ComponentProps } from 'svelte';
  import FieldHarness from './FieldDecorator.svelte';

  let { ...rest }: Omit<ComponentProps<typeof FieldHarness>, 'subGrid'> = $props();

</script>

<FieldHarness subGrid {...rest} />
