<script lang="ts">
  import WsInput from '$lib/components/field-editors/ws-input.svelte';
  import type { ComponentProps } from 'svelte';

  type Props = Omit<ComponentProps<typeof WsInput>, 'value'> & {
    value: { current: string | undefined }
  };

  let { value, ...rest }: Props = $props();
</script>

<WsInput
  bind:value={value.current}
  {...rest}
/>
