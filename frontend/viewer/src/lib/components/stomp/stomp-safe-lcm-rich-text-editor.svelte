<script lang="ts">
  import {untrack, type ComponentProps} from 'svelte';
  import {StompGuard} from './stomp-guard.svelte';
  import {mergeProps} from 'bits-ui';
  import LcmRichTextEditor from '../lcm-rich-text-editor/lcm-rich-text-editor.svelte';

  type Props = ComponentProps<typeof LcmRichTextEditor> & { userIsIdle: boolean, onchange: () => void };

  let { value = $bindable(), userIsIdle, onchange, ...rest}: Props = $props();

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

<LcmRichTextEditor bind:value={guard.value} {...mergeProps({
  onchange: () => {
    guard.commitAndUnlock();
  }
}, rest)} />
