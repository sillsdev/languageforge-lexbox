<script lang="ts">
  import t from '$lib/i18n';
  import zxcvbn from 'zxcvbn';

  // zxcvbn password strength is an int between 0 and 4 inclusive
  // Feedback colors are red for bad passwords, yellow for poor, green for good
  // Set threshholds for "bad" and "poor" by changing the values below
  export let bad = 0;
  export let poor = 2;

  let score: 0|1|2|3|4;

  function passwordStrengthColor(score: number): 'progress-error' | 'progress-warning' | 'progress-success' {
    if (score <= bad) return 'progress-error';
    if (score <= poor) return 'progress-warning';
    return 'progress-success';
  }

  export let password: string = '';

  $: strength = zxcvbn(password);
  $: score = strength.score;
  $: progressColor = passwordStrengthColor(score);
</script>

<div class="mb-2">
  <progress class="progress {progressColor} w-100" value={score+0.33} max={4.33} />
</div>
