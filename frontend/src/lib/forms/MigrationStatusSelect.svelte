<script lang="ts">
  import { ProjectMigrationStatus } from '$lib/gql/generated/graphql';
  import Select from './Select.svelte';

  export let value: ProjectMigrationStatus | "UNMIGRATED" | undefined;
  export let error: string | string[] | undefined = undefined;

</script>
<div class="relative">
  <Select id="migration-status" label="Migration status" bind:value {error} on:change>
    <!-- No need to translate these as this is admin-only -->
    <option value={undefined}>Any</option>
    <option value={ProjectMigrationStatus.Migrated}>Migrated</option>
    <option value={ProjectMigrationStatus.Migrating}>Migrating</option>
    <option value="UNMIGRATED">Unmigrated</option>
  </Select>
</div>
