<script lang="ts">
  import { MultiSelectField, } from 'svelte-ux';

  const options = [
    { name: 'One', value: 1 },
    { name: 'Two', value: 2 },
    { name: 'Three', value: 3 },
    { name: 'Four', value: 4 },
  ];

  let value = [3];
</script>

<MultiSelectField {options} {value} on:change={(e) => (value = e.detail.value)} />

<div class="bg-green-400 h-[3000px]"></div>
