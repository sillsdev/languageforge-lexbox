<script lang="ts">
  import TusUpload from '$lib/components/TusUpload.svelte';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import { Button } from '$lib/forms';
  import { PageBreadcrumb } from '$lib/layout';

  function uploadFinished(): void {
    alert('upload done!');
  }

  async function fetch500(): Promise<Response> {
    return fetch('/api/testing/test500NoException');
  }

  async function fetch403(): Promise<Response> {
    return fetch('/api/AuthTesting/403');
  }
</script>
<PageBreadcrumb>Hello from sandbox</PageBreadcrumb>
<PageBreadcrumb>second value</PageBreadcrumb>
<h2 class="text-lg">Sandbox</h2>
<div class="grid gap-2 grid-cols-3">
  <div class="card w-96 bg-base-200 shadow-lg">
    <a rel="external" class="btn" href="/sandbox/403">Goto page load 403</a>
    <a rel="external" target="_blank" class="btn" href="/sandbox/403">Goto page load 403 new tab</a>
    <a rel="external" class="btn" href="/api/AuthTesting/403">Goto API 403</a>
    <a rel="external" target="_blank" class="btn" href="/api/AuthTesting/403">Goto API 403 new tab</a>
    <button class="btn" on:click={fetch403}>Fetch 403</button>

    <div class="divider" />

    <a rel="external" class="btn" href="/sandbox/500">Goto page load 500</a>
    <a rel="external" target="_blank" class="btn" href="/sandbox/500">Goto page load 500 new tab</a>
    <a rel="external" class="btn" href="/api/testing/test500NoException">Goto API 500</a>
    <a rel="external" target="_blank" class="btn" href="/api/testing/test500NoException">Goto API 500 new tab</a>
    <button class="btn" on:click={fetch500}>Fetch 500</button>
  </div>
  <div class="card w-96 bg-base-200 shadow-lg">
    <div class="card-body">
      <TusUpload endpoint="/api/tus-test" accept="image/*" on:uploadComplete={uploadFinished}/>
    </div>
  </div>

  <div class="card w-96 bg-base-200 shadow-lg">
    <div class="card-body">
      <h2 class="card-title">Dropdown Example</h2>
      <Dropdown>
        <Button style="btn-primary">Open Me!</Button>
        <ul slot="content" class="menu bg-info rounded-box">
          <li><button>First item</button></li>
          <li><button>Second item</button></li>
        </ul>
      </Dropdown>
      <div>
        <Dropdown>
          <Button style="btn-primary">Open Me!</Button>
          <ul slot="content" class="menu bg-info rounded-box">
            <li><button>First item</button></li>
            <li><button>Second item</button></li>
          </ul>
        </Dropdown>
      </div>
    </div>
  </div>
</div>
