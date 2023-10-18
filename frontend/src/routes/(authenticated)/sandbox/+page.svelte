<script lang="ts">
  import TusUpload from '$lib/components/TusUpload.svelte';

  async function execute() {
    const response = await fetch('/api/Testing/testRedirect', {redirect: 'follow'});
    console.log(response);
  }
</script>

<button on:click={execute}>Test</button>
<div class="card w-96 bg-base-200 shadow-lg">
  <div class="card-body">
    <TusUpload/>
  </div>
</div>
