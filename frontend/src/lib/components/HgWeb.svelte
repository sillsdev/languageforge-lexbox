<script lang="ts">
  interface Props {
    code: string;
  }

  let { code }: Props = $props();
  function injectStyles(arg: Event | HTMLIFrameElement): void {
    const iframe = 'target' in arg ? (arg.target as HTMLIFrameElement) : arg;
    iframe.contentDocument?.head.insertAdjacentHTML(
      'beforeend',
      `
            <style>
                .hgk-clone-btn {
                    display: none;
                }
            </style>
        `
    );
  }
</script>

<iframe
  use:injectStyles
  onload={injectStyles}
  src="/api/hg-view/{code}"
  title="HG Web view"
  width="100%"
  height="600px"
></iframe>
