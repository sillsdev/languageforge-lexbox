<script lang="ts">
  import TusUpload from '$lib/components/TusUpload.svelte';


  function uploadFinished(): void {
      alert('upload done!');
  }
</script>
<div class="grid gap-2">
  <h2>Sandbox</h2>
  <div class="card w-96 bg-base-200 shadow-lg">
    <a rel="external" class="btn" href="/api/testing/test500NoException">Goto 500 page</a>
  </div>
  <div class="card w-96 bg-base-200 shadow-lg">
    <div class="card-body">
      <TusUpload endpoint="/api/tus-test" accept="image/*" on:uploadComplete={uploadFinished}/>
    </div>
  </div>
</div>

