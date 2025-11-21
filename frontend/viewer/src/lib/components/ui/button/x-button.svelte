<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import type {ButtonProps} from './button.svelte';
  import Button from './button.svelte';
  import {cn} from '$lib/utils';

  const {
    class: className,
    ...restProps
  }: ButtonProps = $props();

</script>

<Button icon="i-mdi-close" variant="ghost" size="xs-icon" aria-label={$t`Close`}
  class={cn('bg-background opacity-70 transition-opacity hover:opacity-100 ring-offset-background focus:ring-ring focus:outline-none focus:ring-2 disabled:pointer-events-none',
  className)} {...restProps} />
