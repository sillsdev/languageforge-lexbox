<script lang="ts">
  import t from '$lib/i18n';
  interface Props {
    href: string;
    text?: 'sign_in_with_google' | 'register_with_google';
  }

  const { href, text = 'sign_in_with_google' }: Props = $props();
</script>

<!--
  Markup and style from: https://developers.google.com/identity/branding-guidelines#expandable-1#
  Changes:
  - added btn class to root so it animates like our other buttons
  - unset min-height from .btn, because it results in padding that breaks the guidelines
  - commented out original transitions (i.e. animations)
  - commented out overflow: hidden, because it's unnecessary and makes text ugly on click (due to .btn)
  - added media query for dark mode using colors from the guidelines
-->
<a data-sveltekit-preload-data="false" data-sveltekit-reload {href} class="gsi-material-button btn">
  <div class="gsi-material-button-state"></div>
  <div class="gsi-material-button-content-wrapper">
    <div class="gsi-material-button-icon">
      <svg
        version="1.1"
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 48 48"
        xmlns:xlink="http://www.w3.org/1999/xlink"
        style="display: block;"
      >
        <path
          fill="#EA4335"
          d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"
        ></path>
        <path
          fill="#4285F4"
          d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z"
        ></path>
        <path
          fill="#FBBC05"
          d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z"
        ></path>
        <path
          fill="#34A853"
          d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z"
        ></path>
        <path fill="none" d="M0 0h48v48H0z"></path>
      </svg>
    </div>
    <span class="gsi-material-button-contents">{$t(`login.${text}`)}</span>
    <span style="display: none;">{$t(`login.${text}`)}</span>
  </div>
</a>

<style>
  .gsi-material-button {
    /* EDIT: */
    min-height: unset;

    user-select: none;
    -moz-user-select: none;
    -webkit-user-select: none;
    -ms-user-select: none;
    appearance: none;
    -webkit-appearance: none;
    background-color: WHITE;
    background-image: none;
    border: 1px solid #747775;
    -webkit-border-radius: 4px;
    border-radius: 4px;
    -webkit-box-sizing: border-box;
    box-sizing: border-box;
    color: #1f1f1f;
    cursor: pointer;
    font-family: 'Roboto', arial, sans-serif;
    font-size: 14px;
    /* EDIT:
    height: 40px;
    */
    letter-spacing: 0.25px;
    outline: none;
    overflow: hidden;
    padding: 0 12px;
    position: relative;
    text-align: center;
    /* EDIT:
    -webkit-transition: background-color .218s, border-color .218s, box-shadow .218s;
    transition: background-color .218s, border-color .218s, box-shadow .218s;
    */
    vertical-align: middle;
    white-space: nowrap;
    width: auto;
    /* EDIT:
    max-width: 400px;
    */
    min-width: min-content;
  }

  .gsi-material-button .gsi-material-button-icon {
    height: 20px;
    margin-right: 12px;
    min-width: 20px;
    width: 20px;
  }

  .gsi-material-button .gsi-material-button-content-wrapper {
    -webkit-align-items: center;
    align-items: center;
    display: flex;
    -webkit-flex-direction: row;
    flex-direction: row;
    -webkit-flex-wrap: nowrap;
    flex-wrap: nowrap;
    height: 100%;
    justify-content: center; /* EDIT: space-between */
    position: relative;
    width: 100%;
  }

  .gsi-material-button .gsi-material-button-contents {
    /* EDIT:
    -webkit-flex-grow: 1;
    flex-grow: 1;
    */
    font-family: 'Roboto', arial, sans-serif;
    font-weight: 500;
    /* EDIT:
    overflow: hidden;
    */
    text-overflow: ellipsis;
    vertical-align: top;
  }

  .gsi-material-button .gsi-material-button-state {
    -webkit-transition: opacity 0.218s;
    transition: opacity 0.218s;
    bottom: 0;
    left: 0;
    opacity: 0;
    position: absolute;
    right: 0;
    top: 0;
  }

  .gsi-material-button:disabled {
    cursor: default;
    background-color: #ffffff61;
    border-color: #1f1f1f1f;
  }

  .gsi-material-button:disabled .gsi-material-button-contents {
    opacity: 38%;
  }

  .gsi-material-button:disabled .gsi-material-button-icon {
    opacity: 38%;
  }

  .gsi-material-button:not(:disabled):active .gsi-material-button-state,
  .gsi-material-button:not(:disabled):focus .gsi-material-button-state {
    background-color: #303030;
    opacity: 12%;
  }

  /* EDIT:
  .gsi-material-button:not(:disabled):hover {
    -webkit-box-shadow:
      0 1px 2px 0 rgba(60, 64, 67, 0.3),
      0 1px 3px 1px rgba(60, 64, 67, 0.15);
    box-shadow:
      0 1px 2px 0 rgba(60, 64, 67, 0.3),
      0 1px 3px 1px rgba(60, 64, 67, 0.15);
  }
  */

  .gsi-material-button:not(:disabled):hover .gsi-material-button-state {
    background-color: #303030;
    opacity: 8%;
  }

  /* EDIT: */
  @media (prefers-color-scheme: dark) {
    .gsi-material-button {
      background-color: #131314;
      color: #e3e3e3;
      border-color: #8e918f;
    }

    .gsi-material-button:disabled {
      background-color: #13131461;
      border-color: #8e918f1f;
    }

    .gsi-material-button:disabled .gsi-material-button-state {
      background-color: #e3e3e31f;
    }

    .gsi-material-button:not(:disabled):focus .gsi-material-button-state {
      background-color: white;
    }

    .gsi-material-button:not(:disabled):hover .gsi-material-button-state {
      background-color: white;
    }
  }
</style>
