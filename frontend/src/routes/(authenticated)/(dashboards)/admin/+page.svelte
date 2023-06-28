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
        name: z.string(),
        password: z.string().optional(),
        userId: z.string().optional(),
    });
  const verify = z.object({
       keyphrase: z.string().optional(),
  });

  let formModal: FormModal<typeof schema>;
  $: form = formModal?.form();
  let deletionFormModal: FormModal<typeof verify>;
  $: deletionForm = deletionFormModal?.form();


  async function deleteUser(id: any): Promise<void> {
    formModal.close();
       await deletionFormModal.open(async () => {
        if( data.user ){
           if ($deletionForm.keyphrase === 'delete user'){
               const deleteUserInput: DeleteUserByAdminInput = {
                adminId: data.user.id,
                userId: id,
               }
               await _deleteUserByAdmin(deleteUserInput);
           }}
       });
  }
  async function openModal(user: any): Promise<void> {
    $form.email = user.email;
    $form.name = user.name;
    $form.userId = user.id;
    await formModal.open(async () => {
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
    if (password !== '' && $form.password){
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
  <div class="grid grid-cols-2 m:grid-cols-1">
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
            <th>{$t('admin_dashboard.column_edit')}</th>
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
            <td>
                <button class="btn btn-ghost rounded" on:click={async () => {await openModal(user)}}><PencilIcon></PencilIcon></button></td>

            </tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>

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
    <div class = "text-error">
    <Input
        id="password"
        type="password"
        label={$t('admin_dashboard.form_modal.password_label')}
        bind:value={$form.password}
        required={false}/>
    </div>
    <button slot ="extraActions" class="btn btn-error rounded" on:click={async () => {await deleteUser($form.userId)}}>{$t('admin_dashboard.form_modal.delete_user')}<TrashIcon></TrashIcon></button>
    <span slot="submitText">{$t('admin_dashboard.form_modal.update_user')}</span>
  </FormModal>

  <FormModal bind:this={deletionFormModal} {verify} let:errors>
    <span slot="title">{$t('admin_dashboard.form_modal.delete_user')}</span>
    <Input
    id="keyphrase"
    type="text"
    label={$t('admin_dashboard.delete_modal.label')}
    placeholder={$form.name}
    error={errors.keyphrase}
    bind:value={$deletionForm.keyphrase}
  />
  <span slot="submitText">{$t('admin_dashboard.form_modal.delete_user')}</span>
</FormModal>
</main>
