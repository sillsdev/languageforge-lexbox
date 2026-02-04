<script lang="ts">
  import {onDestroy, untrack, type ComponentProps} from 'svelte';
  import {StompGuard} from './stomp-guard.svelte';
  import {mergeProps} from 'bits-ui';
  import LcmRichTextEditor from '../lcm-rich-text-editor/lcm-rich-text-editor.svelte';
  import {useIdleService} from '$lib/services/idle-service';
  import {useRootLocation} from '$lib/services/root-location-service';

  type Props = ComponentProps<typeof LcmRichTextEditor> & {onchange: () => void};

  let {value = $bindable(), onchange, ...rest}: Props = $props();

  const locationService = useRootLocation();

  const guard = new StompGuard(
    () => value,
    (newValue) => (value = newValue),
  );

  function commitAnyChanges() {
    if (guard.isDirty) {
      guard.commitAndUnlock();
      onchange();
    }
  }

  let idleService = useIdleService();
  $effect(() => {
    if (idleService.isIdle) untrack(commitAnyChanges);
  });
  onDestroy(() => {
    // This is just a precaution. I'm not aware of a scenario where we actually need to commit at this point.
    commitAnyChanges();

    return locationService.subscribe(() => {
      // This handler is required, because the contenteditable blur event is too late when blurring due to navigation.
      // Calling subscribe seems to be the only reliable way to get the callback triggered in that case.
      commitAnyChanges();
    });
  });
</script>

<LcmRichTextEditor
  bind:value={guard.value}
  {...mergeProps(
    {
      onchange: () => {
        commitAnyChanges();
      },
    },
    rest,
  )}
/>
