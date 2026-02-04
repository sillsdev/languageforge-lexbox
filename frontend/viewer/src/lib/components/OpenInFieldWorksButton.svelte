<script lang="ts">
  import flexLogo from '$lib/assets/flex-logo.png';
  import {AppNotification} from '$lib/notifications/notifications';
  import {useAppLauncherService} from '../services/app-launcher-service';
  import {useProjectContext} from '$project/project-context.svelte';
  import type {IEntry} from '../dotnet-types';
  import {buttonVariants, type ButtonProps} from './ui/button';
  import {t} from 'svelte-i18n-lingui';
  import {mergeProps} from 'bits-ui';
  import {cn} from '../utils';
  import {Icon} from './ui/icon';

  type Props = {
    entry: IEntry;
  } & ButtonProps;

  const {entry, class: className, ...rest}: Props = $props();

  const appLauncher = useAppLauncherService();
  const projectContext = useProjectContext();

  async function openInFlex() {
    let opened = false;
    if (appLauncher) {
      opened = await appLauncher.openInFieldWorks(entry.id, projectContext.projectName);
    } else {
      // a 302 redirect to the protocol handler works, but sends the user to the home page 🤷
      const fieldWorksUrlResponse = await fetch(`/api/fw/${projectContext.projectName}/link/entry/${entry.id}`);
      if (fieldWorksUrlResponse.ok) {
        opened = true;
        window.location.href = await fieldWorksUrlResponse.text();
      }
    }
    if (opened) {
      AppNotification.displayAction(
        $t`This project is now open in FieldWorks. To continue working in FieldWorks Lite, close the project in FieldWorks and click Reopen.`,
        {
          label: $t`Reopen`,
          callback: () => window.location.reload(),
        },
        'warning',
      );
    } else {
      AppNotification.display($t`Unable to open in FieldWorks`, 'error');
    }
  }

  const mergedProps = $derived(
    mergeProps(
      {
        onclick: openInFlex,
      },
      rest,
    ),
  );
</script>

<!--button must be a link otherwise it won't follow the redirect to a protocol handler-->
<button class={cn(buttonVariants({variant: 'ghost'}), className)} {...mergedProps}>
  <Icon src={flexLogo} alt={$t`FieldWorks logo`} />
  {$t`Open in FieldWorks`}
</button>
