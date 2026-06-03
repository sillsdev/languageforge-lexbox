<script lang="ts" module>
  export type AuthorFilterValue =
    | {kind: 'all'}
    | {kind: 'fieldWorks'}
    | {kind: 'me'; authorName: string}
    | {kind: 'fwLiteUsers'}
    | {kind: 'author'; authorName: string}
    | {kind: 'missing'};

  const FIELD_WORKS_AUTHOR_NAME = 'FieldWorks';

  export function serializeAuthorFilter(value: AuthorFilterValue): string {
    switch (value.kind) {
      case 'all':           return '';
      case 'fieldWorks':    return 'fieldWorks';
      case 'fwLiteUsers':   return 'fwLiteUsers';
      case 'missing':       return 'missing';
      case 'me':            return `me:${value.authorName}`;
      case 'author':        return `author:${value.authorName}`;
    }
  }

  export function deserializeAuthorFilter(raw: string): AuthorFilterValue {
    if (!raw) return {kind: 'all'};
    if (raw === 'fieldWorks') return {kind: 'fieldWorks'};
    if (raw === 'fwLiteUsers') return {kind: 'fwLiteUsers'};
    if (raw === 'missing') return {kind: 'missing'};
    if (raw.startsWith('me:')) return {kind: 'me', authorName: raw.slice('me:'.length)};
    if (raw.startsWith('author:')) return {kind: 'author', authorName: raw.slice('author:'.length)};
    return {kind: 'all'};
  }

  export function authorFilterToActivityFilter(value: AuthorFilterValue): IProjectActivityFilter | undefined {
    switch (value.kind) {
      case 'all':         return undefined;
      case 'fieldWorks':  return {authorName: FIELD_WORKS_AUTHOR_NAME, authorMissing: false, excludeFieldWorks: false};
      case 'fwLiteUsers': return {authorMissing: false, excludeFieldWorks: true};
      case 'missing':     return {authorMissing: true, excludeFieldWorks: false};
      case 'me':          return {authorName: value.authorName, authorMissing: false, excludeFieldWorks: false};
      case 'author':      return {authorName: value.authorName, authorMissing: false, excludeFieldWorks: false};
    }
  }
</script>

<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import * as Select from '$lib/components/ui/select';
  import {Icon} from '$lib/components/ui/icon';
  import flexLogo from '$lib/assets/flex-logo.png';
  import {useProjectContext} from '$project/project-context.svelte';
  import {useActivityAuthors} from './activity-authors.svelte';
  import type {IProjectActivityFilter} from '$lib/dotnet-types';

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
      case 'me':          return $t`Me (${value.authorName})`;
      case 'fwLiteUsers': return $t`FieldWorks Lite users`;
      case 'author':      return value.authorName;
      case 'missing':     return $t`(Unknown author)`;
    }
  });

  function setValue(next: AuthorFilterValue) {
    onchange(next);
  }

  // shadcn Select needs a unique string id for each option so we can map back on select.
  function optionId(v: AuthorFilterValue): string {
    return serializeAuthorFilter(v) || 'all';
  }
  const currentId = $derived(optionId(value));

  const otherAuthorOptions = $derived(
    groups.otherAuthors.map(name => ({id: optionId({kind: 'author', authorName: name}), name})),
  );
  const fieldWorksId = optionId({kind: 'fieldWorks'});
  const fwLiteUsersId = optionId({kind: 'fwLiteUsers'});
  const allId = optionId({kind: 'all'});
  const missingId = optionId({kind: 'missing'});
  const meId = $derived(groups.me ? optionId({kind: 'me', authorName: groups.me}) : undefined);

  function onValueChange(id: string) {
    if (id === allId) return setValue({kind: 'all'});
    if (id === fieldWorksId) return setValue({kind: 'fieldWorks'});
    if (id === fwLiteUsersId) return setValue({kind: 'fwLiteUsers'});
    if (id === missingId) return setValue({kind: 'missing'});
    if (meId && id === meId && groups.me) return setValue({kind: 'me', authorName: groups.me});
    const match = otherAuthorOptions.find(o => o.id === id);
    if (match) return setValue({kind: 'author', authorName: match.name});
  }
</script>

<Select.Root type="single" value={currentId} {onValueChange}>
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
    <Select.Item value={allId}>
      <span>{$t`All authors`}</span>
    </Select.Item>

    {#if groups.hasFieldWorks}
      <Select.Item value={fieldWorksId}>
        <span class="flex items-center justify-between gap-3 w-full">
          <span>{$t`FieldWorks`}</span>
          <Icon src={flexLogo} alt="" class="size-4 shrink-0" />
        </span>
      </Select.Item>
    {/if}

    {#if groups.me && meId}
      <Select.Item value={meId}>
        <span class="flex items-center gap-2">
          <span>{$t`Me`}</span>
          <span class="text-muted-foreground">({groups.me})</span>
        </span>
      </Select.Item>
    {/if}

    {#if groups.showFwLiteUsersBucket}
      <Select.Item value={fwLiteUsersId}>
        <span>{$t`FieldWorks Lite users`}</span>
      </Select.Item>
    {/if}

    {#if otherAuthorOptions.length > 0}
      <Select.Separator />
      {#each otherAuthorOptions as option (option.id)}
        <Select.Item value={option.id} label={option.name}>
          <span>{option.name}</span>
        </Select.Item>
      {/each}
    {/if}

    {#if import.meta.env.DEV && groups.hasMissing}
      <Select.Separator />
      <Select.Item value={missingId}>
        <span class="italic text-muted-foreground">{$t`(Unknown author)`}</span>
      </Select.Item>
    {/if}
  </Select.Content>
</Select.Root>
