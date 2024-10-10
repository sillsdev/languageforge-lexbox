<script lang="ts">
  export let href: string;
  export let title: string | undefined = undefined;
</script>

<a {href} {title} target="_blank" class="external-link link link-hover">
  <!-- &nbsp; prevents the link from ever being at the very beginning of a new line -->
  <slot />&nbsp;<span class="i-mdi-open-in-new external-link-icon" />
</a>

<style>
  .external-link-icon {
    margin-bottom: -0.7%;
    font-size: 0.9em;
  }
</style>
