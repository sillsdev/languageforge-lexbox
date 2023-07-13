<script lang="ts">
  import { onMount } from 'svelte';
  import { EmailTemplate } from '../emails';

  type EmailConfig = { type: EmailTemplate; label?: string } & Record<string, string>;

  let emails: EmailConfig[] = [
    {
      type: EmailTemplate.ForgotPassword,
      resetUrl: '/resetPassword',
    },
    {
      type: EmailTemplate.VerifyEmailAddress,
      verifyUrl: '/user?emailResult=verifiedEmail',
    },
    {
      label: 'Verify New Email Address',
      type: EmailTemplate.VerifyEmailAddress,
      verifyUrl: '/user?emailResult=changedEmail',
      newAddress: true.toString(),
    },
  ];

  let currEmail = emails[0];

  let iframe: HTMLIFrameElement;

  function resizeIframe(): void {
    iframe.height = `${iframe.contentWindow?.document.body.scrollHeight ?? 700}px`;
  }

  onMount(resizeIframe);
</script>

<label class="label cursor-pointer inline-flex gap-4 m-4">
  <span class="label-text">Email template:</span>
  <select class="select select-info" bind:value={currEmail}>
    {#each emails as email}
      <option value={email}>{email.label ?? email.type.replaceAll(/([a-z])([A-Z])/g, '$1 $2')}</option>
    {/each}
  </select>
</label>

<div class="m-4 mockup-window border bg-white">
  <iframe
    bind:this={iframe}
    width="100%"
    src="./tester/raw?{new URLSearchParams(currEmail)}"
    title={currEmail.type}
    on:load={resizeIframe}
  />
</div>
