<script lang="ts">
  import t from '$lib/i18n';
  import Icon from '$lib/icons/Icon.svelte';
  import SetTitle from '$lib/layout/SetTitle.svelte';
</script>

<SetTitle title={$t('fw_lite_signed_in.title')} />

<div class="flex flex-col items-center grow">
  <div class="flex flex-col justify-center grow max-w-lg">
    <div class="flex-grow"></div>
    <div class="grid gap-x-3 gap-y-1 items-center" style="grid-template-columns: auto 1fr">
      <div class="row-span-2">
        <Icon icon="i-mdi-check-decagram" color="text-success" size="text-5xl" />
      </div>
      <h2 class="text-3xl">
        {$t('fw_lite_signed_in.title')}
      </h2>
      <div>
        {$t('fw_lite_signed_in.message')}
      </div>
    </div>
    <div class="flex-grow-[2]"></div>
  </div>
</div>
