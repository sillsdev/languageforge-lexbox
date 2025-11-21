<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import type {Task} from './tasks-service';
  import ReviewList from './ReviewList.svelte';
  import {type TaskSubject} from './subject.svelte';
  import {t} from 'svelte-i18n-lingui';

  let {
    subjects,
    allSubjects,
    task,
    onFinish = () => {
    },
    onContinue = () => {
    },
  }: {
    subjects: TaskSubject[],
    allSubjects: TaskSubject[],
    task: Task,
    onFinish: () => void,
    onContinue: () => void,
  } = $props();

  // Compute a larger flex-basis at the edges and smaller in the center.
  // Tweak these to control density.
  const minBasis = 20; // % width for center items (more per row)
  const maxBasis = 55; // % width for edge items (fewer per row)
  function rawBasis(i: number) {
    const n = subjects.length;
    if (n <= 1) return maxBasis;
    const mid = (n - 1) / 2;
    const t = Math.abs(i - mid) / mid; // 0 at center, 1 at edges
    return minBasis + (maxBasis - minBasis) * t;
  }

  let basis = $derived.by(() => {
    const n = subjects.length;
    const raws = subjects.map((_, i) => rawBasis(i));
    if (n < 3) return raws;
    const total = raws.reduce((a, b) => a + b, 0);
    // Scale up so the total width is at least 300% (i.e., 3 rows).
    const scale = total < 300 ? 300 / total : 1;
    return raws.map(b => b * scale);
  });
  let review = $state(false);
</script>
{#if !review}
  <div class="flex flex-col items-center justify-center">
    <h1 class="text-8xl pb-2">🎊</h1>
    <h2 class="text-lg">{$t`You completed ${subjects.length} ${task.subject}`}</h2>
    <div class="flex flex-wrap gap-2 justify-center mt-4 max-w-2xl">
      {#each subjects as subject, index (subject)}
        <span class="px-2 py-1 bg-primary text-primary-foreground rounded text-center min-w-max"
              style="flex: 0 0 {basis[index]}%">
          {subject.subject}
        </span>
      {/each}
    </div>
  </div>
  <div class="grow"></div>
  <div class="px-4 pt-4 pb-2 flex flex-col gap-2 self-stretch">
    <p>{$t`Do you want to:`}</p>
    <Button variant="secondary" onclick={onContinue}>{$t`Keep going`}</Button>
    <Button autofocus onclick={() => review = true}>{$t`Review`}</Button>
  </div>
{:else}
  <ReviewList subjects={allSubjects} onFinish={() => onFinish()}></ReviewList>
{/if}
