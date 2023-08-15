<script lang="ts">
  export let href: string;
  export let title: string | undefined = undefined;
</script>

<a {href} {title} target="_blank" class="inline-flex gap-0.5 items-center">
  <slot /><span class="i-mdi-open-in-new external-link-icon" />
</a>

<style>
  .external-link-icon {
    margin-top: 3%;
    font-size: 0.9em;
  }
</style>
