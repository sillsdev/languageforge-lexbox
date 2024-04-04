<script lang="ts">
  import TusUpload from '$lib/components/TusUpload.svelte';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import {Button, Form, Input, lexSuperForm, SubmitButton} from '$lib/forms';
  import {PageBreadcrumb} from '$lib/layout';
  import z from 'zod';
  // eslint-disable-next-line no-restricted-imports
  import {t as otherT} from 'svelte-intl-precompile';
  import t from '$lib/i18n';
  import {_gqlThrows500} from './+page';
  import ConfirmModal from '$lib/components/modals/ConfirmModal.svelte';
  import {delay} from '$lib/util/time';
  import DeleteModal from '$lib/components/modals/DeleteModal.svelte';
  import { type SingleUserTypeaheadResult } from '$lib/gql/typeahead-queries';
  import UserTypeahead from '$lib/forms/UserTypeahead.svelte';

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

  let disableDropdown = false;

  async function gqlThrows500(): Promise<void> {
    await _gqlThrows500();
  }

let modal: ConfirmModal;
let deleteModal: DeleteModal;

  let chosenUser: SingleUserTypeaheadResult;
  $: if (chosenUser) alert(`Typeahead chose ${chosenUser.name} (${chosenUser.id})`);
</script>
<PageBreadcrumb>Hello from sandbox</PageBreadcrumb>
<PageBreadcrumb>second value</PageBreadcrumb>
<h2 class="text-lg">Sandbox</h2>
<div class="grid gap-2 grid-cols-3">
  <div class="card w-96 bg-base-200 shadow-lg">
    <a rel="external" class="btn" href="/">Go home</a>
    <div class="divider"/>
    <a rel="external" class="btn" href="/sandbox/403">Goto page load 403</a>
    <a rel="external" target="_blank" class="btn" href="/sandbox/403">Goto page load 403 new tab</a>
    <a rel="external" class="btn" href="/api/AuthTesting/403">Goto API 403</a>
    <a rel="external" target="_blank" class="btn" href="/api/AuthTesting/403">Goto API 403 new tab</a>
    <button class="btn" on:click={fetch403}>Fetch 403</button>

    <div class="divider"/>

    <a rel="external" class="btn" href="/sandbox/500">Goto page load 500</a>
    <a rel="external" target="_blank" class="btn" href="/sandbox/500">Goto page load 500 new tab</a>
    <a rel="external" class="btn" href="/api/testing/test500NoException">Goto API 500</a>
    <a rel="external" target="_blank" class="btn" href="/api/testing/test500NoException">Goto API 500 new tab</a>
    <button class="btn" on:click={fetch500}>Fetch 500</button>
    <button class="btn" on:click={gqlThrows500}>GQL 500</button>
  </div>
  <div class="card w-96 bg-base-200 shadow-lg">
    <div class="card-body">
      <UserTypeahead label="User typeahead demo" bind:result={chosenUser} />
      <TusUpload internalButton endpoint="/api/tus-test" accept="image/*" on:uploadComplete={uploadFinished}/>
    </div>
  </div>

  <div class="card w-96 bg-base-200 shadow-lg">
    <div class="card-body">
      <h2 class="card-title">Dropdown Example</h2>
      <span>Clicking menu items inside dropdown should cause it to close</span>
      <Dropdown>
        <Button variant="btn-primary">Open Me!</Button>
        <ul slot="content" class="menu bg-info rounded-box">
          <li><button>First item</button></li>
          <li><button>Second item</button></li>
        </ul>
      </Dropdown>
      <div>
        <Dropdown>
          <Button variant="btn-primary">Open Me!</Button>
          <ul slot="content" class="menu bg-info rounded-box">
            <li><button>First item</button></li>
            <li><button>Second item</button></li>
          </ul>
        </Dropdown>
      </div>
      <div>
        <Dropdown disabled={disableDropdown}>
          <Button variant="btn-primary" disabled={disableDropdown}>Open dropdown</Button>
          <div slot="content" class="bg-neutral p-5">
            <p>Some content</p>
            <Button outline on:click={() => disableDropdown = true}>Disable myself</Button>
          </div>
        </Dropdown>

        <label class="cursor-pointer label gap-4">
          <span class="label-text">Disabled</span>
          <input bind:checked={disableDropdown} type="checkbox" class="toggle toggle-error"/>
        </label>
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
        />
        <Input
          id="lastName"
          label="Last Name"
          type="text"
          error={$errors.lastName}
          bind:value={$form.lastName}
        />
        <SubmitButton>Submit</SubmitButton>
        <Button outline on:click={preFillForm}>Pre fill</Button>
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
  <div class="card bg-base-200 shadow-lg">
    <div class="card-body grid-cols-2 grid justify-items-start">
      <h2 class="card-title col-span-2">Buttons</h2>
        <Button variant="btn-primary" on:click={() => alert('hello')}>Primary Button</Button>
        <Button variant="btn-primary" size="btn-sm" on:click={() => alert('hello')}>Primary Small Button</Button>
        <Button variant="btn-success" on:click={() => alert('hello')}>Success Button</Button>
        <Button variant="btn-error" on:click={() => alert('hello')}>Error Button</Button>
        <Button outline on:click={() => alert('hello')}>Outline Button</Button>
        <Button variant="btn-ghost" on:click={() => alert('hello')}>Ghost Button</Button>
        <Button variant="btn-primary" disabled on:click={() => alert('should not fire')}>
          Disabled Button
        </Button>
        <Button variant="btn-primary" loading on:click={() => alert('should not fire')}>
          Loading Button
        </Button>
        <Button variant="btn-primary" disabled loading on:click={() => alert('should not fire')}>
          Disabled Loading Button
        </Button>
    </div>
  </div>


  <div class="card bg-base-200 shadow-lg">
    <div class="card-body">
      <h2 class="card-title">Confirm Modal</h2>
      <ConfirmModal bind:this={modal} title="Confirm?"
                    submitText="Confirm"
                    submitIcon="i-mdi-hand-wave"
                    cancelText="Don't confirm">
        Would you like to confirm this modal?
      </ConfirmModal>
      <Button variant="btn-primary" on:click={async () => {
        const result = await modal.open(async () => delay(2000));
        if (result) alert('submitted')
      }}>
        Open Modal
      </Button>
    </div>
  </div>

  <div class="card bg-base-200 shadow-lg">
    <div class="card-body">
      <h2 class="card-title">Delete Modal</h2>
      <DeleteModal bind:this={deleteModal}
      entityName="Car">
        Would you like to delete this car?
      </DeleteModal>
      <Button variant="btn-primary" on:click={async () => {
        const result = await deleteModal.prompt(async () => delay(2000));
        if (result) alert('deleted')
      }}>
        Delete Car
      </Button>
    </div>
  </div>
</div>
