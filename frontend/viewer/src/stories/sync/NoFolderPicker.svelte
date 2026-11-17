<script lang="ts">
  import type {Snippet} from 'svelte';
  import {DotnetService} from '$lib/dotnet-types';
  import {fallbackPlatformFeaturesService, refreshPlatformFeatures} from '$lib/services/platform-features-service';

  const {children}: {children: Snippet} = $props();

  // Like FwLiteWeb on Linux: folder sync works, but there's no native folder picker
  window.lexbox.ServiceProvider.setService(DotnetService.PlatformFeaturesService, {
    ...fallbackPlatformFeaturesService,
    supportsFolderSync: () => Promise.resolve(true),
    folderExists: path => Promise.resolve(path === '/media/usb/test-project'),
  });
  refreshPlatformFeatures();
</script>

{@render children()}
