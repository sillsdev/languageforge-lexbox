<!-- The most "modern" method for creating a dropdown with DaisyUI is using the <details> tag,
  but they only close when the user explicitly clicks on the toggle button again.
  We generally want dropdowns to close automatically as the user interacts with the app -->
<div class="dropdown dropdown-end">
  <slot />
  <ul tabindex="-1" class="dropdown-content menu bg-base-200 shadow rounded-box z-10">
    <slot name="items" />
  </ul>
</div>
