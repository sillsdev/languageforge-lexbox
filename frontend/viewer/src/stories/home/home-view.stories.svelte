<script module lang="ts">
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import HomeViewStory from './HomeViewStory.svelte';

  // The home screen has its own theme picker in the app bar, so the decorator's is hidden.
  // (addon-svelte-csf needs `parameters` to be an object literal, hence no fwliteStoryParameters.)
  const {Story} = defineMeta({
    title: 'home/home-view',
    parameters: {fwlite: {themePicker: false, resizable: false}},
  });
</script>

<Story name="Logged in">
  <HomeViewStory loggedIn />
</Story>

<Story name="Logged out">
  <HomeViewStory loggedIn={false} />
</Story>

<Story name="Nothing downloaded">
  <HomeViewStory loggedIn localProjects={[]} />
</Story>
