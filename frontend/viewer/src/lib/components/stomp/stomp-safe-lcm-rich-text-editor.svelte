<script lang="ts">
  import {untrack, type ComponentProps} from 'svelte';
  import {StompGuard} from './stomp-guard.svelte';
  import {mergeProps} from 'bits-ui';
  import LcmRichTextEditor from '../lcm-rich-text-editor/lcm-rich-text-editor.svelte';
  import {useIdleService} from '$lib/services/idle-service';

  type Props = ComponentProps<typeof LcmRichTextEditor> & { onchange: () => void };

  let { value = $bindable(), onchange, ...rest}: Props = $props();

  const guard = new StompGuard(
    () => value,
    (newValue) => value = newValue
  );

  let idleService = useIdleService();
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

<LcmRichTextEditor bind:value={guard.value} {...mergeProps({
  onchange: () => {
    guard.commitAndUnlock();
  }
}, { onchange }, rest)} />
