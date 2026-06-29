<script lang="ts">
  import * as Dialog from '$lib/components/ui/dialog';
  import {Button} from '$lib/components/ui/button';
  import {Label} from '$lib/components/ui/label';
  import {Textarea} from '$lib/components/ui/textarea';
  import {useMiniLcmApi} from '$lib/services/service-provider';
  import {cn} from '$lib/utils';
  import type {ICommentThread} from '$lib/dotnet-types/generated-types/MiniLcm/Models/ICommentThread';
  import type {IUserComment} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IUserComment';
  import type {SubjectType} from '$lib/dotnet-types/generated-types/MiniLcm/Models/SubjectType';
  import {ThreadStatus} from '$lib/dotnet-types/generated-types/MiniLcm/Models/ThreadStatus';
  import {t} from 'svelte-i18n-lingui';

  type ThreadView = {
    thread: ICommentThread;
    comments: IUserComment[];
  };

  let {
    open = $bindable(false),
    subjectType,
    subjectId,
    subjectName,
  }: {
    open: boolean;
    subjectType: SubjectType;
    subjectId: string;
    subjectName?: string;
  } = $props();

  const api = useMiniLcmApi();

  let threadViews = $state<ThreadView[]>([]);
  let loading = $state(false);
  let saving = $state(false);
  let newThreadText = $state('');
  let replyTextByThreadId = $state<Record<string, string>>({});
  let editingCommentId = $state<string>();
  let editTextByCommentId = $state<Record<string, string>>({});

  const title = $derived(subjectName ? $t`Comments for ${subjectName}` : $t`Comments`);
  const hasThreads = $derived(threadViews.length > 0);

  function onOpenChange(value: boolean): void {
    open = value;
    if (value) void loadThreads();
  }

  async function loadThreads(): Promise<void> {
    loading = true;
    const threads = await api.getCommentThreads(subjectType, subjectId);
    const loadedThreadViews = await Promise.all(
      threads.map(async (thread) => ({
        thread,
        comments: await api.getUserComments(thread.id),
      })),
    );
    threadViews = loadedThreadViews;
    loading = false;
  }

  async function startThread(): Promise<void> {
    const text = newThreadText.trim();
    if (!text) return;

    saving = true;
    const now = new Date().toISOString();
    const threadId = crypto.randomUUID();
    await api.createCommentThread({
      id: threadId,
      subjectId,
      subjectType,
      status: ThreadStatus.Open,
      createdAt: now,
      updatedAt: now,
    }, {
      id: crypto.randomUUID(),
      commentThreadId: threadId,
      text,
      createdAt: now,
      updatedAt: now,
    });
    newThreadText = '';
    await loadThreads();
    saving = false;
  }

  async function replyToThread(threadId: string): Promise<void> {
    const text = replyTextByThreadId[threadId]?.trim();
    if (!text) return;

    saving = true;
    const now = new Date().toISOString();
    await api.addUserComment(threadId, {
      id: crypto.randomUUID(),
      commentThreadId: threadId,
      text,
      createdAt: now,
      updatedAt: now,
    });
    replyTextByThreadId[threadId] = '';
    await loadThreads();
    saving = false;
  }

  function startEditing(comment: IUserComment): void {
    editingCommentId = comment.id;
    editTextByCommentId[comment.id] = comment.text;
  }

  function cancelEditing(commentId: string): void {
    editingCommentId = undefined;
    delete editTextByCommentId[commentId];
  }

  async function saveEdit(commentId: string): Promise<void> {
    const text = editTextByCommentId[commentId]?.trim();
    if (!text) return;

    saving = true;
    await api.editUserComment(commentId, text);
    cancelEditing(commentId);
    await loadThreads();
    saving = false;
  }
</script>

<Dialog.Root bind:open={() => open, onOpenChange}>
  <Dialog.DialogContent class="sm:max-w-2xl">
    <Dialog.DialogHeader>
      <Dialog.DialogTitle class="max-w-[calc(100%-2rem)] truncate pr-2" title={title}>{title}</Dialog.DialogTitle>
      <Dialog.DialogDescription>
        {$t`Start a thread, reply to existing threads, or edit comments.`}
      </Dialog.DialogDescription>
    </Dialog.DialogHeader>

    <div class="space-y-4">
      <div class="space-y-2">
        <Label for="new-comment-thread">{$t`Start a new thread`}</Label>
        <Textarea
          id="new-comment-thread"
          bind:value={newThreadText}
          placeholder={$t`Write the first comment...`}
          rows={3}
          disabled={loading || saving}
        />
        <div class="flex justify-end">
          <Button onclick={() => void startThread()} disabled={!newThreadText.trim() || loading} loading={saving}>
            {$t`Start thread`}
          </Button>
        </div>
      </div>

      <div class="max-h-[60vh] space-y-3 overflow-y-auto pr-1">
        {#if loading}
          <p class="text-sm text-muted-foreground">{$t`Loading comments...`}</p>
        {:else if !hasThreads}
          <p class="rounded-md border border-dashed p-4 text-sm text-muted-foreground">
            {$t`No comment threads yet.`}
          </p>
        {:else}
          {#each threadViews as threadView (threadView.thread.id)}
            <section class="space-y-3 rounded-md border p-3">
              <div class="flex items-center justify-between gap-2">
                <h3 class="text-sm font-semibold">{$t`Thread`}</h3>
                <span class="text-xs text-muted-foreground">{threadView.thread.status}</span>
              </div>

              <div class="space-y-2">
                {#each threadView.comments as comment (comment.id)}
                  <article class="rounded-md bg-muted/60 p-3">
                    <div class="mb-2 flex items-center justify-between gap-2">
                      <span class="text-xs font-medium text-muted-foreground">
                        {comment.authorName || $t`Comment`}
                      </span>
                      <Button
                        variant="ghost"
                        size="xs"
                        onclick={() => startEditing(comment)}
                        disabled={saving || editingCommentId === comment.id}
                      >
                        {$t`Edit`}
                      </Button>
                    </div>

                    {#if editingCommentId === comment.id}
                      <div class="space-y-2">
                        <Textarea
                          bind:value={editTextByCommentId[comment.id]}
                          aria-label={$t`Edit comment`}
                          rows={3}
                          disabled={saving}
                        />
                        <div class="flex justify-end gap-2">
                          <Button variant="secondary" size="sm" onclick={() => cancelEditing(comment.id)} disabled={saving}>
                            {$t`Cancel`}
                          </Button>
                          <Button
                            size="sm"
                            onclick={() => void saveEdit(comment.id)}
                            disabled={!editTextByCommentId[comment.id]?.trim()}
                            loading={saving}
                          >
                            {$t`Save`}
                          </Button>
                        </div>
                      </div>
                    {:else}
                      <p class={cn('whitespace-pre-wrap text-sm', !comment.text && 'text-muted-foreground')}>
                        {comment.text || $t`Empty comment`}
                      </p>
                    {/if}
                  </article>
                {/each}
              </div>

              <div class="space-y-2">
                <Label for={`reply-${threadView.thread.id}`}>{$t`Reply`}</Label>
                <Textarea
                  id={`reply-${threadView.thread.id}`}
                  bind:value={replyTextByThreadId[threadView.thread.id]}
                  placeholder={$t`Write a reply...`}
                  rows={2}
                  disabled={saving}
                />
                <div class="flex justify-end">
                  <Button
                    variant="secondary"
                    size="sm"
                    onclick={() => void replyToThread(threadView.thread.id)}
                    disabled={!replyTextByThreadId[threadView.thread.id]?.trim()}
                    loading={saving}
                  >
                    {$t`Reply`}
                  </Button>
                </div>
              </div>
            </section>
          {/each}
        {/if}
      </div>
    </div>
  </Dialog.DialogContent>
</Dialog.Root>
