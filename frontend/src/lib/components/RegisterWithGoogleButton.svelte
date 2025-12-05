<script lang="ts">
  import type {ComponentProps} from 'svelte';
  import SigninWithGoogleButton from './SigninWithGoogleButton.svelte';

  type Props = Omit<ComponentProps<typeof SigninWithGoogleButton>, 'text'>;

  const { href }: Props = $props();
</script>

<SigninWithGoogleButton {href} text="register_with_google" />
