<script lang="ts">
  import {Switch} from 'svelte-ux';

  export let value: boolean;
</script>

<!-- svelte-ignore a11y-label-has-associated-control -->
<label class="flex gap-2 items-center text-sm">
  <Switch bind:checked={value}
          color="neutral"/>
  Show empty fields
</label>
