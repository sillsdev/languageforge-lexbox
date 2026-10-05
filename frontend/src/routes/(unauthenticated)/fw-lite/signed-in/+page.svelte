<script lang="ts">
  import t from '$lib/i18n';
  import {TitlePage} from '$lib/layout';
</script>

<TitlePage title={$t('fw_lite_signed_in.title')}>
  <p class="prose">
    {$t('fw_lite_signed_in.message')}
  </p>
</TitlePage>
