<script lang="ts">
  import t from '$lib/i18n';
  import Page from '$lib/layout/Page.svelte';
</script>

<Page>
  <svelte:fragment slot="header">
    {$t('forgot_password.email_sent.title')}
  </svelte:fragment>

  <p class="prose">
    {$t('forgot_password.email_sent.email_sent_message')}
  </p>

  <div class="text-center mt-8">
    <a href="/login" class="btn btn-primary">{$t('forgot_password.email_sent.back_to_login')}</a>
  </div>
</Page>
