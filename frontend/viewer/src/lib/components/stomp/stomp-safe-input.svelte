<script lang="ts">
  import {untrack, type ComponentProps} from 'svelte';
  import {Input} from '../ui/input';
  import {StompGuard} from './stomp-guard.svelte';
  import {mergeProps} from 'bits-ui';

  type Props = ComponentProps<typeof Input> & { userIsIdle: boolean, onchange: () => void };

  let { value = $bindable(), userIsIdle = false, onchange, ...rest}: Props = $props();

  const guard = new StompGuard(
    () => value,
    (newValue) => value = newValue
  );

  function onIdle() {
    if (guard.isDirty) {
      onchange();
    }
  }

  $effect(() => {
    if (userIsIdle) untrack(onIdle);
  });
</script>

<Input bind:value={guard.value} {...mergeProps({
  onchange: () => {
    guard.commitAndUnlock();
  }
}, rest)} />
