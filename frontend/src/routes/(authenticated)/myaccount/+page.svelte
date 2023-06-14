<script lang="ts">
    import type { PageData } from './$types';
    import t from '$lib/i18n';
    import EditableText from '$lib/components/EditableText.svelte';

    export let data: PageData;
    let name = "fred";
  function updateName(newName: string){
    name=newName;
  }
</script>
<svelte:head>
  <title>Account Settings</title>
</svelte:head>
<!-- svelte-ignore a11y-missing-attribute -->
<html data-theme="cupcake">
<head>
  <!-- Include Tailwind CSS and daisyUI -->
</head>
<body>
<div class="w-2/3">
    <h1 class="text-4xl font-bold">Account Settings</h1>
      <div class="card">
        <h2 class="text-2xl font-semibold">Personal Information</h2>
        <form class="flex flex-col gap-4 mt-4">
          <input type="text" disabled={true} placeholder="username" class="input input-bordered" />
          <input type="text" placeholder="Name" class="input input-bordered" />
          <input type="email" placeholder="Email" class="input input-bordered" />
          <input type="password" placeholder="Password" class="input input-bordered" />
          <button type="submit" class="btn btn-primary">Save</button>
        </form>
      </div>
    </div>
</body>
</html>
