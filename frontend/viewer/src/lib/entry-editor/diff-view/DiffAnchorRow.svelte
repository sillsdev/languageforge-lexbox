<script lang="ts">
  import DiffAnchor from './DiffAnchor.svelte';
  import type {AnchorText} from './anchor-text';

  let props: AnchorText = $props();
</script>

<!-- A primitive's own heading, for when nothing outside it (like the activity preview's change header) names the entity -->
<div class="col-span-full text-lg font-semibold">
  <DiffAnchor {...props} />
</div>
