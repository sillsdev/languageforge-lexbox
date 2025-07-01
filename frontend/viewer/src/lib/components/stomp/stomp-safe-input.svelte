<script lang="ts">
  import type { ComponentProps } from 'svelte';
  import {Input} from '../ui/input';
  import {StompGuard} from './stomp-guard.svelte';
  import {mergeProps} from 'bits-ui';

  type Props = ComponentProps<typeof Input>;

  let { value = $bindable(), ...rest}: Props = $props();

  const guard = new StompGuard(
    () => value,
    (newValue) => value = newValue
  );
</script>

<Input bind:value={guard.value} {...mergeProps({
  onchange: () => {
    guard.commitAndUnlock();
  }
}, rest)} />
