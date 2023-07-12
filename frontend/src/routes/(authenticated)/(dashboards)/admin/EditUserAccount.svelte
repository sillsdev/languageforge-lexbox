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
  type UserRow = LoadAdminDashboardQuery['users'][0];

  const schema = z.object({
    email: z.string().email(),
    name: z.string(),
    password: z.string().optional(),
  });
  let formModal: FormModal<typeof schema>;
  $: form = formModal?.form();
  export function close(): void {
    formModal.close();
  }
  let _user: UserRow;
  export async function openModal(user: UserRow): Promise<void> {
    _user = user;
    $form.email = user.email;
    $form.name = user.name;
    await formModal.open(async () => {
      const changeInput: ChangeUserAccountByAdminInput = {
        userId: user.id,
        email: $form.email,
        name: $form.name,
      };
      const { error } = await _changeUserAccountByAdmin(changeInput);
      if (error) {
        return error.message;
      }
      if ($form.password) {
        await fetch('/api/Admin/resetPassword', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ passwordHash: await hash($form.password), userId: user.id }),
        });
      }
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
    error={errors.email}
    autofocus
  />
  <Input
    id="name"
    type="text"
    label={$t('admin_dashboard.form_modal.name_label')}
    bind:value={$form.name}
    error={errors.name}
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
    <ButtonToggle theme="error" text1={$t('admin_dashboard.form_modal.unlock')} text2={$t('admin_dashboard.form_modal.lock')} icon1="i-mdi-lock" icon2="i-mdi-unlocked" />
    <button
      class="btn btn-error"
      on:click={async () => {
        await deleteUser(_user.id);
      }}>{$t('admin_dashboard.form_modal.delete_user')}<span class="ml-2"><TrashIcon /></span></button
    >
  </div>
  <span slot="submitText">{$t('admin_dashboard.form_modal.update_user')}</span>
</FormModal>
