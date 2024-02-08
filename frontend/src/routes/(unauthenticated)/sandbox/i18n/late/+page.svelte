<script lang="ts">
  import t from '$lib/i18n';
  import { onMount } from 'svelte';

  let text: string | undefined = undefined;

  onMount(() => {
    setTimeout(() => {
      text = $t('login.title');
    }, 500);
  });
</script>

<div class="grid gap-2 grid-cols-3">
  <div class="card bg-base-200 shadow-lg">
    <div class="card-body">
      <h2 class="card-title">Translations</h2>
      <div>
        The only translation on this page happens <code>onMount</code>.
        So, it looks like we call Svelte's <code>getContext</code> <a href="https://svelte.dev/docs/svelte#getcontext" target="_blank">later than is allowed</a>.
        However, as long as we use <code>$t</code> syntax and not <code>t.subscribe()</code> it seems to work fine.
        <code>console.log</code> suggests that Svelte magically subscribes during component initialization long before we even call <code>setTimeout</code>.
      </div>

      <div class="delayed-current">Translated text: {text}</div>
    </div>
  </div>
</div>
