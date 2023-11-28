<script lang="ts">
  export let href: string;
  export let title: string | undefined = undefined;
</script>

<a {href} {title} target="_blank" class="inline-flex items-end gap-0.5">
  <slot /><span class="i-mdi-open-in-new" />
</a>
