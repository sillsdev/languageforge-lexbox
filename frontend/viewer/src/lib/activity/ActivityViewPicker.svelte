<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import ResponsivePopup from '$lib/components/responsive-popup/responsive-popup.svelte';
  import {Button} from '$lib/components/ui/button';
  import ViewPicker from '$project/browse/ViewPicker.svelte';

  let open = $state(false);
</script>

<ResponsivePopup bind:open>
  {#snippet trigger({props})}
    <Button {...props} size="icon-xs" variant="ghost" icon="i-mdi-layers" title={$t`View options`} />
  {/snippet}
  <ViewPicker onClose={() => open = false} />
</ResponsivePopup>
