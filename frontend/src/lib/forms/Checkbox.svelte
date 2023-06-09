<script lang="ts">
  export let label: string;
  export let value: boolean;
</script>

<div class="form-control w-full">
  <label class="label cursor-pointer">
    <span class="label-text">{label}</span>

    <input type="checkbox" bind:checked={value} class="checkbox" />
  </label>
</div>
