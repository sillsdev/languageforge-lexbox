<script lang="ts">
  let dropdownContainer: HTMLElement;

  /**
   * Enables closing the dropdown by clicking on the dropdown a second time
   */
  function blurIfOpen(): void {
    if (dropdownContainer.contains(document.activeElement)) {
      // Wait until the click is over
      setTimeout(() => (document.activeElement as HTMLElement)?.blur());
    }
  }
</script>

<!-- The most "modern" method for creating a dropdown with DaisyUI is using the <details> tag,
  but they only close when the user explicitly clicks on the toggle button again.
  We generally want dropdowns to close automatically as the user interacts with the app -->
<div bind:this={dropdownContainer} class="dropdown dropdown-end">
  <div class="contents" on:mousedown={blurIfOpen}>
    <slot />
  </div>
  <div tabindex="-1" class="dropdown-content bg-base-200 shadow rounded-box z-10">
    <slot name="content" />
  </div>
</div>
