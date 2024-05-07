<script lang="ts">
  import { mdiArrowDownThick, mdiArrowUpThick, mdiTrashCanOutline } from "@mdi/js";
  import { createEventDispatcher } from "svelte";
  import { Button, ButtonGroup } from "svelte-ux";

  const dispatch = createEventDispatcher<{
    moveDown: void;
    moveUp: void;
    delete: void;
  }>();

  export let i: number;
  export let count: number;

  $: notFirst = i > 0;
  $: notLast = i < count - 1;

</script>
<ButtonGroup color="primary">
  {#if notFirst}
    <Button on:click={() => dispatch('moveDown')} variant="fill-light" color="info" rounded icon={mdiArrowUpThick} size="sm"></Button>
  {/if}
  {#if notLast}
    <Button on:click={() => dispatch('moveUp')} variant="fill-light" color="info" rounded icon={mdiArrowDownThick} size="sm"></Button>
  {/if}
  <Button on:click={() => dispatch('delete')} variant="fill-light" rounded icon={mdiTrashCanOutline} color="danger" size="sm"></Button>
</ButtonGroup>
