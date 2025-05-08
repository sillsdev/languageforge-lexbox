<script lang="ts">
  import { Icon } from '$lib/icons';
  import type { HelpLink } from '.';

  interface Props {
    helpLink: HelpLink;
  }

  let { helpLink }: Props = $props();
</script>

<sup>
  <a href={helpLink} target="_blank" rel="external">
    <Icon icon="i-mdi-help-circle-outline" color="text-info" size="text-md" />
  </a>
</sup>
