<script lang="ts">
  import { Icon } from '$lib/icons';
  import type { HelpLink } from '.';

  export let helpLink: HelpLink;
</script>

<sup>
  <a href={helpLink} target="_blank" rel="external">
    <Icon icon="i-mdi-help-circle-outline" color="text-info" size="text-md" />
  </a>
</sup>
