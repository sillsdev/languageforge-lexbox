<script lang="ts">
  import {Checkbox as CheckboxPrimitive} from 'bits-ui';

  let {
    value = $bindable<string[]>([]),
    ...restProps
  }: CheckboxPrimitive.GroupProps = $props();
</script>

<CheckboxPrimitive.Group bind:value {...restProps} />
