<script lang="ts">
  import { FormModal } from '$lib/components/modals';
  import { TrashIcon } from '$lib/icons';
  import ButtonToggle from '$lib/components/ButtonToggle.svelte';
  import { z } from 'zod';
  import Input from '$lib/forms/Input.svelte';
  import type { ChangeUserAccountByAdminInput, LoadAdminDashboardQuery } from '$lib/gql/types';
  import { _changeUserAccountByAdmin } from './+page';
  import { hash } from '$lib/user';
  import t from '$lib/i18n';

  export let deleteUser: CallableFunction;
  type UserRow = LoadAdminDashboardQuery['users'][0]

  const schema = z.object({
    email: z.string().email(),
    name: z.string(),
    password: z.string().optional(),
    userId: z.string().optional(),
  });
  let formModal: FormModal<typeof schema>;
  $: form = formModal?.form();
  export async function close(): Promise<void> {
    await formModal.close();
  }
  export async function openModal(user: UserRow): Promise<void> {
    $form.email = user.email;
    $form.name = user.name;
    $form.userId = user.id;
    await formModal.open(async () => {
        const changeInput: ChangeUserAccountByAdminInput = {
          userId: user.id,
          email: $form.email,
          name: $form.name,
        }
        await _changeUserAccountByAdmin(changeInput);
      let password: string = $form.password ?? '';
      if (password !== '' && $form.password) {
        await fetch('/api/Admin/resetPasswordAdmin', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ passwordHash: await hash(password), userId: user.id }),
        });
      }
      return;
    });
  }
</script>

<FormModal bind:this={formModal} {schema} let:errors>
  <span slot="title">{$t('admin_dashboard.form_modal.title')}</span>
  <Input
    id="email"
    type="email"
    label={$t('admin_dashboard.form_modal.email_label')}
    bind:value={$form.email}
    required
    error={errors.email}
    autofocus
  />
  <Input
    id="name"
    type="text"
    label={$t('admin_dashboard.form_modal.name_label')}
    bind:value={$form.name}
    required
    error={errors.name}
    autofocus
  />
  <div class="text-error">
    <Input
      id="password"
      type="password"
      label={$t('admin_dashboard.form_modal.password_label')}
      bind:value={$form.password}
    />
  </div>
  <div style="display: flex" slot="extraActions" class="space-x-4">
    <ButtonToggle theme="error" text1="unlock" text2="lock" icon1="i-mdi-lock" icon2="i-mdi-unlocked" />
    <button
      class="btn btn-error rounded"
      on:click={async () => {
        await deleteUser($form.userId);
      }}>{$t('admin_dashboard.form_modal.delete_user')}<TrashIcon /></button
    >
  </div>
  <span slot="submitText">{$t('admin_dashboard.form_modal.update_user')}</span>
</FormModal>
