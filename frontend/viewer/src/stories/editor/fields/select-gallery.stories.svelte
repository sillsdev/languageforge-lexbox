<script module lang="ts">
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import SelectGallery from './SelectGallery.svelte';

  const {Story} = defineMeta({
    title: 'editor/fields/Select gallery',
    component: SelectGallery,
  });
</script>

<Story name="All selects" />
