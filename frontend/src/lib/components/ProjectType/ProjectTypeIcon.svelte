<script lang="ts">
  import { ProjectType } from '$lib/gql/types';
  import flexLogo from '$lib/assets/flex-logo.png';
  import oneStoryLogo from '$lib/assets/onestory-editor-logo.png';
  import weSayLogo from '$lib/assets/we-say-logo.png';
  import ourWordLogo from '$lib/assets/our-word-logo.png';
  import t from '$lib/i18n';

  export let type: ProjectType | undefined;
  export let size = 'h-6';

  $: src = type === ProjectType.FlEx ? flexLogo
    : type === ProjectType.OneStoryEditor ? oneStoryLogo
    : type === ProjectType.WeSay ? weSayLogo
    : type === ProjectType.OurWord ? ourWordLogo : '';
</script>

{#if src}
  <img {src} alt={$t('project_type.logo', { type: type ?? ProjectType.Unknown })} class={size}>
{:else if type}
  <span class="i-mdi-help-circle-outline text-xl mb-[-2px]" />
{/if}
