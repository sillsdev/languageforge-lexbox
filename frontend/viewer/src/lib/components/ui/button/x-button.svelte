<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Icon} from '../icon';
  import type {ButtonProps} from './button.svelte';
  import Button from './button.svelte';
  import {cn} from '$lib/utils';

  const {
    class: className,
    ...restProps
  }: ButtonProps = $props();

</script>

<Button {...restProps} variant="ghost" size="xs-icon"
  class={cn('opacity-70 transition-opacity hover:opacity-100 ring-offset-background focus:ring-ring focus:outline-none focus:ring-2 disabled:pointer-events-none',
  className)}>
  <Icon icon="i-mdi-close"/>
  <span class="sr-only">{$t`Close`}</span>
</Button>
