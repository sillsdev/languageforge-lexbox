<script lang="ts">
  import SigninWithGoogleButton from './SigninWithGoogleButton.svelte';

  export let href: string;
</script>

<SigninWithGoogleButton {href} text="register_with_google" />
