<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {getEntityConfig} from '$lib/views/entity-config';
  import {pt, tvt, type ViewText} from '$lib/views/view-text';
  import {useViewService} from '$lib/views/view-service.svelte';
  import type {BulkNoun, ChangeFact, CollectionKind, ObjectKind, SummaryEntity} from './change-summary';

  let {fact, subject, target}: {fact: ChangeFact; subject?: string; target?: string} = $props();

  const viewService = useViewService();

  // These kinds weave the subject into their own sentence ("Created entry X"); the rest get it as a leading chip.
  const selfNaming = $derived(
    fact.kind === 'create' || fact.kind === 'createObject' || fact.kind === 'editObject'
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

  function collectionNoun(collection: CollectionKind): string {
    const nouns: Record<CollectionKind, string> = {
      senses: $t`senses`,
      examples: $t`examples`,
      components: $t`components`,
      writingSystems: $t`writing systems`,
    };
    return nouns[collection];
  }

  // Plural noun for a bulk-create collapse ("Created 100 semantic domains").
  function bulkNoun(noun: BulkNoun): string {
    const nouns: Record<BulkNoun, string> = {
      entries: $t`entries`,
      senses: $t`senses`,
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
      senses: $t`sense`,
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

{#snippet chip(text: string)}<span class="font-medium text-foreground">{text}</span>{/snippet}

{#if subject && !selfNaming}{@render chip(subject)}<span class="px-1.5 text-muted-foreground/70">·</span>{/if}{#if fact.kind === 'setField'}
  {@const label = fieldLabel(fact.entity, fact.fieldId)}
  {#if fact.ws}{$t`Set ${label} (${fact.ws}) to “${fact.value}”`}{:else}{$t`Set ${label} to “${fact.value}”`}{/if}
{:else if fact.kind === 'clearField'}
  {@const label = fieldLabel(fact.entity, fact.fieldId)}
  {#if fact.ws}{$t`Cleared ${label} (${fact.ws})`}{:else}{$t`Cleared ${label}`}{/if}
{:else if fact.kind === 'changeField'}
  {@const label = fieldLabel(fact.entity, fact.fieldId)}
  {#if target}{$t`Changed ${label} to`} {@render chip(target)}{:else}{$t`Changed ${label}`}{/if}
{:else if fact.kind === 'addItem'}
  {@const noun = itemNoun(fact.fieldId)}
  {#if noun}
    {#if fact.label}{$t`Added ${noun}`} {@render chip(fact.label)}{:else}{$t`Added a ${noun}`}{/if}
  {:else}
    {@const label = fieldLabel(fact.entity, fact.fieldId)}
    {#if fact.label}{$t`Added to ${label}: ${fact.label}`}{:else}{$t`Added to ${label}`}{/if}
  {/if}
{:else if fact.kind === 'removeItem'}
  {@const noun = itemNoun(fact.fieldId)}
  {#if noun}
    {#if target}{$t`Removed ${noun}`} {@render chip(target)}{:else}{$t`Removed a ${noun}`}{/if}
  {:else}
    {$t`Removed from ${fieldLabel(fact.entity, fact.fieldId)}`}
  {/if}
{:else if fact.kind === 'replaceItem'}
  {@const label = itemNoun(fact.fieldId) ?? fieldLabel(fact.entity, fact.fieldId)}
  {#if fact.label}{$t`Changed ${label} to`} {@render chip(fact.label)}{:else}{$t`Changed ${label}`}{/if}
{:else if fact.kind === 'create'}
  {@const name = subject ?? fact.label}
  {#if fact.entity === 'entry'}
    {#if name}{$t`Created entry`} {@render chip(name)}{:else}{$t`Created an entry`}{/if}
  {:else if fact.entity === 'sense'}
    {#if name}{$t`Added sense`} {@render chip(name)}{:else}{$t`Added a sense`}{/if}
  {:else}
    {#if subject}{$t`Added an example to`} {@render chip(subject)}{:else}{$t`Added an example`}{/if}
  {/if}
{:else if fact.kind === 'delete'}
  {#if subject}
    {#if fact.entity === 'entry'}{$t`Deleted entry`} {@render chip(subject)}
    {:else if fact.entity === 'sense'}{$t`Deleted sense`} {@render chip(subject)}
    {:else}{$t`Deleted an example from`} {@render chip(subject)}{/if}
  {:else}{$t`Deleted ${entityName(fact.entity)}`}{/if}
{:else if fact.kind === 'reorder'}
  {#if target}{$t`Reordered ${collectionItemNoun(fact.collection)}`} {@render chip(target)}{:else}{$t`Reordered ${collectionNoun(fact.collection)}`}{/if}
{:else if fact.kind === 'moveSense'}
  {$t`Moved from another entry`}
{:else if fact.kind === 'componentLink'}
  {#if fact.action === 'add'}
    {#if target}{$t`Linked component`} {@render chip(target)}{:else}{$t`Linked a component`}{/if}
  {:else if fact.action === 'remove'}{$t`Unlinked a component`}
  {:else}
    {#if target}{$t`Updated component`} {@render chip(target)}{:else}{$t`Updated a component link`}{/if}
  {/if}
{:else if fact.kind === 'setDefaultTranslation'}
  {$t`Set the default translation`}
{:else if fact.kind === 'createObject'}
  {@const name = subject ?? fact.label}
  {#if name}{$t`Created ${objectNoun(fact.object)}`} {@render chip(name)}{:else}{$t`Created a ${objectNoun(fact.object)}`}{/if}
{:else if fact.kind === 'editObject'}
  {$t`Edited ${objectNoun(fact.object)}`} {#if subject}{@render chip(subject)}{/if}
{:else if fact.kind === 'editObjectField'}
  {@const objectType = capitalize(objectNoun(fact.object))}
  {#if subject}{objectType} {@render chip(subject)}{:else}{objectType}{/if}<span class="px-1.5 text-muted-foreground/70">·</span>{#if fact.cleared}{$t`Cleared ${fact.field}`}{:else if fact.value !== undefined}{#if fact.ws}{$t`Set ${fact.field} (${fact.ws}) to “${fact.value}”`}{:else}{$t`Set ${fact.field} to “${fact.value}”`}{/if}{:else}{$t`Changed ${fact.field}`}{/if}
{:else if fact.kind === 'deleteObject'}
  {$t`Deleted ${objectNoun(fact.object)}`} {#if subject}{@render chip(subject)}{/if}
{:else if fact.kind === 'bulkCreate'}
  {$t`Created ${fact.count} ${bulkNoun(fact.noun)}`}
{:else}
  {fact.text}
{/if}
