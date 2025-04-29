<script lang="ts">
  import SigninWithGoogleButton from './SigninWithGoogleButton.svelte';

  interface Props {
    href: string;
  }

  let { href }: Props = $props();
</script>

<SigninWithGoogleButton {href} text="register_with_google" />
