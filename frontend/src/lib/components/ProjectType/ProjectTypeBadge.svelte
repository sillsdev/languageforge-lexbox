<script lang="ts">
  import ProjectTypeIcon from './ProjectTypeIcon.svelte';
  import FormatProjectType from './FormatProjectType.svelte';
  import type { ProjectType } from '$lib/gql/types';
  import { Badge } from '../Badges';

  interface Props {
    type: ProjectType;
  }

  const { type }: Props = $props();
</script>

<Badge>
  <span class="flex items-center gap-2">
    <FormatProjectType {type} />
    <ProjectTypeIcon {type} />
  </span>
</Badge>
