<script lang="ts">
  import Subject from '$lib/email/Subject.svelte';
  import t from '$lib/i18n';
  const lexboxLogo = 'https://lexbox.org/images/logo-dark.png';
  export let subject: string;
  export let name: string;
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
        <mj-divider border-color="black" />
        <slot />
      </mj-column>
    </mj-section>
    <mj-section padding-top="100px">
      <mj-column>
        <mj-divider border-color="#6a737d" border-width="1px" />
        <mj-social font-size="15px" icon-size="40px" icon-padding="8px" align="left">
          <mj-social-element
            href="https://lexbox.org"
            src={lexboxLogo}
            alt="Lexbox Logo"
            font-size="20px">
            {$t('appbar.app_name')}
          </mj-social-element>
        </mj-social>
      </mj-column>
    </mj-section>
  </mj-body>
</mjml>
