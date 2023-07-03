<script lang="ts">
  import { randomFieldId } from './utils';

  export let label: string;
  export let value: boolean;
  export let id = randomFieldId();
</script>

<div class="form-control w-full">
  <label class="label cursor-pointer">
    <span class="label-text">{label}</span>
    <input {id} type="checkbox" bind:checked={value} class="checkbox" />
  </label>
</div>
