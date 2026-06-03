<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import * as Select from '$lib/components/ui/select';
  import {Icon} from '$lib/components/ui/icon';
  import flexLogo from '$lib/assets/flex-logo.png';
  import {useProjectContext} from '$project/project-context.svelte';
  import {useActivityAuthors} from './activity-authors.svelte';
  import {
    type AuthorFilterValue,
    FIELD_WORKS_AUTHOR_NAME,
    serializeAuthorFilter,
    deserializeAuthorFilter,
  } from './author-filter';

  let {value, onchange}: {value: AuthorFilterValue; onchange: (next: AuthorFilterValue) => void} = $props();

  const projectContext = useProjectContext();
  const authorsService = useActivityAuthors();

  const meAuthorName = $derived(projectContext.projectData?.lastUserName ?? undefined);

  // Group authors into special buckets + the long tail of "everyone else".
  // Authors list is loaded lazily by ActivityAuthorsService; it starts empty.
  const groups = $derived.by(() => {
    const authors = authorsService.current;
    const hasFieldWorks = authors.includes(FIELD_WORKS_AUTHOR_NAME);
    const hasMissing = authors.some(a => !a);
    const me = meAuthorName && authors.includes(meAuthorName) ? meAuthorName : undefined;
    const otherAuthors = authors
      .filter((a): a is string => !!a && a !== FIELD_WORKS_AUTHOR_NAME && a !== me)
      .sort((a, b) => a.localeCompare(b));
    // Only offer "FieldWorks Lite users" when it's a meaningful subset
    // (i.e. there are both FieldWorks and non-FieldWorks commits).
    const showFwLiteUsersBucket = hasFieldWorks && (me !== undefined || otherAuthors.length > 0);
    return {hasFieldWorks, me, showFwLiteUsersBucket, otherAuthors, hasMissing};
  });

  const triggerLabel = $derived.by(() => {
    switch (value.kind) {
      case 'all':         return $t`All authors`;
      case 'fieldWorks':  return $t`FieldWorks`;
      case 'fwLiteUsers': return $t`FieldWorks Lite users`;
      case 'missing':     return $t`(Unknown author)`;
      case 'author':
        return value.authorName === meAuthorName ? $t`Me (${value.authorName})` : value.authorName;
    }
  });

  // shadcn Select needs a unique non-empty string id per option. We use the serialized form
  // and substitute 'all' for the empty string ('all' is never a valid serialized author name).
  const ALL_ID = 'all';
  function optionId(v: AuthorFilterValue): string {
    return serializeAuthorFilter(v) || ALL_ID;
  }
  function fromOptionId(id: string): AuthorFilterValue {
    return deserializeAuthorFilter(id === ALL_ID ? '' : id);
  }
  const currentId = $derived(optionId(value));
</script>

<Select.Root type="single" value={currentId} onValueChange={(id) => onchange(fromOptionId(id))}>
  <Select.Trigger
    size="sm"
    class="w-auto min-w-44 max-w-[16rem] gap-2"
    aria-label={$t`Filter by author`}
  >
    <span class="flex items-center gap-2 min-w-0">
      <Icon icon="i-mdi-account-outline" class="opacity-70 shrink-0" />
      <span class="truncate">{triggerLabel}</span>
      {#if value.kind === 'fieldWorks'}
        <Icon src={flexLogo} alt="" class="size-4 shrink-0" />
      {/if}
    </span>
  </Select.Trigger>
  <Select.Content align="start" class="max-h-[60vh]">
    <Select.Item value={optionId({kind: 'all'})}>
      <span>{$t`All authors`}</span>
    </Select.Item>

    {#if groups.hasFieldWorks}
      <Select.Item value={optionId({kind: 'fieldWorks'})}>
        <span class="flex items-center justify-between gap-3 w-full">
          <span>{$t`FieldWorks`}</span>
          <Icon src={flexLogo} alt="" class="size-4 shrink-0" />
        </span>
      </Select.Item>
    {/if}

    {#if groups.me}
      <Select.Item value={optionId({kind: 'author', authorName: groups.me})} label={groups.me}>
        <span class="flex items-center gap-2">
          <span>{$t`Me`}</span>
          <span class="text-muted-foreground">({groups.me})</span>
        </span>
      </Select.Item>
    {/if}

    {#if groups.showFwLiteUsersBucket}
      <Select.Item value={optionId({kind: 'fwLiteUsers'})}>
        <span>{$t`FieldWorks Lite users`}</span>
      </Select.Item>
    {/if}

    {#if groups.otherAuthors.length > 0}
      <Select.Separator />
      {#each groups.otherAuthors as name (name)}
        <Select.Item value={optionId({kind: 'author', authorName: name})} label={name}>
          <span>{name}</span>
        </Select.Item>
      {/each}
    {/if}

    {#if import.meta.env.DEV && groups.hasMissing}
      <Select.Separator />
      <Select.Item value={optionId({kind: 'missing'})}>
        <span class="italic text-muted-foreground">{$t`(Unknown author)`}</span>
      </Select.Item>
    {/if}
  </Select.Content>
</Select.Root>
