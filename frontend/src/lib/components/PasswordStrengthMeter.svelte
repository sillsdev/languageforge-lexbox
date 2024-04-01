<script lang="ts">
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

<progress class="progress progress-{progressColor} w-100" value={score+0.33} max={4.33} />
