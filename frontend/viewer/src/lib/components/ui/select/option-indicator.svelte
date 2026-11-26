<script lang="ts">
  import {Icon} from '../icon';
  import {cn} from '$lib/utils.js';

  let {state, class: className}: {state: boolean | 'some'; class?: string} = $props();
</script>

<Icon
  icon={state === 'some' ? 'i-mdi-minus' : 'i-mdi-check'}
  class={cn('ms-auto size-4 shrink-0', state || 'invisible', className)}
  aria-hidden="true"
/>
