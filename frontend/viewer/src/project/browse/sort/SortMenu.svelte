<script lang="ts" module>
  import {SortField} from '$lib/dotnet-types';
  import {msg} from 'svelte-i18n-lingui';
  import type {IconClass} from '$lib/icon-class';
  import type {SortConfig, SortDirection} from './options';

  const sortLabels = {
    [SortField.SearchRelevance]: msg`Best match`,
    [SortField.Headword]: msg`Headword`
  } as const;
  type SortLabel = (typeof sortLabels)[keyof typeof sortLabels];

  const directionIcons: Record<SortDirection, IconClass> = {
    asc: 'i-mdi-sort-alphabetical-ascending',
    desc: 'i-mdi-sort-alphabetical-descending'
  };

  type SortMenuOption = {
    config: SortConfig;
    /** Translatable label (static fields); mutually exclusive with labelText. */
    labelMsg?: SortLabel;
    /** Raw label (writing system names, which aren't translatable). */
    labelText?: string;
    /** Muted secondary text, e.g. the writing system id, to disambiguate names. */
    muted?: string;
    icon?: IconClass;
  };

  function configKey(config: SortConfig): string {
    return `${config.field}|${config.dir}|${config.writingSystem ?? ''}`;
  }

  function configEquals(a: SortConfig, b: SortConfig): boolean {
    return a.field === b.field && a.dir === b.dir && (a.writingSystem ?? undefined) === (b.writingSystem ?? undefined);
  }
</script>

<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {badgeVariants} from '$lib/components/ui/badge';
  import * as ResponsiveMenu from '$lib/components/responsive-menu';
  import {cn} from '$lib/utils';
  import {watch, type Getter} from 'runed';
  import {Icon} from '$lib/components/ui/icon';
  import {useWritingSystemService} from '$project/data';
  import {Button, buttonVariants} from '$lib/components/ui/button';

  type Props = {
    value?: SortConfig;
    autoSelector: Getter<SortField>;
  };

  let {
    value = $bindable(),
    autoSelector,
  }: Props = $props();

  const writingSystemService = useWritingSystemService();

  // undefined = "Auto": follow autoSelector, which picks best-match while searching else headword.
  let selected = $state<SortConfig>();
  const autoField = $derived(autoSelector());
  const effective = $derived<SortConfig>(selected ?? {field: autoField, dir: 'asc'});
  watch(() => effective, (config) => {
    value = config;
  });

  const options = $derived.by<SortMenuOption[]>(() => {
    const opts: SortMenuOption[] = [
      {config: {field: SortField.SearchRelevance, dir: 'asc'}, labelMsg: sortLabels[SortField.SearchRelevance]},
    ];
    // The default vernacular keeps the historical "Headword" label and an undefined writing
    // system (so it resolves to the project default the same way it always has).
    const defaultWsId = writingSystemService.defaultVernacular?.wsId;
    for (const dir of ['asc', 'desc'] as const) {
      opts.push({config: {field: SortField.Headword, dir}, labelMsg: sortLabels[SortField.Headword], icon: directionIcons[dir]});
    }
    // Every other vernacular writing system (excluding audio) is sortable by name.
    for (const ws of writingSystemService.vernacularNoAudio) {
      if (ws.wsId === defaultWsId) continue;
      for (const dir of ['asc', 'desc'] as const) {
        opts.push({config: {field: SortField.Headword, dir, writingSystem: ws.wsId}, labelText: ws.name, muted: ws.wsId, icon: directionIcons[dir]});
      }
    }
    return opts;
  });

  const activeOption = $derived(options.find(o => configEquals(o.config, effective)));
  const triggerText = $derived.by(() => {
    if (activeOption?.labelMsg) return $t(activeOption.labelMsg);
    if (activeOption?.labelText) return activeOption.labelText;
    return $t(sortLabels[effective.field]);
  });
  const triggerIcon = $derived(effective.field === SortField.Headword ? directionIcons[effective.dir] : 'i-mdi-arrow-down');
</script>

<ResponsiveMenu.Root>
  <ResponsiveMenu.Trigger class={cn(buttonVariants({variant: 'secondary', size: 'xs'}), badgeVariants({ variant: 'secondary' }), 'border-none h-7')}>
    {#snippet child({props})}
      <Button {...props}
        data-testid="sort-menu-trigger"
        icon={triggerIcon}
        iconProps={{ class: 'size-4' }}>
        {triggerText}
      </Button>
    {/snippet}
  </ResponsiveMenu.Trigger>
  <ResponsiveMenu.Content align="start">
    <ResponsiveMenu.Item
        onSelect={() => selected = undefined}
        class={cn(!selected && 'bg-muted')}
        >
        {$t`Auto`}
        <span class="text-muted-foreground ml-auto">
          ({$t(sortLabels[autoField])})
        </span>
    </ResponsiveMenu.Item>
    {#each options as option (configKey(option.config))}
      <ResponsiveMenu.Item
        onSelect={() => selected = option.config}
        class={cn(selected && configEquals(selected, option.config) && 'bg-muted')}
        >
        {#if option.icon}
          <Icon icon={option.icon} />
        {/if}
        {option.labelMsg ? $t(option.labelMsg) : option.labelText}
        {#if option.muted}
          <span class="text-muted-foreground ml-auto text-xs">{option.muted}</span>
        {/if}
      </ResponsiveMenu.Item>
    {/each}
  </ResponsiveMenu.Content>
</ResponsiveMenu.Root>
