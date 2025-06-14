<script lang="ts">
  import { FormModal } from '$lib/components/modals';
  import { TrashIcon } from '$lib/icons';
  import { z } from 'zod';
  import { Button, FormError, Input, SystemRoleSelect, emptyString, passwordFormRules } from '$lib/forms';
  import { type FeatureFlag, UserRole } from '$lib/gql/types';
  import { _changeUserAccountByAdmin, _setUserLocked, type User } from './+page';
  import type { LexAuthUser } from '$lib/user';
  import t from '$lib/i18n';
  import type { FormModalResult } from '$lib/components/modals/FormModal.svelte';
  import { hash } from '$lib/util/hash';
  import Icon from '$lib/icons/Icon.svelte';
  import UserLockedAlert from '$lib/components/Users/UserLockedAlert.svelte';
  import PasswordStrengthMeter from '$lib/components/PasswordStrengthMeter.svelte';
  import { allPossibleFlags } from '$lib/user';
  import AdminContent from '$lib/layout/AdminContent.svelte';
  import { untrack } from 'svelte';

  interface Props {
    currUser: LexAuthUser;
    deleteUser: (user: User) => void;
  }

  const { currUser, deleteUser }: Props = $props();

  const schema = z.object({
    email: z.string().email($t('form.invalid_email')).nullish(),
    emailVerified: z.boolean(),
    name: z.string(),
    password: passwordFormRules($t).or(emptyString()).default(''),
    score: z.number(),
    featureFlags: z.array(z.string()),
    role: z.enum([UserRole.User, UserRole.Admin]),
  });
  const refinedSchema = schema.refine((data) => data.role !== UserRole.Admin || (data.email && data.emailVerified), {
    message: $t('admin_dashboard.form_modal.role_label.verified_email_required_for_admin'),
    path: ['role'],
  });

  type Schema = typeof schema;
  type RefinedSchema = typeof refinedSchema;
  // eslint-disable-next-line @typescript-eslint/no-redundant-type-constituents
  let formModal: FormModal<RefinedSchema> | undefined = $state();

  export function close(): void {
    formModal?.close();
  }

  let _user: User | undefined = $state();
  export async function openModal(user: User): Promise<FormModalResult<Schema>> {
    _user = user;
    userIsLocked = user.locked;
    const role = user.isAdmin ? UserRole.Admin : UserRole.User;
    return await formModal!.open(
      {
        name: user.name,
        email: user.email ?? null,
        featureFlags: user.featureFlags,
        role,
        emailVerified: user.emailVerified,
      },
      async () => {
        const { error, data } = await _changeUserAccountByAdmin({
          userId: user.id,
          email: $form!.email,
          name: $form!.name,
          featureFlags: $form!.featureFlags as FeatureFlag[],
          role: $form!.role,
        });
        if (data?.changeUserAccountByAdmin.errors?.some((e) => e.__typename === 'UniqueValueError')) {
          return { email: [$t('account_settings.email_taken')] };
        }
        if (error) {
          return error.message;
        }
        if ($form!.password) {
          await fetch('/api/Admin/resetPassword', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
              passwordHash: await hash($form!.password),
              passwordStrength: $form!.score,
              userId: user.id,
            }),
          });
        }
      },
    );
  }

  let userIsLocked: boolean = $state(false);
  let locking = $state(false);
  let lockUserError: string | undefined = $state();

  async function onLockedClicked(event: Event): Promise<void> {
    if (!_user) return;
    event.preventDefault();
    lockUserError = undefined;
    const newLocked = !userIsLocked;
    locking = true;
    const { error, data } = await _setUserLocked({
      userId: _user.id,
      locked: newLocked,
    }).finally(() => (locking = false));
    if (error) {
      // this is not pretty, but it's for entirely unexpected circumstances
      lockUserError = error?.message;
      return;
    }
    userIsLocked = data!.setUserLocked.user!.locked;
  }
  let form = $derived(formModal?.form());
  // This is a bit of a hack to make sure that the email field is not required if the user has no email
  // even if the user edited the email field
  $effect(() => {
    if (form && $form && !$form.email && $form.email !== null && _user && !_user.email) $form.email = null;
  });
</script>

<FormModal bind:this={formModal} schema={refinedSchema}>
  {#snippet title()}
    <span>
      {$t('admin_dashboard.form_modal.title')}
    </span>
  {/snippet}
  {#snippet children({ errors })}
    <UserLockedAlert locked={userIsLocked} />
    <Input
      id="email"
      type="email"
      label={$t('admin_dashboard.form_modal.email_label')}
      bind:value={$form!.email}
      error={errors.email}
      autofocus
    />
    <Input
      id="name"
      type="text"
      label={$t('admin_dashboard.form_modal.name_label')}
      bind:value={$form!.name}
      error={errors.name}
    />
    <SystemRoleSelect id="role" bind:value={$form!.role} error={errors.role} disabled={_user?.id === currUser.id} />
    <AdminContent>
      <div>
        Feature flags:
        <ul>
          {#each allPossibleFlags as flag}
            <li>
              <label
                ><input type="checkbox" name="featureFlags" value={flag} bind:group={$form!.featureFlags} />
                {flag}</label
              >
            </li>
          {/each}
        </ul>
      </div>
    </AdminContent>
    <div class="text-error">
      <Input
        id="new-password"
        type="password"
        label={$t('admin_dashboard.form_modal.password_label')}
        bind:value={$form!.password}
        autocomplete="new-password"
        error={errors.password}
      />
      <PasswordStrengthMeter
        onScoreUpdated={(score) => {
          if (untrack(() => $form)) $form!.score = score;
        }}
        password={$form!.password}
      />
    </div>
    <FormError error={lockUserError} />
  {/snippet}
  {#snippet extraActions()}
    <label
      class="btn btn-warning swap"
      class:btn-disabled={_user?.id === currUser.id}
      class:btn-outline={!userIsLocked}
    >
      <input readonly type="checkbox" checked={userIsLocked} onclick={onLockedClicked} />
      <span class="swap-on flex gap-2 items-center justify-between">
        {$t('admin_dashboard.form_modal.unlock')}
        <Icon icon={locking ? 'loading loading-spinner loading-sm' : 'i-mdi-lock'} />
      </span>
      <span class="swap-off flex gap-2 items-center justify-between">
        {$t('admin_dashboard.form_modal.lock')}
        <Icon icon={locking ? 'loading loading-spinner loading-sm' : 'i-mdi-lock-open-outline'} />
      </span>
    </label>
    <Button
      variant="btn-error"
      onclick={() => {
        if (_user) deleteUser(_user);
      }}
      disabled={_user?.id === currUser.id}
    >
      {$t('admin_dashboard.form_modal.delete_user.submit')}
      <TrashIcon />
    </Button>
  {/snippet}
  {#snippet submitText()}
    <span>{$t('admin_dashboard.form_modal.update_user')}</span>
  {/snippet}
</FormModal>
