<script lang="ts">
  import TusUpload from '$lib/components/TusUpload.svelte';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import { Button, Form, Input, lexSuperForm, SubmitButton } from '$lib/forms';
  import { PageBreadcrumb } from '$lib/layout';
  import z from 'zod';
  // eslint-disable-next-line no-restricted-imports
  import { t as otherT } from 'svelte-intl-precompile';
  import t from '$lib/i18n';
  import { _gqlThrows500 } from './+page';
  import ConfirmModal from '$lib/components/modals/ConfirmModal.svelte';
  import { delay } from '$lib/util/time';
  import DeleteModal from '$lib/components/modals/DeleteModal.svelte';
  import { Modal } from '$lib/components/modals';
  import { useNotifications } from '$lib/notify';

  function uploadFinished(): void {
    alert('upload done!');
  }

  async function fetch500(): Promise<Response> {
    return fetch('/api/testing/test500NoException');
  }

  async function fetch403(): Promise<Response> {
    return fetch('/api/AuthTesting/403');
  }
  const formSchema = z.object({
    name: z.string().trim().min(3).max(255),
    lastName: z.string().min(3).max(255),
  });

  // eslint-disable-next-line @typescript-eslint/require-await
  let { form, enhance, errors } = lexSuperForm(formSchema, async () => {
    console.log('submit', $form);
  });
  function preFillForm(): void {
    form.update((f) => ({ ...f, name: 'John' }), { taint: false });
  }

  let disableDropdown = $state(false);

  async function gqlThrows500(): Promise<void> {
    await _gqlThrows500();
  }

  const { notifySuccess } = useNotifications();

  let modal: ConfirmModal | undefined = $state();
  let deleteModal: DeleteModal | undefined = $state();
  let notificationModal: Modal | undefined = $state();
  let notificationModalIsAtBottom = $state(false);
</script>

<PageBreadcrumb>Hello from sandbox</PageBreadcrumb>
<PageBreadcrumb>second value</PageBreadcrumb>
<h2 class="text-lg">Sandbox</h2>
<div class="grid gap-2 grid-cols-3">
  <div class="card w-96 bg-base-200 shadow-lg">
    <a rel="external" class="btn" href="/">Go home</a>
    <div class="divider"></div>
    <a rel="external" class="btn" href="/sandbox/403">Goto page load 403</a>
    <a rel="external" target="_blank" class="btn" href="/sandbox/403">Goto page load 403 new tab</a>
    <a rel="external" class="btn" href="/api/AuthTesting/403">Goto API 403</a>
    <a rel="external" target="_blank" class="btn" href="/api/AuthTesting/403">Goto API 403 new tab</a>
    <button class="btn" onclick={fetch403}>Fetch 403</button>

    <div class="divider"></div>

    <a rel="external" class="btn" href="/sandbox/500">Goto page load 500</a>
    <a rel="external" target="_blank" class="btn" href="/sandbox/500">Goto page load 500 new tab</a>
    <a rel="external" class="btn" href="/api/testing/test500NoException">Goto API 500</a>
    <a rel="external" target="_blank" class="btn" href="/api/testing/test500NoException">Goto API 500 new tab</a>
    <button class="btn" onclick={fetch500}>Fetch 500</button>
    <button class="btn" onclick={gqlThrows500}>GQL 500</button>
  </div>
  <div class="card w-96 bg-base-200 shadow-lg">
    <div class="card-body">
      <TusUpload internalButton endpoint="/api/tus-test" accept="image/*" onUploadComplete={uploadFinished} />
    </div>
  </div>

  <div class="card w-96 bg-base-200 shadow-lg">
    <div class="card-body">
      <h2 class="card-title">Dropdown Example</h2>
      <span>Clicking menu items inside dropdown should cause it to close</span>
      <Dropdown>
        <Button variant="btn-primary">Open Me!</Button>
        {#snippet content()}
          <ul class="menu bg-info rounded-box">
            <li><button>First item</button></li>
            <li><button>Second item</button></li>
          </ul>
        {/snippet}
      </Dropdown>
      <div>
        <Dropdown>
          <Button variant="btn-primary">Open Me!</Button>
          {#snippet content()}
            <ul class="menu bg-info rounded-box">
              <li><button>First item</button></li>
              <li><button>Second item</button></li>
            </ul>
          {/snippet}
        </Dropdown>
      </div>
      <div>
        <Dropdown disabled={disableDropdown}>
          <Button variant="btn-primary" disabled={disableDropdown}>Open dropdown</Button>
          {#snippet content()}
            <div class="bg-neutral p-5">
              <p>Some content</p>
              <Button outline onclick={() => (disableDropdown = true)}>Disable myself</Button>
            </div>
          {/snippet}
        </Dropdown>

        <label class="cursor-pointer label gap-4">
          <span class="label-text">Disabled</span>
          <input bind:checked={disableDropdown} type="checkbox" class="toggle toggle-error" />
        </label>
      </div>
    </div>
  </div>
  <div class="card w-96 bg-base-200 shadow-lg">
    <div class="card-body">
      <h2 class="card-title">Form Example</h2>
      <Form {enhance}>
        <Input id="name" label="Name" type="text" error={$errors.name} bind:value={$form.name} />
        <Input
          id="lastName"
          label="Last Name (allows only white space)"
          type="text"
          error={$errors.lastName}
          bind:value={$form.lastName}
        />
        <SubmitButton>Submit</SubmitButton>
        <Button outline onclick={preFillForm}>Pre fill</Button>
      </Form>
    </div>
  </div>
  <div class="card bg-base-200 shadow-lg col-span-2">
    <div class="card-body">
      <h2 class="card-title">Translations of login.title</h2>
      <div>Current: {$t('login.title')}</div>
      <div>English: {$otherT('login.title', { locale: 'en' })} (always works, because it's the fallback)</div>
      <div>
        French: {$otherT('login.title', { locale: 'fr' })} (only works if it's the current, otherwise uses fallback)
      </div>
      <div>
        Spanish: {$otherT('login.title', { locale: 'es' })} (only works if it's the current, otherwise uses fallback)
      </div>
    </div>
  </div>
  <div class="card bg-base-200 shadow-lg">
    <div class="card-body grid-cols-2 grid justify-items-start">
      <h2 class="card-title col-span-2">Buttons</h2>
      <Button variant="btn-primary" onclick={() => alert('hello')}>Primary Button</Button>
      <Button variant="btn-primary" size="btn-sm" onclick={() => alert('hello')}>Primary Small Button</Button>
      <Button variant="btn-success" onclick={() => alert('hello')}>Success Button</Button>
      <Button variant="btn-error" onclick={() => alert('hello')}>Error Button</Button>
      <Button outline onclick={() => alert('hello')}>Outline Button</Button>
      <Button variant="btn-ghost" onclick={() => alert('hello')}>Ghost Button</Button>
      <Button variant="btn-primary" disabled onclick={() => alert('should not fire')}>Disabled Button</Button>
      <Button variant="btn-primary" loading onclick={() => alert('should not fire')}>Loading Button</Button>
      <Button variant="btn-primary" disabled loading onclick={() => alert('should not fire')}>
        Disabled Loading Button
      </Button>
    </div>
  </div>

  <div class="card bg-base-200 shadow-lg">
    <div class="card-body">
      <h2 class="card-title">Confirm Modal</h2>
      <ConfirmModal
        bind:this={modal}
        title="Confirm?"
        submitText="Confirm"
        submitIcon="i-mdi-hand-wave"
        cancelText="Don't confirm"
      >
        Would you like to confirm this modal?
      </ConfirmModal>
      <Button
        variant="btn-primary"
        onclick={async () => {
          const result = await modal?.open(async () => delay(2000));
          if (result) alert('submitted');
        }}
      >
        Open Modal
      </Button>

      <h2 class="card-title">Delete Modal</h2>
      <DeleteModal bind:this={deleteModal} entityName="Car">Would you like to delete this car?</DeleteModal>
      <Button
        variant="btn-primary"
        onclick={async () => {
          const result = await deleteModal?.prompt(async () => delay(2000));
          if (result) alert('deleted');
        }}
      >
        Delete Car
      </Button>

      <h2 class="card-title">Notification modal</h2>
      <Modal bind:this={notificationModal} bottom={notificationModalIsAtBottom}>
        <h2 class="text-xl mb-2">Notification fun 🎉</h2>
        <Button onclick={() => notifySuccess('Hurra you generated a notification! 😎')}>Generate notification</Button>
        <Button onclick={() => (notificationModalIsAtBottom = !notificationModalIsAtBottom)}>Toggle position</Button>
      </Modal>
      <Button
        variant="btn-primary"
        onclick={() => {
          notificationModal?.openModal();
        }}
      >
        Play with notifications
      </Button>
    </div>
  </div>
</div>
