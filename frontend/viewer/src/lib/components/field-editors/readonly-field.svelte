<script lang="ts">
  import type {ComponentProps} from 'svelte';
  import {inputVariants} from '../ui/input/input.svelte';
  import {tryUseFieldBody} from '../editor/field/field-root.svelte';
  import BadgeList from './badge-list.svelte';

  const labelledBy = tryUseFieldBody()?.labelId;

  const props: ComponentProps<typeof BadgeList> = $props();
</script>

<div
  role="textbox"
  aria-readonly="true"
  aria-labelledby={labelledBy}
  tabindex="0"
  class={[inputVariants(), 'h-auto min-h-10']}
>
  <BadgeList {...props} />
</div>
