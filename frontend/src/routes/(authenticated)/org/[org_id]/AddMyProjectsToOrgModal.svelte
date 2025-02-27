<script lang="ts">
  import {DialogResponse, FormModal} from '$lib/components/modals';
  import UserProjects, {type Project} from '$lib/components/Users/UserProjects.svelte';
  import Button from '$lib/forms/Button.svelte';
  import t from '$lib/i18n';
  import {type LexAuthUser} from '$lib/user';
  import {z} from 'zod';
  import {_addProjectsToOrg, _getProjectsIManage, type Org} from './+page';
  import {ProjectRole} from '$lib/gql/types';
  import {useNotifications} from '$lib/notify';
  import {type UUID} from 'crypto';

  export let user: LexAuthUser;
  export let org: Org;

  const {notifySuccess} = useNotifications();

  const schema = z.object({});

  let formModal: FormModal<typeof schema>;
  let newProjects: Project[] = [];
  let alreadyAddedProjects: number = 0;
  let selectedProjects: string[] = [];

  async function openModal(): Promise<void> {
    const projectsIManage = await _getProjectsIManage(user);

    newProjects = [];
    alreadyAddedProjects = 0;
    projectsIManage.forEach(proj => {
      if (org.projects.find(p => p.id === proj.id)) {
        alreadyAddedProjects++;
      } else {
        newProjects.push({
          ...proj,
          memberRole: ProjectRole.Manager,
        });
      }
    });

    const { response } = await formModal.open(undefined, async () => {
      if (!selectedProjects.length) {
        return $t('org_page.add_my_projects.no_projects_selected');
      }
      const result = await _addProjectsToOrg(org.id as UUID, selectedProjects);
      if (result.error?.message) return result.error.message;
    });

    if (response === DialogResponse.Submit) {
      notifySuccess($t('org_page.notifications.added_projects', { count: selectedProjects.length }));
    }
  }
</script>

<Button variant="btn-success" on:click={openModal}>
  {$t('org_page.add_my_projects.open_button')}
  <span class="i-mdi-plus text-2xl" />
</Button>

<FormModal bind:this={formModal} {schema} hideActions={!newProjects.length}>
  <span slot="title">
    {$t('org_page.add_my_projects.title')}
  </span>
  {#if newProjects.length}
    <UserProjects projects={newProjects} bind:selectedProjects hideRoleColumn />
  {:else if alreadyAddedProjects}
    <span class="text-secondary">
      {$t('org_page.add_my_projects.all_projects_already_added', { count: alreadyAddedProjects })}
    </span>
  {:else}
    <span class="text-secondary">
      {$t('org_page.add_my_projects.no_projects_managed')}
    </span>
  {/if}
  <span slot="submitText">{$t('org_page.add_my_projects.submit_button')}</span>
</FormModal>
