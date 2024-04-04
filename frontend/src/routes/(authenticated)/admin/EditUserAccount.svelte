<script lang="ts">
  import { FormModal } from '$lib/components/modals';
  import { TrashIcon } from '$lib/icons';
  import { z } from 'zod';
  import { Button, FormError, Input, SystemRoleSelect, emptyString, passwordFormRules } from '$lib/forms';
  import { UserRole } from '$lib/gql/types';
  import { _changeUserAccountByAdmin, _setUserLocked, type User } from './+page';
  import type { LexAuthUser } from '$lib/user';
  import t from '$lib/i18n';
  import type { FormModalResult } from '$lib/components/modals/FormModal.svelte';
  import { hash } from '$lib/util/hash';
  import Icon from '$lib/icons/Icon.svelte';
  import UserLockedAlert from '$lib/components/Users/UserLockedAlert.svelte';
  import PasswordStrengthMeter from '$lib/components/PasswordStrengthMeter.svelte';

  export let currUser: LexAuthUser;
  export let deleteUser: (user: User) => void;

  const schema = z.object({
    email: z.string().email($t('form.invalid_email')),
    name: z.string(),
    password: passwordFormRules($t).or(emptyString()).default(''),
    role: z.enum([UserRole.User, UserRole.Admin]),
  });
  type Schema = typeof schema;
  let formModal: FormModal<Schema>;
  $: form = formModal?.form();

  export function close(): void {
    formModal.close();
  }

  let _user: User;
  export async function openModal(user: User): Promise<FormModalResult<Schema>> {
    _user = user;
    userIsLocked = user.locked;
    const role = user.isAdmin ? UserRole.Admin : UserRole.User;
    return await formModal.open({ name: user.name, email: user.email ?? undefined, role }, async () => {
      const { error, data } = await _changeUserAccountByAdmin({
        userId: user.id,
        email: $form.email,
        name: $form.name,
        role: $form.role,
      });
      if (data?.changeUserAccountByAdmin.errors?.some((e) => e.__typename === 'UniqueValueError')) {
        return { email: [$t('account_settings.email_taken')] };
      }
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

  let userIsLocked: boolean;
  let locking = false;
  let lockUserError: string | undefined;

  async function onLockedClicked(event: Event): Promise<void> {
    event.preventDefault();
    lockUserError = undefined;
    const newLocked = !userIsLocked;
    locking = true;
    const { error, data } = await _setUserLocked({
      userId: _user.id,
      locked: newLocked,
    }).finally(() => locking = false)
    if (error) {
      // this is not pretty, but it's for entirely unexpected circumstances
      lockUserError = error?.message;
      return;
    }
    userIsLocked = data!.setUserLocked.user!.locked;
  }
</script>

<FormModal bind:this={formModal} {schema} let:errors>
  <span slot="title">
    {$t('admin_dashboard.form_modal.title')}
  </span>
  <UserLockedAlert locked={userIsLocked} />
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
  <SystemRoleSelect
    id="role"
    bind:value={$form.role}
    error={errors.role}
    disabled={_user.id === currUser.id}
  />
  <div class="text-error">
    <Input
      id="new-password"
      type="password"
      label={$t('admin_dashboard.form_modal.password_label')}
      bind:value={$form.password}
      autocomplete="new-password"
      error={errors.password}
    />
    <PasswordStrengthMeter password={$form.password} />
  </div>
  <FormError error={lockUserError} />
  <svelte:fragment slot="extraActions">
    <label class="btn btn-warning swap" class:btn-disabled={_user.id === currUser.id} class:btn-outline={!userIsLocked}>
      <input
        readonly
        type="checkbox"
        checked={userIsLocked}
        on:click={onLockedClicked} />
        <span class="swap-on flex gap-2 items-center justify-between">
          {$t('admin_dashboard.form_modal.unlock')}
          <Icon icon={locking ? 'loading loading-spinner loading-sm' : 'i-mdi-lock'} />
        </span>
        <span class="swap-off flex gap-2 items-center justify-between">
          {$t('admin_dashboard.form_modal.lock')}
          <Icon icon={locking ? 'loading loading-spinner loading-sm' : 'i-mdi-lock-open-outline'} />
        </span>
    </label>
    <Button variant="btn-error" on:click={() => deleteUser(_user)} disabled={_user.id === currUser.id}>
      {$t('admin_dashboard.form_modal.delete_user.submit')}
      <TrashIcon />
    </Button>
  </svelte:fragment>
  <span slot="submitText">{$t('admin_dashboard.form_modal.update_user')}</span>
</FormModal>
