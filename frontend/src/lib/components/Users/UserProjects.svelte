<script  context="module" lang="ts">
  import type { User as AdminDashboardUser } from '../../../routes/(authenticated)/admin/+page';

  // We define User type that we'll want in here, and export it so that callers can know they're passing in the right type
  export type User = Pick<AdminDashboardUser, 'id' | 'projects'> & {
    projects: {
      name: string
      code: string
      role: string
    }
  };
  // TODO: That's not the correct way to expand the projects type in AdminDashboardUser. Figure out correct Typescripty way to do it.

</script>

<script lang="ts">
  // List of projects that a given user is member of
  // Shows "manager" badge if user is manager of project
  // Includes confidential projects only if currently logged in user is site admin OR is same user as the one whose project we're looking at
  // Has checkbox beside each project so they can be selected
  // Projects the given user is managing are automatically pre-selected
  // Exposes bindable prop with list of project codes (or project objects) that are selected
  // TODO: Determine if list of project objects or project codes would be more useful / easier to implement

  // export let userId: string; // GUID
  export let user: User;
</script>

<ul>
  {#each user.projects as proj}
    <li>{proj.name} (proj.code)</li>
  {/each}
</ul>
