<script>
  import { goto } from '$app/navigation';
  import { useNotifications } from '$lib/notify';
  import { onMount } from 'svelte';

  const { notifySuccess } = useNotifications();

  export let data;

  $: successMsg = `${data.userName} has been approved to join the ${data.projectCode} project`;

  onMount(() => {
    if (successMsg)
    {
      notifySuccess(successMsg);
      if (data.redirect) void goto(data.redirect, {replaceState: true}); //{replaceState: true}
    }
  });
</script>
