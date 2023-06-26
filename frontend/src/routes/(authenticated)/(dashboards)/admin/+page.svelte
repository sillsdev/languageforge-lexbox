<script lang="ts">
  import { Badge } from '$lib/components/Badges';
  import FormatDate from '$lib/components/FormatDate.svelte';
  import FormatProjectType from '$lib/components/FormatProjectType.svelte';
  import Input from '$lib/forms/Input.svelte';
  import t from '$lib/i18n';
  import type { PageData } from './$types';
  import {PencilIcon, TrashIcon} from '$lib/icons';
  import { z } from 'zod';
  import { FormModal } from '$lib/components/modals';
  import {_changeUserAccountByAdmin, _deleteUserByAdmin} from './+page';
  import  type {ChangeUserAccountByAdminInput} from '$lib/gql/types';
  import type {DeleteUserByAdminInput} from '$lib/gql/types';
  import { hash } from '$lib/user';
  export let data: PageData;

  let projectSearch = '';
  let userSearch = '';
  $: projectSearchLower = projectSearch.toLocaleLowerCase();
  $: projects = data.projects
    .filter(
      (p) =>
        !projectSearch ||
        p.name.toLocaleLowerCase().includes(projectSearchLower) ||
        p.code.toLocaleLowerCase().includes(projectSearchLower)
    )
    .slice(0, projectSearch ? undefined : 10);
  $: userSearchLower = userSearch.toLocaleLowerCase();
  $: users = data.users
    .filter(
      (u) =>
        !userSearch ||
        u.name.toLocaleLowerCase().includes(userSearchLower) ||
        u.email.toLocaleLowerCase().includes(userSearchLower)
    )
    .slice(0, userSearch ? undefined : 10);

    const schema = z.object({
        email: z.string().email(),
        confirm: z.string().email(),
        name: z.string(),
        password: z.string().optional(),
        confirmPassword: z.string().optional(),
        userId: z.string().optional(),
    });
  const verify = z.object({
       keyphrase: z.string().optional(),
  });
  let formModal: FormModal<typeof schema>;
  $: form = formModal?.form();
  let deletionFormModal: FormModal<typeof verify>;
  $: deletionForm = deletionFormModal?.form();

  async function deleteUser(id: string): Promise<void> {
    alert(id);
       await deletionFormModal.open(async () => {
           if ($deletionForm.keyphrase === 'hello'){
            if(data.user){
               const deleteUserInput: DeleteUserByAdminInput = {
                adminId: data.user.id,
                userId: id,
               }
               await _deleteUserByAdmin(deleteUserInput);
               return 'cool';
           }}
           return;
       });
  }
  async function openModal(user: any): Promise<void> {
    $form.email = user.email;
    $form.name = user.name;
    $form.confirm = user.email;
    $form.userId = user.id;
    await formModal.open(async () => {

        if ($form.email !== $form.confirm){
            return 'Emails do not match';
        }
        if (data.user){
        const changeInput: ChangeUserAccountByAdminInput = {
            adminId: data.user.id,
            userId: user.id,
            email: $form.email,
            name: $form.name,

        }
        await _changeUserAccountByAdmin(changeInput);
    }
    let password: string = $form.password ?? '';
    if (password !== '' && $form.password === $form.confirmPassword){
        await fetch('api/login/resetPasswordAdmin', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ passwordHash: await hash(password), userId: user.id}),
        });
    }
      return;
    });
  }
</script>

<svelte:head>
    <title>{$t('admin_dashboard.title')}</title>
</svelte:head>

<main>
  <div class="grid grid-cols-2">
    <div class="pl-1 overflow-x-auto">
      <span class="text-xl">
        {$t('admin_dashboard.project_table_title')}
        <Badge>{projectSearch ? projects.length : data.projects.length}</Badge>
      </span>

      <Input
        type="text"
        label={$t('admin_dashboard.filter_label')}
        placeholder={$t('admin_dashboard.filter_placeholder')}
        autofocus
        bind:value={projectSearch}
      />

      <div class="divider" />

      <table class="table">
        <thead>
          <tr>
            <th>{$t('admin_dashboard.column_name')}</th>
            <th>{$t('admin_dashboard.column_code')}</th>
            <th>{$t('admin_dashboard.column_users')}</th>
            <th
              >{$t('admin_dashboard.column_last_change')}<span
                class="i-mdi-sort-ascending text-xl align-[-5px] ml-1"
              /></th
            >
            <th>{$t('admin_dashboard.column_type')}</th>
          </tr>
        </thead>
        <tbody>
          {#each projects as project}
            <tr>
              <td>
                <a class="link" href={`/project/${project.code}`}>
                  {project.name}
                </a>
              </td>
              <td>{project.code}</td>
              <td>{project.projectUsersAggregate.aggregate?.count ?? 0}</td>
              <td>
                <FormatDate date={project.lastCommit} />
              </td>
              <td>
                <FormatProjectType type={project.type} />
              </td>
            </tr>
          {/each}
        </tbody>
      </table>
    </div>

    <div class="pl-1 overflow-x-auto">
      <span class="text-xl">
        {$t('admin_dashboard.user_table_title')}
        <Badge>{userSearch ? users.length : data.users.length}</Badge>
      </span>

      <Input
        type="text"
        label={$t('admin_dashboard.filter_label')}
        placeholder={$t('admin_dashboard.filter_placeholder')}
        bind:value={userSearch}
      />

      <div class="divider" />

      <table class="table">
        <thead>
          <tr>
            <th>{$t('admin_dashboard.column_name')}<span class="i-mdi-sort-ascending text-xl align-[-5px] ml-1" /></th>
            <th>{$t('admin_dashboard.column_email')}</th>
            <th>{$t('admin_dashboard.column_role')}</th>
            <th>{$t('admin_dashboard.column_created')}</th>
            <th>Edit</th>
          </tr>
        </thead>
        <tbody>
          {#each users as user}
            <tr>
              <td>{user.name}</td>
              <td>{user.email}</td>
              <td>{user.isAdmin ? $t('user_types.admin') : $t('user_types.user')}</td>
              <td>
                <FormatDate date={user.createdDate} />
              </td>
            <td><button class="btn btn-ghost rounded" on:click={async () => {await openModal(user)}}><PencilIcon></PencilIcon></button></td>

            </tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>

<FormModal bind:this={formModal} {schema} let:errors>
    <span slot="title">Edit </span>
    <Input
      id="email"
      type="email"
      label="Enter new email"
      bind:value={$form.email}
      required
      error={errors.email}
      autofocus
    />
    <Input
      id="confirm"
      type="email"
      label="Confirm new email"
      bind:value={$form.confirm}
      required
      error={errors.confirm}
      autofocus
    />
    <Input
      id="name"
      type="text"
      label="Change display name"
      bind:value={$form.name}
      required
      error={errors.confirm}
      autofocus
    />
    <span class="text text-warning mb-4">Danger zone:</span>
    <Input
        id="password"
        type="password"
        label="Change password"
        bind:value={$form.password}
        required={false}

  />
  <Input
    id="confirmPassword"
    type="password"
    label="Confirm password"
    bind:value={$form.confirmPassword}
    required={false}

    />
    <button class="btn btn-error" on:click={async () => {await deleteUser($form.userId)}}>Delete User<TrashIcon></TrashIcon></button>
    <span slot="submitText">Apply</span>
  </FormModal>

  <FormModal bind:this={deletionFormModal} {verify} let:errors>
    <span slot="title">Edit </span>
    <Input
    id="keyphrase"
    type="text"
    label="Entere the keyphrase"
    placeholder=""
    error={errors.keyphrase}
    bind:value={$deletionForm.keyphrase}
  />
</FormModal>
</main>
