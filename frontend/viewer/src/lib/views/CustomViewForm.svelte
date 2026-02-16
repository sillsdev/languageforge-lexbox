<script lang="ts" module>
  export type CustomViewFormValue = {
    label: string;
    baseViewId: 'fwlite' | 'fieldworks';
    fieldIds: import('$lib/entry-editor/field-data').FieldId[];
    overrides?: import('./view-data').Overrides;
  };

  export type CustomViewFormInitialValue = CustomViewFormValue & {
    id?: string;
  };
</script>

<script lang="ts">
  import * as RadioGroup from '$lib/components/ui/radio-group';
  import {Button} from '$lib/components/ui/button';
  import {Input} from '$lib/components/ui/input';
  import {Checkbox} from '$lib/components/ui/checkbox';
  import Icon from '$lib/components/ui/icon/icon.svelte';
  import {Reorderer} from '$lib/components/reorderer';
  import {dndzone} from 'svelte-dnd-action';
  import {t} from 'svelte-i18n-lingui';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {useWritingSystemService} from '$project/data';

  import {allFields, FW_CLASSIC_VIEW, FW_LITE_VIEW, type Overrides} from './view-data';
  import type {FieldId} from '$lib/entry-editor/field-data';
  import {pt, vt} from './view-text';
  type CustomViewFormInitialValue = import('./CustomViewForm.svelte').CustomViewFormInitialValue;
  type CustomViewFormValue = import('./CustomViewForm.svelte').CustomViewFormValue;

  // ensure TS doesn't elide the import (Lingui preprocess relies on it)
  void $t;

  interface Props {
    initialValue?: CustomViewFormInitialValue;
    resetToken?: number;
    submitLabel?: string;
    onSubmit: (result: CustomViewFormValue) => void | Promise<void>;
    onCancel: () => void;
  }

  let {
    initialValue,
    resetToken = 0,
    submitLabel = $t`Save`,
    onSubmit,
    onCancel
  }: Props = $props();

  let name = $state('');
  let baseViewId = $state<'fwlite' | 'fieldworks'>('fwlite');
  type FormErrors = {
    name?: string;
    fields?: string;
    vernacular?: string;
    analysis?: string;
    submit?: string;
  };

  const writingSystemService = useWritingSystemService();

  type WsMode = 'all' | 'custom';
  let vernacularMode = $state<WsMode>('all');
  let analysisMode = $state<WsMode>('all');
  let vernacularSelected = $state<string[]>([]);
  let analysisSelected = $state<string[]>([]);
  let vernacularOrder = $state<WsItem[]>([]);
  let analysisOrder = $state<WsItem[]>([]);

  const allVernacularIds = $derived(writingSystemService.vernacular.map((ws) => ws.wsId));
  const allAnalysisIds = $derived(writingSystemService.analysis.map((ws) => ws.wsId));
  const allVernacularIdSet = $derived(new Set(allVernacularIds));
  const allAnalysisIdSet = $derived(new Set(allAnalysisIds));

  function normalizeSelection(allIds: string[], selectedIds: string[]): string[] {
    const all: Record<string, true> = {};
    for (const id of allIds) {
      all[id] = true;
    }
    const normalized: string[] = [];
    for (const id of selectedIds) {
      if (!all[id] || normalized.includes(id)) continue;
      normalized.push(id);
    }
    return normalized;
  }

  function mergeOrderedAll(allIds: string[], prioritizedIds: string[]): string[] {
    const prioritized = normalizeSelection(allIds, prioritizedIds);
    const selectedSet = new Set(prioritized);
    return [...prioritized, ...allIds.filter((id) => !selectedSet.has(id))];
  }

  function orderedSelection(order: string[], selectedIds: string[]): string[] {
    const selected = new Set(normalizeSelection(order, selectedIds));
    return order.filter((id) => selected.has(id));
  }

  function isVernacularWsId(id: string): boolean {
    return allVernacularIdSet.has(id);
  }

  function isAnalysisWsId(id: string): boolean {
    return allAnalysisIdSet.has(id);
  }

  function ensureCustomWithAllSelected(kind: 'vernacular' | 'analysis') {
    if (kind === 'vernacular') {
      if (vernacularMode === 'all') {
        vernacularMode = 'custom';
        vernacularSelected = [...allVernacularIds];
      }
    } else {
      if (analysisMode === 'all') {
        analysisMode = 'custom';
        analysisSelected = [...allAnalysisIds];
      }
    }
  }

  function toggleWs(kind: 'vernacular' | 'analysis', wsId: string, checked: boolean) {
    ensureCustomWithAllSelected(kind);
    const currentOrder = kind === 'vernacular'
      ? normalizeSelection(allVernacularIds, vernacularOrder.map((i) => i.id))
      : normalizeSelection(allAnalysisIds, analysisOrder.map((i) => i.id));
    if (kind === 'vernacular') {
      const next = checked
        ? [...vernacularSelected, wsId]
        : vernacularSelected.filter((id) => id !== wsId);
      vernacularSelected = orderedSelection(currentOrder, next);
    } else {
      const next = checked
        ? [...analysisSelected, wsId]
        : analysisSelected.filter((id) => id !== wsId);
      analysisSelected = orderedSelection(currentOrder, next);
    }
  }

  type WsItem = { id: string };

  const vernacularById = $derived(new Map(writingSystemService.vernacular.map((ws) => [ws.wsId, ws])));
  const analysisById = $derived(new Map(writingSystemService.analysis.map((ws) => [ws.wsId, ws])));
  function sanitizeWsOrder(allIds: string[], items: WsItem[]): WsItem[] {
    return mergeOrderedAll(allIds, items.map((i) => i.id)).map((id) => ({id}));
  }

  function onVernacularOrderChange(nextOrder: WsItem[]) {
    vernacularOrder = sanitizeWsOrder(allVernacularIds, nextOrder);
    vernacularSelected = orderedSelection(vernacularOrder.map((i) => i.id), vernacularSelected);
  }

  function onAnalysisOrderChange(nextOrder: WsItem[]) {
    analysisOrder = sanitizeWsOrder(allAnalysisIds, nextOrder);
    analysisSelected = orderedSelection(analysisOrder.map((i) => i.id), analysisSelected);
  }

  type FieldItem = { id: string };

  const allFieldIds = Object.keys(allFields) as FieldId[];
  const allFieldIdSet = new Set<FieldId>(allFieldIds);

  function isFieldId(id: unknown): id is FieldId {
    return typeof id === 'string' && allFieldIdSet.has(id as FieldId);
  }

  function sanitizeFieldIds(items: Array<{id: unknown}>): FieldId[] {
    const seen = new Set<FieldId>();
    const sanitized: FieldId[] = [];
    for (const item of items) {
      if (!isFieldId(item.id)) continue;
      if (seen.has(item.id)) continue;
      seen.add(item.id);
      sanitized.push(item.id);
    }
    return sanitized;
  }

  const fieldLabels: Record<FieldId, ReturnType<typeof vt>> = {
    lexemeForm: vt($t`Lexeme form`, $t`Word`),
    citationForm: vt($t`Citation form`, $t`Display as`),
    complexForms: vt($t`Complex forms`, $t`Part of`),
    components: $t`Components`,
    complexFormTypes: vt($t`Complex form types`, $t`Uses components as`),
    literalMeaning: vt($t`Literal meaning`),
    note: vt($t`Note`),
    publishIn: vt($t`Publish entry in`, $t`Publish word in`),

    gloss: $t`Gloss`,
    definition: $t`Definition`,
    partOfSpeechId: vt($t`Grammatical info.`, $t`Part of speech`),
    semanticDomains: $t`Semantic domains`,

    sentence: $t`Sentence`,
    translations: $t`Translations`,
    reference: $t`Reference`,
  };

  let fieldOrder = $state<FieldItem[]>([]);
  let selectedFieldIds = $state<FieldId[]>([]);
  let fieldSelectionDirty = $state(false);
  let lastBaseViewId = $state<'fwlite' | 'fieldworks'>('fwlite');

  function setSelectedFromBase(baseId: 'fwlite' | 'fieldworks' = baseViewId) {
    const baseView = baseId === 'fieldworks' ? FW_CLASSIC_VIEW : FW_LITE_VIEW;
    const baseFields = baseView.fields as Record<FieldId, {show: boolean; order: number}>;
    const orderedByBase = [...allFieldIds].sort((a, b) => baseFields[a].order - baseFields[b].order);
    const fromBase = orderedByBase.filter((id) => baseFields[id].show);
    selectedFieldIds = normalizeSelection(allFieldIds, fromBase) as FieldId[];
    fieldOrder = orderedByBase.map((id) => ({id}));
  }

  function getViewTypeForBase(): 'fw-lite' | 'fw-classic' {
    return baseViewId === 'fieldworks' ? 'fw-classic' : 'fw-lite';
  }

  function fieldLabel(fieldId: FieldId): string {
    const label = fieldLabels[fieldId];
    if (!label) return fieldId;
    return pt(label, getViewTypeForBase());
  }

  function resetForm() {
    name = initialValue?.label ?? '';
    baseViewId = initialValue?.baseViewId ?? 'fwlite';

    if (initialValue?.fieldIds?.length) {
      selectedFieldIds = sanitizeFieldIds(initialValue.fieldIds.map((id) => ({id})));
      fieldOrder = mergeOrderedAll(allFieldIds, selectedFieldIds).map((id) => ({id}));
    } else {
      setSelectedFromBase();
    }
    fieldSelectionDirty = false;
    lastBaseViewId = baseViewId;

    const vernacularOverrides = initialValue?.overrides?.vernacularWritingSystems;
    const analysisOverrides = initialValue?.overrides?.analysisWritingSystems;

    vernacularMode = vernacularOverrides ? 'custom' : 'all';
    analysisMode = analysisOverrides ? 'custom' : 'all';
    vernacularSelected = normalizeSelection(allVernacularIds, vernacularOverrides ?? allVernacularIds);
    analysisSelected = normalizeSelection(allAnalysisIds, analysisOverrides ?? allAnalysisIds);
    vernacularOrder = mergeOrderedAll(allVernacularIds, vernacularSelected).map((id) => ({id}));
    analysisOrder = mergeOrderedAll(allAnalysisIds, analysisSelected).map((id) => ({id}));

    errors = {};
  }

  let lastResetToken = $state<number>(-1);
  $effect(() => {
    if (lastResetToken === resetToken) return;
    lastResetToken = resetToken;
    resetForm();
  });

  $effect(() => {
    if (baseViewId === lastBaseViewId) return;
    if (!fieldSelectionDirty) {
      setSelectedFromBase(baseViewId);
    }
    lastBaseViewId = baseViewId;
  });

  const orderedFieldIds = $derived(sanitizeFieldIds(fieldOrder as Array<{id: unknown}>));

  function onFieldOrderChange(nextOrder: FieldItem[]) {
    fieldOrder = mergeOrderedAll(allFieldIds, sanitizeFieldIds(nextOrder as Array<{id: unknown}>)).map((id) => ({id}));
    selectedFieldIds = orderedSelection(orderedFieldIds, selectedFieldIds) as FieldId[];
    fieldSelectionDirty = true;
  }

  let fieldOrderViewport = $state<HTMLElement | null>(null);
  let vernacularOrderViewport = $state<HTMLElement | null>(null);
  let analysisOrderViewport = $state<HTMLElement | null>(null);
  let dragging = $state(false);
  let draggedItemFocusKey = $state<string | undefined>(undefined);

  function preserveScroll(viewport: HTMLElement | null, update: () => void) {
    const top = viewport?.scrollTop ?? 0;
    update();
    queueMicrotask(() => {
      if (viewport) viewport.scrollTop = top;
      requestAnimationFrame(() => {
        if (viewport) viewport.scrollTop = top;
      });
    });
  }

  function startDragFocusGuard(viewport: HTMLElement | null) {
    if (dragging) return;
    dragging = true;
    const active = document.activeElement as HTMLElement | null;
    if (active && viewport?.contains(active)) {
      draggedItemFocusKey = active.closest<HTMLElement>('[data-reorder-id]')?.dataset.reorderId;
      active.blur();
    } else {
      draggedItemFocusKey = undefined;
    }
  }

  function endDragFocusGuard(viewport: HTMLElement | null) {
    const key = draggedItemFocusKey;
    dragging = false;
    draggedItemFocusKey = undefined;
    queueMicrotask(() => {
      const target = key ? viewport?.querySelector<HTMLElement>(`[data-reorder-id="${key}"]`) : undefined;
      if (target) {
        target.focus({preventScroll: true});
      } else {
        viewport?.focus({preventScroll: true});
      }
    });
  }

  function considerFieldDrag(e: CustomEvent<{items: FieldItem[] }>) {
    startDragFocusGuard(fieldOrderViewport);
    preserveScroll(fieldOrderViewport, () => {
      fieldOrder = e.detail.items;
    });
  }

  function finalizeFieldDrag(e: CustomEvent<{items: FieldItem[] }>) {
    preserveScroll(fieldOrderViewport, () => {
      onFieldOrderChange(e.detail.items);
    });
    endDragFocusGuard(fieldOrderViewport);
  }

  function considerVernacularDrag(e: CustomEvent<{items: WsItem[] }>) {
    startDragFocusGuard(vernacularOrderViewport);
    preserveScroll(vernacularOrderViewport, () => {
      vernacularOrder = e.detail.items;
    });
  }

  function finalizeVernacularDrag(e: CustomEvent<{items: WsItem[] }>) {
    preserveScroll(vernacularOrderViewport, () => {
      onVernacularOrderChange(e.detail.items);
    });
    endDragFocusGuard(vernacularOrderViewport);
  }

  function considerAnalysisDrag(e: CustomEvent<{items: WsItem[] }>) {
    startDragFocusGuard(analysisOrderViewport);
    preserveScroll(analysisOrderViewport, () => {
      analysisOrder = e.detail.items;
    });
  }

  function finalizeAnalysisDrag(e: CustomEvent<{items: WsItem[] }>) {
    preserveScroll(analysisOrderViewport, () => {
      onAnalysisOrderChange(e.detail.items);
    });
    endDragFocusGuard(analysisOrderViewport);
  }

  function toggleField(fieldId: FieldId, checked: boolean) {
    if (checked) {
      if (selectedFieldIds.includes(fieldId)) return;
      selectedFieldIds = orderedSelection(orderedFieldIds, [...selectedFieldIds, fieldId]) as FieldId[];
    } else {
      selectedFieldIds = selectedFieldIds.filter((id) => id !== fieldId);
    }
    fieldSelectionDirty = true;
  }

  function fieldDisplayName(item: FieldItem): string {
    if (!isFieldId(item.id)) return item.id;
    return fieldLabel(item.id);
  }

  function wsDisplayName(byId: Map<string, {name: string; abbreviation: string}>, item: WsItem): string {
    const ws = byId.get(item.id);
    return ws ? `${ws.name} (${ws.abbreviation})` : item.id;
  }

  let errors = $state<FormErrors>({});
  let saving = $state(false);
  const validationErrors = $derived([errors.name, errors.fields, errors.vernacular, errors.analysis, errors.submit].filter((e): e is string => !!e));

  async function submit() {
    errors = {};
    const label = name.trim();
    if (!label) {
      errors.name = 'Name is required';
      return;
    }
    if (selectedFieldIds.length === 0) {
      errors.fields = 'Select at least one field';
      return;
    }

    if (vernacularMode === 'custom' && vernacularSelected.length === 0) {
      errors.vernacular = 'Select at least one vernacular writing system';
      return;
    }
    if (analysisMode === 'custom' && analysisSelected.length === 0) {
      errors.analysis = 'Select at least one analysis writing system';
      return;
    }

    const overrides: Overrides = {
      vernacularWritingSystems: vernacularMode === 'custom' ? normalizeSelection(allVernacularIds, vernacularSelected) : undefined,
      analysisWritingSystems: analysisMode === 'custom' ? normalizeSelection(allAnalysisIds, analysisSelected) : undefined,
    };
    const overridesOrUndefined = (overrides.analysisWritingSystems || overrides.vernacularWritingSystems) ? overrides : undefined;

    saving = true;
    try {
      await onSubmit({
        label,
        baseViewId,
        fieldIds: orderedSelection(orderedFieldIds, selectedFieldIds) as FieldId[],
        overrides: overridesOrUndefined,
      });
    } catch (e) {
      errors.submit = e instanceof Error ? e.message : 'Failed to save custom view';
    } finally {
      saving = false;
    }
  }
</script>

<form class="flex flex-col gap-4" onsubmit={(e) => { e.preventDefault(); void submit(); }}>
  <div class="flex flex-col gap-2">
    <div class="text-sm font-medium">{$t`Name`}</div>
    <Input bind:value={name} placeholder={$t`e.g. Minimal editing`} />
  </div>

  <div class="flex flex-col gap-2">
    <div class="text-sm font-medium">{$t`Based on`}</div>
    <RadioGroup.Root bind:value={baseViewId}>
      <RadioGroup.Item value="fwlite" label={FW_LITE_VIEW.label} />
      <RadioGroup.Item value="fieldworks" label={FW_CLASSIC_VIEW.label} />
    </RadioGroup.Root>
  </div>

  <div class="flex flex-col gap-2">
    <div class="text-sm font-medium">{$t`Fields`}</div>

    <div class="rounded-md border p-2">
      <div class="text-xs text-muted-foreground mb-2">{$t`Order (drag to reorder)`}</div>
      {#if !IsMobile.value}
        <div
          bind:this={fieldOrderViewport}
          tabindex="-1"
          class="flex flex-col gap-1"
          use:dndzone={{
            items: fieldOrder,
            type: 'custom-view-fields',
            dropFromOthersDisabled: true,
            flipDurationMs: 0,
            dropAnimationDisabled: true,
            zoneTabIndex: -1,
            zoneItemTabIndex: -1
          }}
          onconsider={considerFieldDrag}
          onfinalize={finalizeFieldDrag}
        >
          {#each fieldOrder as item (item.id)}
            {#if isFieldId(item.id)}
              {@const fieldId = item.id as FieldId}
              <div data-reorder-id={`field:${fieldId}`} tabindex="-1" class="flex items-center gap-2 rounded-sm px-2 py-1 hover:bg-muted">
                <Icon icon="i-mdi-drag-vertical" class="text-muted-foreground/70" />
                <Checkbox checked={selectedFieldIds.includes(fieldId)} onCheckedChange={(checked) => toggleField(fieldId, !!checked)} />
                <span class="text-sm truncate">{fieldLabel(fieldId)}</span>
              </div>
            {:else}
              <div class="h-7" />
            {/if}
          {/each}
        </div>
      {:else}
        <div class="flex flex-col gap-1">
          {#each fieldOrder as item (item.id)}
            {#if isFieldId(item.id)}
              {@const fieldId = item.id as FieldId}
              <div class="flex items-center gap-2 rounded-sm px-2 py-1 hover:bg-muted justify-between">
              <div class="flex items-center gap-2 min-w-0">
                <Checkbox checked={selectedFieldIds.includes(fieldId)} onCheckedChange={(checked) => toggleField(fieldId, !!checked)} />
                <span class="text-sm truncate">{fieldLabel(fieldId)}</span>
              </div>
                <Reorderer
                  direction="vertical"
                  item={item}
                  bind:items={fieldOrder}
                  getDisplayName={fieldDisplayName}
                  onchange={(nextOrder) => onFieldOrderChange(nextOrder)}
                />
              </div>
            {:else}
              <div class="h-7" />
            {/if}
          {/each}
        </div>
      {/if}
    </div>
  </div>

  <div class="flex flex-col gap-2">
    <div class="text-sm font-medium">{$t`Vernacular writing systems`}</div>
    <RadioGroup.Root bind:value={vernacularMode}>
      <RadioGroup.Item value="all" label={$t`Default`} />
      <RadioGroup.Item value="custom" label={$t`Custom`} />
    </RadioGroup.Root>

    <div class="rounded-md border p-2">
      <div class="text-xs text-muted-foreground mb-2">{$t`Order (drag to reorder)`}</div>
      {#if !IsMobile.value}
        <div
          bind:this={vernacularOrderViewport}
          tabindex="-1"
          class="flex flex-col gap-1"
          use:dndzone={{
            items: vernacularOrder,
            type: 'custom-view-vernacular-ws',
            dropFromOthersDisabled: true,
            flipDurationMs: 0,
            dropAnimationDisabled: true,
            dragDisabled: vernacularMode === 'all',
            zoneTabIndex: -1,
            zoneItemTabIndex: -1
          }}
          onconsider={considerVernacularDrag}
          onfinalize={finalizeVernacularDrag}
        >
          {#each vernacularOrder as item (item.id)}
            {#if isVernacularWsId(item.id)}
              {@const ws = vernacularById.get(item.id)}
              <div data-reorder-id={`vern:${item.id}`} tabindex="-1" class="flex items-center gap-2 rounded-sm px-2 py-1 hover:bg-muted">
                <Icon icon="i-mdi-drag-vertical" class={vernacularMode === 'all' ? 'text-muted-foreground/30' : 'text-muted-foreground/70'} />
                <Checkbox checked={vernacularMode === 'all' || vernacularSelected.includes(item.id)} disabled={vernacularMode === 'all'} onCheckedChange={(checked) => toggleWs('vernacular', item.id, !!checked)} />
                <span class="text-sm truncate">{ws?.name} ({ws?.abbreviation})</span>
              </div>
            {:else}
              <div class="h-7" />
            {/if}
          {/each}
        </div>
      {:else}
        <div class="flex flex-col gap-1">
          {#each vernacularOrder as item (item.id)}
            {#if isVernacularWsId(item.id)}
              {@const ws = vernacularById.get(item.id)}
              <div class="flex items-center gap-2 rounded-sm px-2 py-1 hover:bg-muted justify-between">
                <div class="flex items-center gap-2 min-w-0">
                  <Checkbox checked={vernacularMode === 'all' || vernacularSelected.includes(item.id)} disabled={vernacularMode === 'all'} onCheckedChange={(checked) => toggleWs('vernacular', item.id, !!checked)} />
                  <span class="text-sm truncate">{ws?.name} ({ws?.abbreviation})</span>
                </div>
                {#if vernacularMode === 'custom'}
                  <Reorderer
                    direction="vertical"
                    item={item}
                    bind:items={vernacularOrder}
                    getDisplayName={(x) => wsDisplayName(vernacularById, x)}
                    onchange={(nextOrder) => onVernacularOrderChange(nextOrder)}
                  />
                {/if}
              </div>
            {:else}
              <div class="h-7" />
            {/if}
          {/each}
        </div>
      {/if}
    </div>
  </div>

  <div class="flex flex-col gap-2">
    <div class="text-sm font-medium">{$t`Analysis writing systems`}</div>
    <RadioGroup.Root bind:value={analysisMode}>
      <RadioGroup.Item value="all" label={$t`Default`} />
      <RadioGroup.Item value="custom" label={$t`Custom`} />
    </RadioGroup.Root>

    <div class="rounded-md border p-2">
      <div class="text-xs text-muted-foreground mb-2">{$t`Order (drag to reorder)`}</div>
      {#if !IsMobile.value}
        <div
          bind:this={analysisOrderViewport}
          tabindex="-1"
          class="flex flex-col gap-1"
          use:dndzone={{
            items: analysisOrder,
            type: 'custom-view-analysis-ws',
            dropFromOthersDisabled: true,
            flipDurationMs: 0,
            dropAnimationDisabled: true,
            dragDisabled: analysisMode === 'all',
            zoneTabIndex: -1,
            zoneItemTabIndex: -1
          }}
          onconsider={considerAnalysisDrag}
          onfinalize={finalizeAnalysisDrag}
        >
          {#each analysisOrder as item (item.id)}
            {#if isAnalysisWsId(item.id)}
              {@const ws = analysisById.get(item.id)}
              <div data-reorder-id={`anal:${item.id}`} tabindex="-1" class="flex items-center gap-2 rounded-sm px-2 py-1 hover:bg-muted">
                <Icon icon="i-mdi-drag-vertical" class={analysisMode === 'all' ? 'text-muted-foreground/30' : 'text-muted-foreground/70'} />
                <Checkbox checked={analysisMode === 'all' || analysisSelected.includes(item.id)} disabled={analysisMode === 'all'} onCheckedChange={(checked) => toggleWs('analysis', item.id, !!checked)} />
                <span class="text-sm truncate">{ws?.name} ({ws?.abbreviation})</span>
              </div>
            {:else}
              <div class="h-7" />
            {/if}
          {/each}
        </div>
      {:else}
        <div class="flex flex-col gap-1">
          {#each analysisOrder as item (item.id)}
            {#if isAnalysisWsId(item.id)}
              {@const ws = analysisById.get(item.id)}
              <div class="flex items-center gap-2 rounded-sm px-2 py-1 hover:bg-muted justify-between">
                <div class="flex items-center gap-2 min-w-0">
                  <Checkbox checked={analysisMode === 'all' || analysisSelected.includes(item.id)} disabled={analysisMode === 'all'} onCheckedChange={(checked) => toggleWs('analysis', item.id, !!checked)} />
                  <span class="text-sm truncate">{ws?.name} ({ws?.abbreviation})</span>
                </div>
                {#if analysisMode === 'custom'}
                  <Reorderer
                    direction="vertical"
                    item={item}
                    bind:items={analysisOrder}
                    getDisplayName={(x) => wsDisplayName(analysisById, x)}
                    onchange={(nextOrder) => onAnalysisOrderChange(nextOrder)}
                  />
                {/if}
              </div>
            {:else}
              <div class="h-7" />
            {/if}
          {/each}
        </div>
      {/if}
    </div>
  </div>

  {#if validationErrors.length > 0}
    <div class="rounded-md border border-destructive/40 bg-destructive/5 p-2">
      {#each validationErrors as err (err)}
        <p class="text-sm text-destructive">{err}</p>
      {/each}
    </div>
  {/if}

  <div class="flex justify-end gap-2">
    <Button type="button" variant="outline" onclick={onCancel} disabled={saving}>{$t`Cancel`}</Button>
    <Button type="submit" disabled={saving}>{submitLabel}</Button>
  </div>
</form>
