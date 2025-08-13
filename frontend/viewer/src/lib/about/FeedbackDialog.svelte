<script lang="ts">
  import * as Dialog from '$lib/components/ui/dialog';
  import { Button } from '$lib/components/ui/button';
  import { t } from 'svelte-i18n-lingui';
  import { Icon } from '$lib/components/ui/icon';
  import { useFwLiteConfig } from '$lib/services/service-provider';

  let { open } = $props();
  const config = useFwLiteConfig();

  const appVersion = config.appVersion;
</script>

<Dialog.Root bind:open>
  <Dialog.Content>
    <Dialog.Header>
      <Dialog.Title>{$t`Feedback`}</Dialog.Title>
    </Dialog.Header>
    <div class="flex flex-col gap-4">
      <a href="https://userjo.com/1" target="_blank" class="flex items-center gap-4 p-4 rounded-lg hover:bg-muted">
        <Icon icon="i-mdi-lightbulb-on-outline" class="text-2xl" />
        <div>
          <div class="font-semibold">{$t`Suggest your ideas`}</div>
          <div class="text-sm text-muted-foreground">{$t`Share your suggestions for new features or improvements.`}</div>
        </div>
      </a>
      <a href={config.feedbackUrl} target="_blank" class="flex items-center gap-4 p-4 rounded-lg hover:bg-muted">
        <Icon icon="i-mdi-bug-outline" class="text-2xl" />
        <div>
          <div class="font-semibold">{$t`Report a technical problem`}</div>
          <div class="text-sm text-muted-foreground">{$t`Let us know about any bugs or technical issues you encounter.`}</div>
        </div>
      </a>
      <a href={`mailto:lexbox_support@groups.sil.org?subject=FW%20Lite%20Feedback&body=App%20Version:%20${appVersion}`} class="flex items-center gap-4 p-4 rounded-lg hover:bg-muted">
        <Icon icon="i-mdi-email-outline" class="text-2xl" />
        <div>
          <div class="font-semibold">{$t`Send us a message`}</div>
          <div class="text-sm text-muted-foreground">{$t`For any other inquiries, feel free to send us an email.`}</div>
        </div>
      </a>
    </div>
    <Dialog.Footer>
      <Button onclick={() => open = false}>{$t`Close`}</Button>
    </Dialog.Footer>
  </Dialog.Content>
</Dialog.Root>
