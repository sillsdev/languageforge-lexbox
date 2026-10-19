<script lang="ts">
  import {Tooltip as TooltipPrimitive} from 'bits-ui';

  let {...restProps}: TooltipPrimitive.ProviderProps = $props();
</script>

<!-- Focus that arrives by click or from a closing dialog shouldn't open a tooltip; keyboard focus still does -->
<TooltipPrimitive.Provider ignoreNonKeyboardFocus {...restProps} />
