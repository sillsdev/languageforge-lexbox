<script lang="ts">
  import {untrack, type ComponentProps} from 'svelte';
  import {Input} from '../ui/input';
  import {StompGuard} from './stomp-guard.svelte';
  import {mergeProps} from 'bits-ui';
  import {useIdleService} from '$lib/services/idle-service';

  type Props = ComponentProps<typeof Input> & {onchange: () => void};

  let {value = $bindable(), onchange, ...rest}: Props = $props();

  let idleService = useIdleService();

  const guard = new StompGuard(
    () => value,
    (newValue) => (value = newValue),
  );

  function onIdle() {
    if (guard.isDirty) {
      guard.commitAndUnlock();
      onchange();
    }
  }

  $effect(() => {
    if (idleService.isIdle) untrack(onIdle);
  });
</script>

<Input
  bind:value={guard.value}
  {...mergeProps(
    {
      onchange: () => {
        guard.commitAndUnlock();
      },
    },
    {onchange},
    rest,
  )}
/>
