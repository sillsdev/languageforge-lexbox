<script lang="ts">
  import {TooltipProvider} from '$lib/components/ui/tooltip';
  import {setupGlobalErrorHandlers} from '$lib/errors/global-errors';
  import {ModeWatcher} from 'mode-watcher';
  import {Router} from 'svelte-routing';
  import NotificationOutlet from './lib/notifications/NotificationOutlet.svelte';
  import AppRoutes from './AppRoutes.svelte';

  setupGlobalErrorHandlers();
</script>

<ModeWatcher />


<TooltipProvider delayDuration={300}>
  <div class="app">
    <Router>
      <AppRoutes />
    </Router>
    <NotificationOutlet/>
  </div>
</TooltipProvider>
