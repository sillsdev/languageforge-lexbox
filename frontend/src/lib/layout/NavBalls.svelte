<script>
  import { AdminIcon, AuthenticatedUserIcon, HomeIcon } from '$lib/icons';
  import AdminContent from './AdminContent.svelte';
</script>

<div class="flex gap-2">
  <AdminContent>
    <div class="tooltip tooltip-hover tooltip-bottom" data-tip="Admin Dashboard">
      <a href="/admin" class="btn btn-accent btn-md-sm btn-outline btn-circle">
        <AdminIcon />
      </a>
    </div>
  </AdminContent>
  <div class="tooltip tooltip-hover tooltip-bottom" data-tip="My Projects">
    <a href="/" class="btn btn-md-sm btn-outline btn-circle">
      <HomeIcon />
    </a>
  </div>
  <div class="tooltip tooltip-hover tooltip-bottom" data-tip="Account Settings">
    <a href="/user" class="btn btn-md-sm btn-outline btn-circle">
      <AuthenticatedUserIcon />
    </a>
  </div>
</div>
