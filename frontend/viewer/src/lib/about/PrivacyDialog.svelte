<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Button} from '$lib/components/ui/button';
  import ResponsiveDialog from '$lib/components/responsive-dialog/responsive-dialog.svelte';
  import {useAnalyticsService} from '$lib/services/service-provider';

  let {open = $bindable()}: {open: boolean} = $props();

  const analyticsService = useAnalyticsService();

  let enabled = $state(true);
  let busy = $state(false);

  $effect(() => {
    if (open) void refresh();
  });

  async function refresh(): Promise<void> {
    if (!analyticsService) return;
    enabled = await analyticsService.getAnalyticsEnabled();
  }

  async function setEnabled(value: boolean): Promise<void> {
    if (!analyticsService) return;
    busy = true;
    try {
      await analyticsService.setAnalyticsEnabled(value);
      enabled = value;
    } finally {
      busy = false;
    }
  }
</script>

<ResponsiveDialog bind:open title={$t`Privacy`}>
  <div class="flex flex-col gap-4">
    <p class="text-muted-foreground">
      {$t`We collect anonymous usage data to understand how FieldWorks Lite is used and where to improve it. This never includes your dictionary content.`}
    </p>
    {#if enabled}
      <Button variant="outline" icon="i-mdi-cancel" loading={busy} disabled={!analyticsService} onclick={() => setEnabled(false)}>
        {$t`Opt out of analytics`}
      </Button>
    {:else}
      <p class="text-sm text-muted-foreground">{$t`Analytics are turned off.`}</p>
      <Button variant="outline" icon="i-mdi-check" loading={busy} disabled={!analyticsService} onclick={() => setEnabled(true)}>
        {$t`Turn analytics back on`}
      </Button>
    {/if}
    <Button variant="link" href="https://software.sil.org/language-software-privacy-policy/" target="_blank"
            class="h-auto justify-start gap-1 self-start p-0 text-muted-foreground">
      <i class="i-mdi-open-in-new"></i>
      {$t`View the SIL privacy policy`}
    </Button>
  </div>
</ResponsiveDialog>
