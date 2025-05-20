<script lang="ts">
  import type { Snippet } from 'svelte';
  import Subject from '$lib/email/Subject.svelte';
  import t from '$lib/i18n';
  const lexboxLogo = 'https://lexbox.org/images/logo-dark.png';
  interface Props {
    subject: string;
    name?: string;
    children?: Snippet;
  }

  const { subject, name, children }: Props = $props();
</script>

<Subject value={subject} />
<!--https://documentation.mjml.io/#getting-started-->
<mjml>
  <mj-head>
    <mj-style>
      {`
      .break-words {
        word-break: break-word;
      }
      `}
    </mj-style>
  </mj-head>
  <mj-body background-color="white" css-class="break-words">
    <mj-section>
      <mj-column>
        <mj-text font-size="20px">
          {#if name}
            {$t('emails.shared.greeting', { name })}
          {:else}
            {$t('emails.shared.greeting_noname')}
          {/if}
        </mj-text>
        <mj-divider border-color="black"></mj-divider>
        {@render children?.()}
      </mj-column>
    </mj-section>
    <mj-section padding-top="100px">
      <mj-column>
        <mj-divider border-color="#6a737d" border-width="1px"></mj-divider>
        <mj-social font-size="15px" icon-size="40px" icon-padding="8px" align="left">
          <mj-social-element href="https://lexbox.org" src={lexboxLogo} alt="Lexbox Logo" font-size="20px">
            {$t('appbar.app_name')}
          </mj-social-element>
        </mj-social>
      </mj-column>
    </mj-section>
  </mj-body>
</mjml>
