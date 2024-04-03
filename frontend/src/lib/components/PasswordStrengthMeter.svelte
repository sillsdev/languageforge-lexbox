<script lang="ts">
  import t from '$lib/i18n';
  import zxcvbn from 'zxcvbn';

  // zxcvbn password strength is an int between 0 and 4 inclusive
  // Feedback colors are red for bad passwords, yellow for poor, green for good
  // Set threshholds for "bad" and "poor" by changing the values below
  export let bad = 0;
  export let poor = 2;

  let score: 0|1|2|3|4;

  function passwordStrengthColor(score: number): 'error' | 'warning' | 'success' {
    if (score <= bad) return 'error';
    if (score <= poor) return 'warning';
    return 'success';
  }

  export let password: string = '';

  $: strength = zxcvbn(password);
  $: score = strength.score;
  $: progressColor = passwordStrengthColor(score);
  // TODO: Use strength.feedback.warning and strength.feedback.suggestions to provide user feedback (in a hover tooltip) on how to improve the password
</script>

<div class="mb-2">
  <progress class="progress progress-{progressColor} w-100" value={score+0.33} max={4.33} />
  {#if strength?.feedback?.warning}
    <!-- TODO: This isn't great: if we don't translate some warning, l10n users see something like "login.password_warnings.This is a top-10 password". How can we do this better? -->
    <span class="text-error w-100">{$t(`login.password_warnings.${strength?.feedback?.warning}`)}</span>
  {/if}
</div>
