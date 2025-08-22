<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Icon} from '$lib/components/ui/icon';
  import {useFwLiteConfig} from '$lib/services/service-provider';
  import {Button} from '$lib/components/ui/button';
  import ResponsiveDialog from '$lib/components/responsive-dialog/responsive-dialog.svelte';

  let {open = $bindable()}: { open: boolean } = $props();
  const config = useFwLiteConfig();

  const appVersion = config.appVersion;
  const mailtoUrl = `mailto:lexbox_support@groups.sil.org?subject=${encodeURIComponent('FW Lite Feedback')}&body=${encodeURIComponent(`App Version: ${appVersion} on ${config.os.toString()}`)}`;
</script>

<ResponsiveDialog bind:open title={$t`Feedback`}>
    <div class="flex flex-col gap-4">
      <Button variant="ghost" href="https://lexbox.org/fw-lite/request-features" target="_blank" class="gap-4 p-4 h-auto text-base justify-start whitespace-normal">
        <Icon icon="i-mdi-lightbulb-on-outline" class="size-10"/>
        <div>
          <div class="font-semibold underline">{$t`Suggest your ideas`}</div>
          <div class="text-sm text-muted-foreground">
            {$t`Share your suggestions for new features or improvements.`}
          </div>
        </div>
      </Button>
      <Button variant="ghost" href={config.feedbackUrl} target="_blank" class="gap-4 p-4 h-auto text-base justify-start whitespace-normal">
        <Icon icon="i-mdi-bug-outline" class="size-10"/>
        <div>
          <div class="font-semibold underline">
            {$t`Report a technical problem`}
          </div>
          <div
            class="text-sm text-muted-foreground">
            {$t`Let us know about any bugs or technical issues you encounter.`}
          </div>
        </div>
      </Button>
      <Button variant="ghost" href={mailtoUrl} class="gap-4 p-4 h-auto text-base justify-start whitespace-normal">
        <Icon icon="i-mdi-email-outline" class="size-10"/>
        <div>
          <div class="font-semibold underline">
            {$t`Send us a message`}
          </div>
          <div class="text-sm text-muted-foreground">
            {$t`For any other inquiries, feel free to send us an email.`}
          </div>
        </div>
      </Button>
    </div>
</ResponsiveDialog>
