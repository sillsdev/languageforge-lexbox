<script lang="ts">
  import { ProjectMigrationStatus } from '$lib/gql/generated/graphql';
  import Select from './Select.svelte';

  export let value: ProjectMigrationStatus | undefined;
  export let error: string | string[] | undefined = undefined;
  export let undefinedOptionLabel: string | undefined = undefined;

</script>
<div class="relative">
  <Select id="migration-status" label="Migration status" bind:value {error} on:change>
    <!-- No need to translate these as this is admin-only -->
    <option value="">Any</option>
    <option value={ProjectMigrationStatus.Migrated}>Migrated</option>
    <option value={ProjectMigrationStatus.Migrating}>Migrating</option>
    <option value={ProjectMigrationStatus.Unknown}>Unknown</option>
    <option value={ProjectMigrationStatus.PrivateRedmine}>Unmigrated, private</option>
    <option value={ProjectMigrationStatus.PublicRedmine}>Unmigrated, public</option>
  </Select>
</div>
