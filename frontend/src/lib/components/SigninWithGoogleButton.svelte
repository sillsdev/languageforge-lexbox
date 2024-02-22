<script lang="ts">
  export let href: string;
  import t from '$lib/i18n';
</script>

<div class="google-signin-top-level" style="position: relative;">
<a data-sveltekit-preload-data="false" {href}>
<div tabindex="0" role="button" aria-labelledby="button-label" class="google-signin-button bg-google-blue text-white-no-border">
<div class="hover-target">
</div>
<div class="flex-container">
<div class="logo-with-bg">
<div class="logo-alone">
<!-- Official Google logo, do not modify -->
<svg version="1.1" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 48 48">
<g>
<path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z" />
<path fill="#4285F4" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z" />
<path fill="#FBBC05" d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z" />
<path fill="#34A853" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z" />
<path fill="none" d="M0 0h48v48H0z" />
</g>
</svg>
</div>
</div>
<span class="visible-label">
  $t('login.sign_in_with_google')</span>
<span class="hidden-label" id="button-label">
  $t('login.sign_in_with_google')</span>
</div>
</div>
</a>
</div>

<style>
  .google-signin-top-level, .google-signin-top-level * {
    border: none;
    margin: 0;
    padding: 0;
  }

  .google-signin-button .logo-with-bg .logo-alone {
    margin: 0;
    padding: 0;
  }

  .google-signin-button.bg-google-blue {
    background-color: #1a73e8;
  }

  .google-signin-button.text-white-no-border {
    border: none;
    color: #fff;
  }

  .google-signin-button.text-white-no-border:hover .hover-target,
  .google-signin-button.text-white-no-border:focus .hover-target {
    background: rgba(255,255,255,.24);
  }

  .google-signin-button.text-white-no-border:active .hover-target {
    background: rgba(255,255,255,.32);
  }

  .google-signin-button:hover,
  .google-signin-button:focus {
    box-shadow: none;
    border-color: #d2e3fc;
    outline: none;
  }
  .google-signin-button:hover .hover-target,
  .google-signin-button:focus .hover-target {
    background: rgba(66,133,244,.04);
  }
  .google-signin-button:active .hover-target {
    background: rgba(66,133,244,.1);
  }

  .google-signin-button {
    border-radius: 4px;
    box-sizing: border-box;
    transition: background-color .218s,border-color .218s;
    -moz-user-select: none;
    user-select: none;
    background-color: #fff;
    background-image: none;
    border: 1px solid #dadce0;
    color: #3c4043;
    cursor: pointer;
    font-family: "Google Sans",arial,sans-serif;
    font-size: 14px;
    height: 40px;
    letter-spacing: 0.25px;
    outline: none;
    overflow: hidden;
    padding: 0 12px;
    position: relative;
    text-align: center;
    vertical-align: middle;
    white-space: nowrap;
    width: auto;
  }

  @media screen and (-ms-high-contrast:active) {
    .google-signin-button {
      border: 2px solid windowText;
      color: windowText;
    }
  }

  .google-signin-button .hover-target {
    transition: background-color .218s;
    position: absolute;
    top: 0;
    left: 0;
    bottom: 0;
    right: 0;
  }

  .google-signin-button .flex-container {
    display: -webkit-box;
    display: -moz-box;
    display: -ms-flexbox;
    display: -webkit-flex;
    display: flex;
    position: relative;
    align-items: center;
    flex-direction: row;
    justify-content: space-between;
    flex-wrap: nowrap;
    height: 100%;
    width: 100%;
  }

  .google-signin-button .logo-with-bg {
    border-top-left-radius: 3px;
    border-bottom-left-radius: 3px;
    display: -webkit-box;
    display: -moz-box;
    display: -ms-flexbox;
    display: -webkit-flex;
    display: flex;
    justify-content: center;
    align-items: center;
    background-color: #fff;
    height: 36px;
    margin-left: -10px;
    margin-right: 12px;
    min-width: 36px;
    width: 36px;
  }

  .google-signin-button .logo-alone {
    height: 18px;
    margin-right: 8px;
    min-width: 18px;
    width: 18px;
  }

  .google-signin-button .visible-label {
    flex-grow: 1;
    font-family: "Google Sans",arial,sans-serif;
    font-weight: 500;
    overflow: hidden;
    text-overflow: ellipsis;
    vertical-align: top;
  }

  .hidden-label {
    display: none;
  }
</style>
