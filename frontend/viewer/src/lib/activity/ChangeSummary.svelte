<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Icon} from '$lib/components/ui/icon';
  import {getEntityConfig} from '$lib/views/entity-config';
  import {pt, tvt, type ViewText} from '$lib/views/view-text';
  import {useViewService} from '$lib/views/view-service.svelte';
  import type {BulkNoun, ChangeFact, CollectionKind, ObjectKind, SummaryEntity} from './change-summary';

  // `hideSubject`: the subject is already shown as a group header (Detailed mode), so suppress the leading
  // subject token and render just the verb phrase. Only affects non-self-naming facts (self-naming facts
  // weave the subject into their own sentence and are never grouped under a header).
  let {fact, subject, target, hideSubject = false}: {fact: ChangeFact; subject?: string; target?: string; hideSubject?: boolean} = $props();

  const viewService = useViewService();

  // These kinds weave the subject into their own sentence ("Created entry X"); the rest get it as a leading chip.
  // Sense- and example-creates are self-naming for `delete` (the deleted entity's own label) but NOT for `create`:
  // there the subject is the *parent* entry, so we want it as a leading chip — "gwa₁ · Added sense apple".
  const selfNaming = $derived(
    (fact.kind === 'create' && fact.entity === 'entry')
    || fact.kind === 'createObject' || fact.kind === 'editObject'
    || fact.kind === 'editObjectField' || fact.kind === 'deleteObject' || fact.kind === 'delete',
  );

  function capitalize(text: string): string {
    return text.charAt(0).toUpperCase() + text.slice(1);
  }

  function resolve(text: ViewText): string {
    return pt($tvt(text), viewService.currentView);
  }

  function fieldLabel(entity: SummaryEntity, fieldId: string): string {
    const config = getEntityConfig(entity) as Record<string, {label?: ViewText}>;
    const label = config[fieldId]?.label;
    return label ? resolve(label) : fieldId;
  }

  function entityName(entity: SummaryEntity): string {
    return resolve(getEntityConfig(entity).$label);
  }

  // View-aware entity nouns — Classic "entry/sense" vs Lite "word/meaning" — so the log matches the field labels and the rest of the app.
  function entityNoun(entity: SummaryEntity): string {
    const nouns: Record<SummaryEntity, string> = {
      entry: pt($t`entry`, $t`word`, viewService.currentView),
      sense: pt($t`sense`, $t`meaning`, viewService.currentView),
      example: $t`example`,
    };
    return nouns[entity];
  }

  function collectionNoun(collection: CollectionKind): string {
    const nouns: Record<CollectionKind, string> = {
      senses: pt($t`senses`, $t`meanings`, viewService.currentView),
      examples: $t`examples`,
      components: $t`components`,
      writingSystems: $t`writing systems`,
    };
    return nouns[collection];
  }

  // Plural noun for a bulk-create collapse ("Created 100 semantic domains").
  function bulkNoun(noun: BulkNoun): string {
    const nouns: Record<BulkNoun, string> = {
      entries: pt($t`entries`, $t`words`, viewService.currentView),
      senses: pt($t`senses`, $t`meanings`, viewService.currentView),
      examples: $t`examples`,
      partsOfSpeech: $t`parts of speech`,
      semanticDomains: $t`semantic domains`,
      publications: $t`publications`,
      complexFormTypes: $t`complex form types`,
      morphTypes: $t`morph types`,
      writingSystems: $t`writing systems`,
      customViews: $t`custom views`,
    };
    return nouns[noun];
  }

  // Singular noun for one reordered item, so we can name it ("Reordered sense apple").
  function collectionItemNoun(collection: CollectionKind): string {
    const nouns: Record<CollectionKind, string> = {
      senses: pt($t`sense`, $t`meaning`, viewService.currentView),
      examples: $t`example`,
      components: $t`component`,
      writingSystems: $t`writing system`,
    };
    return nouns[collection];
  }

  function objectNoun(object: ObjectKind): string {
    const nouns: Record<ObjectKind, string> = {
      partOfSpeech: $t`part of speech`,
      semanticDomain: $t`semantic domain`,
      publication: $t`publication`,
      complexFormType: $t`complex form type`,
      writingSystem: $t`writing system`,
      morphType: $t`morph type`,
      customView: $t`custom view`,
    };
    return nouns[object];
  }

  // The singular noun for one item of a list field, so adds/removes read as "Added semantic domain X".
  // Unmapped fields fall back to the plural field label ("Added to <label>").
  function itemNoun(fieldId: string): string | undefined {
    const nouns: Record<string, string> = {
      semanticDomains: $t`semantic domain`,
      complexFormTypes: $t`complex form type`,
      publishIn: $t`publication`,
      translations: $t`translation`,
      components: $t`component`,
    };
    return nouns[fieldId];
  }
</script>

<!-- Visual hierarchy: the SUBJECT (the entry/object the change is about) is the dominant scannable token —
  bold foreground text, no chrome. The verb phrase inherits the container's muted colour. DATA values the
  change carries render as bordered `chip`s. So a line reads: **subject**  muted verb phrase  [boxed data].
  `subjectToken` carries a trailing margin (mid-line separation without a middot); `chip` a leading one. -->
{#snippet subjectToken(text: string)}<span class="me-1 font-semibold text-foreground">{text}</span>{/snippet}
{#snippet chip(text: string)}<span class="ms-1 rounded border border-border bg-background px-1 font-medium text-foreground">{text}</span>{/snippet}
{#snippet wsCode(ws: string)}<span class="ms-1 font-mono text-xs text-muted-foreground">{ws}</span>{/snippet}
{#snippet noHeadword()}<span class="ms-1 italic text-muted-foreground">{$t`(no headword)`}</span>{/snippet}
<!-- Audio marker: a media value has no readable text, so show an icon + "audio" chip instead of a URI. -->
{#snippet audioNote()}<span class="ms-1 inline-flex items-center gap-1 rounded border border-border bg-background px-1 font-medium text-foreground"><Icon icon="i-mdi-volume-high" class="size-3.5" />{$t`audio`}</span>{/snippet}

{#if subject && !selfNaming && !hideSubject}{@render subjectToken(subject)}{/if}{#if fact.kind === 'setField'}
  {@const label = fieldLabel(fact.entity, fact.fieldId)}
  {#if fact.audio}{$t`Set ${label}`}{@render audioNote()}{:else}{$t`Set ${label} to`}{@render chip(fact.value)}{#if fact.ws}{@render wsCode(fact.ws)}{/if}{/if}
{:else if fact.kind === 'setHomograph'}
  {$t`Set homograph number to`}{@render chip(fact.value)}
{:else if fact.kind === 'clearField'}
  {@const label = fieldLabel(fact.entity, fact.fieldId)}
  {$t`Cleared ${label}`}{#if fact.audio}{@render audioNote()}{:else if fact.ws}{@render wsCode(fact.ws)}{/if}
{:else if fact.kind === 'changeField'}
  {@const label = fieldLabel(fact.entity, fact.fieldId)}
  {#if target}{$t`Changed ${label} to`}{@render chip(target)}{:else}{$t`Changed ${label}`}{/if}
{:else if fact.kind === 'addItem'}
  {@const noun = itemNoun(fact.fieldId)}
  {#if noun}
    {$t`Added ${noun}`}{#if fact.label}{@render chip(fact.label)}{/if}
  {:else}
    {@const label = fieldLabel(fact.entity, fact.fieldId)}
    {#if fact.label}{$t`Added to ${label}`}:{@render chip(fact.label)}{:else}{$t`Added to ${label}`}{/if}
  {/if}
{:else if fact.kind === 'removeItem'}
  {@const noun = itemNoun(fact.fieldId)}
  {#if noun}
    {$t`Removed ${noun}`}{#if target}{@render chip(target)}{/if}
  {:else}
    {$t`Removed from ${fieldLabel(fact.entity, fact.fieldId)}`}
  {/if}
{:else if fact.kind === 'replaceItem'}
  {@const label = itemNoun(fact.fieldId) ?? fieldLabel(fact.entity, fact.fieldId)}
  {#if fact.label}{$t`Changed ${label} to`}{@render chip(fact.label)}{:else}{$t`Changed ${label}`}{/if}
{:else if fact.kind === 'create'}
  {#if fact.entity === 'entry'}
    {@const name = subject ?? fact.label}
    {$t`Created ${entityNoun('entry')}`}{#if name}{@render chip(name)}{:else}{@render noHeadword()}{/if}
  {:else if fact.entity === 'sense'}
    <!-- "headword · Added sense senseN". Subject (parent entry headword) renders as leading chip via selfNaming=false;
         `target` is the sense identifier from the backend (SenseGlossPart — the gloss when present, "senseN" subscript otherwise). -->
    {@const name = target ?? fact.label}
    {$t`Added ${entityNoun('sense')}`}{#if name}{@render chip(name)}{/if}
  {:else}
    <!-- "headword › gloss · Added example". Subject is the parent sense's SenseLabel (headword › gloss); leading chip.
         An audio-only example has no readable sentence text, so show the audio marker instead of a media URI. -->
    {@const name = fact.label}
    {$t`Added ${entityNoun('example')}`}{#if name}{@render chip(name)}{:else if fact.audioOnly}{@render audioNote()}{/if}
  {/if}
{:else if fact.kind === 'delete'}
  {#if subject}
    {#if fact.entity === 'entry'}{$t`Deleted ${entityNoun('entry')}`}{@render chip(subject)}
    {:else if fact.entity === 'sense'}{$t`Deleted ${entityNoun('sense')}`}{@render chip(subject)}
    {:else}{$t`Deleted ${entityNoun('example')} from`}{@render chip(subject)}{/if}
  {:else}{$t`Deleted ${entityName(fact.entity)}`}{/if}
{:else if fact.kind === 'reorder'}
  {#if target}{$t`Reordered ${collectionItemNoun(fact.collection)}`}{@render chip(target)}{:else}{$t`Reordered ${collectionNoun(fact.collection)}`}{/if}
{:else if fact.kind === 'moveSense'}
  {$t`Moved from another entry`}
{:else if fact.kind === 'componentLink'}
  {#if fact.action === 'add'}
    {$t`Added component`}{#if target}{@render chip(target)}{/if}
  {:else if fact.action === 'remove'}{$t`Removed component`}
  {:else}
    {#if target}{$t`Changed component to`}{@render chip(target)}{:else}{$t`Changed component`}{/if}
  {/if}
{:else if fact.kind === 'setDefaultTranslation'}
  {$t`Set default translation`}
{:else if fact.kind === 'createObject'}
  {@const name = subject ?? fact.label}
  {$t`Created ${objectNoun(fact.object)}`}{#if name}{@render chip(name)}{/if}
{:else if fact.kind === 'editObject'}
  {$t`Edited ${objectNoun(fact.object)}`}{#if subject}{@render chip(subject)}{/if}
{:else if fact.kind === 'editObjectField'}
  {@const objectType = capitalize(objectNoun(fact.object))}
  {objectType}{#if subject} {@render subjectToken(subject)}{/if}{#if fact.cleared}{$t`Cleared ${fact.field}`}{#if fact.ws}{@render wsCode(fact.ws)}{/if}{:else if fact.value !== undefined}{$t`Set ${fact.field} to`}{@render chip(fact.value)}{#if fact.ws}{@render wsCode(fact.ws)}{/if}{:else}{$t`Changed ${fact.field}`}{/if}
{:else if fact.kind === 'deleteObject'}
  {$t`Deleted ${objectNoun(fact.object)}`}{#if subject}{@render chip(subject)}{/if}
{:else if fact.kind === 'sensePicture'}
  {#if fact.action === 'add'}{$t`Added picture`}{:else if fact.action === 'remove'}{$t`Removed picture`}{:else if fact.action === 'update'}{$t`Updated picture`}{:else}{$t`Reordered pictures`}{/if}
{:else if fact.kind === 'setMainPublication'}
  {$t`Set as the main publication`}
{:else if fact.kind === 'bulkCreate'}
  {$t`Created ${fact.count} ${bulkNoun(fact.noun)}`}
{:else}
  {fact.text}
{/if}
