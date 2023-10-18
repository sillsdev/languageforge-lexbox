<script lang="ts">
  import TusUpload from '$lib/components/TusUpload.svelte';

  async function execute(): Promise<void> {
    const response = await fetch('/api/Testing/testRedirect', {redirect: 'follow'});
    console.log(response);
  }

  function uploadFinished(): void {
      alert('upload done!');
  }
</script>

<button on:click={execute}>Test</button>
<div class="card w-96 bg-base-200 shadow-lg">
  <div class="card-body">
    <TusUpload endpoint="/api/tus-test" accept="image/*" on:uploadComplete={uploadFinished}/>
  </div>
</div>
