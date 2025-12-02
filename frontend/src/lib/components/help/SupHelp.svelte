<script lang="ts">
  import {Icon} from '$lib/icons';
  import type {HelpLink} from '.';

  interface Props {
    helpLink: HelpLink;
  }

  const { helpLink }: Props = $props();
</script>

<sup>
  <!-- eslint-disable-next-line svelte/no-navigation-without-resolve -->
  <a href={helpLink} target="_blank" rel="external">
    <Icon icon="i-mdi-help-circle-outline" color="text-info" size="text-md" />
  </a>
</sup>
