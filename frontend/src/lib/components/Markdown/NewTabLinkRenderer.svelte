<script lang="ts">
  export let href: string;
  export let title: string | undefined = undefined;
  export let text: string;
</script>

<a {href} {title} target="_blank" class="inline-flex items-end gap-0.5">
  {text}<span class="i-mdi-open-in-new" />
</a>
