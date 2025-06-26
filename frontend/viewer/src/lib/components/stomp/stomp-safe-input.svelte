<script lang="ts">
  import type { ComponentProps } from 'svelte';
  import {Input} from '../ui/input';
  import {StompGuard} from './stomp-guard.svelte';
  import {mergeProps} from 'bits-ui';

  type Props = ComponentProps<typeof Input>;

  let { value = $bindable(), ...rest}: Props = $props();

  const guardedValue = new StompGuard(
    () => value,
    (newValue) => value = newValue
  );
</script>

<Input bind:value={guardedValue.value} {...mergeProps({
  onchange: () => {
    guardedValue.commitAndUnlock();
  }
}, rest)} />
