<script lang="ts">
  import TusUpload from '$lib/components/TusUpload.svelte';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import {Button, Form, Input, lexSuperForm, SubmitButton} from '$lib/forms';
  import { PageBreadcrumb } from '$lib/layout';
  import z from 'zod';
  // eslint-disable-next-line no-restricted-imports
  import { t as otherT } from 'svelte-intl-precompile';
  import t from '$lib/i18n';

  function uploadFinished(): void {
    alert('upload done!');
  }

  async function fetch500(): Promise<Response> {
    return fetch('/api/testing/test500NoException');
  }

  async function fetch403(): Promise<Response> {
    return fetch('/api/AuthTesting/403');
  }
  const formSchema = z.object(
    {
      name: z.string().min(3).max(255),
      lastName: z.string().min(3).max(255),
    }
  );

  // eslint-disable-next-line @typescript-eslint/require-await
  let {form, enhance, errors} = lexSuperForm(formSchema, async () => {
      console.log('submit', $form);
  });
function preFillForm(): void {
  form.update(f => ({...f, name: 'John'}), {taint: false});
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
      <TusUpload internalButton endpoint="/api/tus-test" accept="image/*" on:uploadComplete={uploadFinished}/>
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
  <div class="card w-96 bg-base-200 shadow-lg">
    <div class="card-body">
      <h2 class="card-title">Form Example</h2>
      <Form {enhance}>
        <Input
          id="name"
          label="Name"
          type="text"
          error={$errors.name}
          bind:value={$form.name}
          autofocus
        />
        <Input
          id="lastName"
          label="Last Name"
          type="text"
          error={$errors.lastName}
          bind:value={$form.lastName}
          autofocus
        />
        <SubmitButton>Submit</SubmitButton>
        <Button style="btn-outline" on:click={preFillForm}>Pre fill</Button>
      </Form>
    </div>
  </div>
  <div class="card bg-base-200 shadow-lg col-span-2">
    <div class="card-body">
      <h2 class="card-title">Translations of login.title</h2>
      <div>Current: {$t('login.title')}</div>
      <div>English: {$otherT('login.title', { locale: 'en'})} (always works, because it's the fallback)</div>
      <div>French: {$otherT('login.title', { locale: 'fr'})} (only works if it's the current, otherwise uses fallback)</div>
      <div>Spanish: {$otherT('login.title', { locale: 'es'})} (only works if it's the current, otherwise uses fallback)</div>
    </div>
  </div>
</div>
