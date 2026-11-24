<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import type {Task} from './tasks-service';
  import ReviewList from './ReviewList.svelte';
  import {type TaskSubject} from './subject.svelte';
  import {Icon} from '$lib/components/ui/icon';
  import {plural, t} from 'svelte-i18n-lingui';
  import {pt} from '$lib/views/view-text';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {useTasksStats} from './tasks-stats.svelte';

  let {
    subjects,
    allSubjects,
    task,
    onFinish = () => {
    },
    onContinue = () => {
    },
    onClose = () => {
    },
  }: {
    subjects: TaskSubject[],
    allSubjects: TaskSubject[],
    task: Task,
    onFinish: () => void,
    onContinue: () => void,
    onClose: () => void,
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
  const viewService = useViewService();
  const statsResource = useTasksStats();
  const remaining = $derived(statsResource.current.progress[task.id]?.remaining);
  const taskComplete = $derived(remaining === 0);
  // A task can fill several senses or examples of one entry.
  const filledEntries = $derived(new Set(subjects.map(subject => subject.entry.id)).size);

  function filledText(count: number): string {
    return pt(
      $plural(count, {one: 'You filled in # entry', other: 'You filled in # entries'}),
      $plural(count, {one: 'You filled in # word', other: 'You filled in # words'}),
      viewService.currentView);
  }

  function remainingText(count: number): string {
    return pt(
      $plural(count, {one: '# entry to go', other: '# entries to go'}),
      $plural(count, {one: '# word to go', other: '# words to go'}),
      viewService.currentView);
  }
</script>
{#if !review}
  <div class="flex grow flex-col md:justify-center">
    <div class="flex flex-col items-center gap-2 p-4 text-center">
      <Icon icon="i-mdi-check-circle-outline" class="text-primary size-12" />
      <!-- Focus the heading, not a button: the Enter that saved the last item would otherwise press it. -->
      {#if taskComplete}
        <h2 tabindex="-1" {@attach el => el.focus()} class="text-xl font-semibold outline-none">{$t`This task is complete`}</h2>
        <p class="text-muted-foreground">{filledText(filledEntries)}</p>
      {:else}
        <h2 tabindex="-1" {@attach el => el.focus()} class="text-xl font-semibold outline-none">{filledText(filledEntries)}</h2>
        {#if remaining}
          <p class="text-muted-foreground tabular-nums">{remainingText(remaining)}</p>
        {/if}
      {/if}
      <div class="flex flex-wrap gap-2 justify-center mt-2 max-w-2xl">
        {#each subjects as subject, index (subject)}
          <span class="px-2 py-1 bg-primary text-primary-foreground rounded text-center min-w-max"
                style="flex: 0 0 {basis[index]}%">
            {subject.subject}
          </span>
        {/each}
      </div>
    </div>
    <div class="grow md:hidden"></div>
    <div class="px-4 pt-4 pb-2 flex flex-col gap-2 self-stretch md:self-center md:w-full md:max-w-sm">
      {#if !taskComplete}
        <Button onclick={onContinue}>{$t`Keep going`}</Button>
      {/if}
      <Button variant={taskComplete ? 'default' : 'outline'} icon="i-mdi-arrow-left" iconProps={{class: 'rtl:rotate-180'}} onclick={onClose}>{$t`Back to tasks`}</Button>
      <Button variant="ghost" onclick={() => review = true}>{$t`Review what you filled in`}</Button>
    </div>
  </div>
{:else}
  <ReviewList subjects={allSubjects} onFinish={() => onFinish()}></ReviewList>
{/if}
