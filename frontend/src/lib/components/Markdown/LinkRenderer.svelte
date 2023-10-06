<script lang="ts">
  export let href: string;
  export let title: string | undefined = undefined;
  export let text: string;
</script>

<a {href} {title} class="link">
  {text}
</a>
