<script lang="ts">
  import { run } from 'svelte/legacy';

  import zxcvbn from 'zxcvbn';

  // zxcvbn password strength is an int between 0 and 4 inclusive
  // Feedback colors are red for bad passwords, yellow for poor, green for good
  


  function passwordStrengthColor(score: number): 'progress-error' | 'progress-warning' | 'progress-success' {
    if (score <= bad) return 'progress-error';
    if (score <= poor) return 'progress-warning';
    return 'progress-success';
  }

  interface Props {
    // Set threshholds for "bad" and "poor" by changing the values below
    bad?: number;
    poor?: number;
    score: number;
    password?: string;
  }

  let {
    bad = 0,
    poor = 2,
    score = $bindable(),
    password = ''
  }: Props = $props();

  let strength = $derived(zxcvbn(password));
  run(() => {
    score = strength.score;
  });
  let progressColor = $derived(passwordStrengthColor(score));
</script>

<div class="mb-2">
  <progress class="progress {progressColor} w-100" value={score+0.33} max={4.33}></progress>
</div>
