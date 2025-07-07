<script lang="ts">
  import type { ComponentProps } from 'svelte';
  import {StompGuard} from './stomp-guard.svelte';
  import {mergeProps} from 'bits-ui';
  import LcmRichTextEditor from '../lcm-rich-text-editor/lcm-rich-text-editor.svelte';

  type Props = ComponentProps<typeof LcmRichTextEditor>;

  let { value = $bindable(), ...rest}: Props = $props();

  const guard = new StompGuard(
    () => value,
    (newValue) => value = newValue
  );
</script>

<LcmRichTextEditor bind:value={guard.value} {...mergeProps({
  onchange: () => {
    guard.commitAndUnlock();
  }
}, rest)} />
