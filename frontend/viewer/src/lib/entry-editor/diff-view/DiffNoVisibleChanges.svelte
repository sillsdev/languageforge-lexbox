<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
</script>

<!-- The change only touched what a diff doesn't show, e.g. formatting. Styled like the preview's "Preview not available" -->
<div class="col-span-full text-muted-foreground text-center">{$t`No visible changes`}</div>
