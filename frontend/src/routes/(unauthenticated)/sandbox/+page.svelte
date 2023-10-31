<script lang="ts">
  import TusUpload from '$lib/components/TusUpload.svelte';


  function uploadFinished(): void {
      alert('upload done!');
  }

  async function fetch500(): Promise<Response> {
    return fetch('/api/testing/test500NoException');
  }
</script>
<div class="grid gap-2">
  <h2>Sandbox</h2>
  <div class="card w-96 bg-base-200 shadow-lg">
    <a rel="external" class="btn" href="/api/testing/test500NoException">Goto 500 page</a>
    <a rel="external" target="_blank" class="btn" href="/api/testing/test500NoException">Goto 500 new tab</a>
    <button class="btn" on:click={fetch500}>Fetch 500</button>
  </div>
  <div class="card w-96 bg-base-200 shadow-lg">
    <div class="card-body">
      <TusUpload endpoint="/api/tus-test" accept="image/*" on:uploadComplete={uploadFinished}/>
    </div>
  </div>
</div>

