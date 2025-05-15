<script lang="ts">
  import SigninWithGoogleButton from './SigninWithGoogleButton.svelte';

  interface Props {
    href: string;
  }

  const { href }: Props = $props();
</script>

<SigninWithGoogleButton {href} text="register_with_google" />
