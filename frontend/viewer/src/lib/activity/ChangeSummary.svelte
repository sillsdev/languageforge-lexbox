<script lang="ts">
  import {T, t} from 'svelte-i18n-lingui';
  import {Icon} from '$lib/components/ui/icon';
  import {getEntityConfig} from '$lib/views/entity-config';
  import {pt, tvt, type ViewText} from '$lib/views/view-text';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {useWritingSystemService} from '$project/data';
  import HomographNumber from '$lib/components/dictionary/HomographNumber.svelte';
  import type {IActivityChangeInfo} from '$lib/dotnet-types/generated-types/LcmCrdt/IActivityChangeInfo';
  import type {IActivityEntryName} from '$lib/dotnet-types/generated-types/LcmCrdt/IActivityEntryName';
  import type {IActivitySenseName} from '$lib/dotnet-types/generated-types/LcmCrdt/IActivitySenseName';
  import {subjectLead, type BulkNoun, type ChangeFact, type CollectionKind, type ObjectKind, type SummaryEntity} from './change-summary';

  // `grouped`: this line renders under an entry-group header (see ActivityView), which already names the entry.
  let {fact, info, grouped = false}: {
    fact: ChangeFact;
    info?: IActivityChangeInfo;
    grouped?: boolean;
  } = $props();

  const viewService = useViewService();
  const writingSystemService = useWritingSystemService();

  // These kinds weave the subject into their own sentence ("Created entry X"); the rest get it as a leading chip.
  // Sense- and example-creates are self-naming for `delete` (the deleted entity's own label) but NOT for `create`:
  // there the subject is the *parent* entry, so we want it as a leading chip — "gwa₁ · Added sense apple".
  const selfNaming = $derived(
    (fact.kind === 'create' && fact.entity === 'entry')
    || fact.kind === 'createObject' || fact.kind === 'editObject'
    || fact.kind === 'editObjectField' || fact.kind === 'deleteObject' || fact.kind === 'delete',
  );

  const lead = $derived(subjectLead(fact, info, grouped));
  const entry = $derived(info?.entry?.headword ? info.entry : undefined);
  const sense = $derived(info?.sense);
  const target = $derived(info?.target);

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

<!-- Visual hierarchy: entity NAMES (the subject a change is about, and the name of an entity a sentence
  creates/deletes/edits) are the dominant scannable tokens — bold foreground text, no chrome — wherever they
  appear in the sentence. The verb phrase is plain foreground text. DATA values the change
  carries render as bordered `chip`s. So a line reads: **subject**  verb phrase  [boxed data].
  Under a group header the subject may instead render as a `contextToken` ("› gloss") — a muted "›" connector
  plus the bold entity name: subordinate to the header via the connector and indentation, while the name stays
  bold and scannable like every other entity name.
  Spacing contract: name tokens and `contextToken` carry a trailing margin (mid-line separation without a
  middot); name tokens add an opt-in `leadingMargin` when they follow other text mid-line. `chip` carries
  a leading margin (its whole gap), droppable via `noLeadingMargin` when a preceding translated fragment
  already supplies a literal space (the " to " in a `<T msg="Set # to #">` slot) — that keeps every data chip
  on the same one-margin rhythm. Whitespace at a {#if}/{:else}/snippet boundary is trimmed, so these margins
  — never literal spaces — carry the gaps across branch edges. -->
<!-- Sense numbers are full size and lead, like the dictionary preview: a subscript would read as a homograph number -->
{#snippet entryName(name: IActivityEntryName)}{name.headword}<HomographNumber homographNumber={name.homographNumber} />{/snippet}
{#snippet senseName(name: IActivitySenseName)}{#if name.number}{name.number}{#if name.label}&nbsp;{/if}{/if}{name.label}{/snippet}
{#snippet nameToken(name: string, homographNumber = 0, leadingMargin = false)}<span class="me-1 font-semibold text-foreground {leadingMargin ? 'ms-1' : ''}">{name}<HomographNumber {homographNumber} /></span>{/snippet}
{#snippet entryToken(name: IActivityEntryName, leadingMargin = false)}<span class="me-1 font-semibold text-foreground {leadingMargin ? 'ms-1' : ''}">{@render entryName(name)}</span>{/snippet}
<!-- "headword › sense", degrading to whichever part exists; the connector stays muted, like in the preview's crumbs -->
{#snippet qualifiedToken(child: IActivitySenseName | undefined)}<span class="me-1 ms-1 font-semibold text-foreground">{#if entry}{@render entryName(entry)}{/if}{#if child?.label || child?.number}{#if entry}<span class="mx-1 font-normal text-foreground/60">›</span>{/if}{@render senseName(child)}{/if}</span>{/snippet}
{#snippet contextToken(name: IActivitySenseName)}<span class="me-1 font-semibold text-foreground"><span class="me-1 font-normal text-foreground/60">›</span>{#if !name.label && !name.number}<span class="font-normal italic text-foreground/60">{$t`Untitled`}</span>{:else}{@render senseName(name)}{/if}</span>{/snippet}
{#snippet chip(text: string, noLeadingMargin = false)}<span class="rounded border border-border bg-background px-1 font-medium text-foreground {noLeadingMargin ? '' : 'ms-1'}">{text}</span>{/snippet}
<!-- Names the change refers to (a part of speech, a linked component), which can carry a homograph number; user values stay plain text -->
{#snippet labelChip(text: string, homographNumber = 0)}<span class="rounded border border-border bg-background px-1 font-medium text-foreground ms-1">{text}<HomographNumber {homographNumber} /></span>{/snippet}
<!-- foreground/60, not text-muted-foreground, which misses WCAG AA on (selected) list rows. Muted and in parentheses so the abbreviation reads as part of the field label, not as data (chips) or a homograph number (subscript) -->
{#snippet wsAbbr(wsId: string)}{@const ws = writingSystemService.getWritingSystem(wsId) ?? {wsId, name: wsId, abbreviation: wsId}}<span class="ms-1 whitespace-nowrap text-foreground/60" title={ws.name === ws.wsId ? ws.wsId : `${ws.name} (${ws.wsId})`}>(<bdi>{ws.abbreviation}</bdi>)</span>{/snippet}
{#snippet noHeadword()}<span class="ms-1 italic text-foreground/60">{$t`(no headword)`}</span>{/snippet}
<!-- Audio marker: a media value has no readable text, so show an icon + "audio" chip instead of a URI.
  Plain inline span (not inline-flex) so it sits on the text baseline like the other data chips; the icon
  is aligned within it. An inline-flex chip aligns by its bottom edge and rides low against the text. -->
{#snippet audioNote()}<span class="ms-1 rounded border border-border bg-background px-1 font-medium text-foreground whitespace-nowrap"><Icon icon="i-mdi-volume-high" class="inline-block align-middle size-3.5 me-0.5" />{$t`audio`}</span>{/snippet}

{#if lead && !selfNaming}{#if lead.entry}{@render entryToken(lead.entry)}{/if}{#if lead.sense}{@render contextToken(lead.sense)}{/if}{#if lead.bareSense}<span class="me-1 font-semibold text-foreground">{@render senseName(lead.bareSense)}</span>{/if}{/if}{#if fact.kind === 'setField'}
  {@const label = fieldLabel(fact.entity, fact.fieldId)}
  {#if fact.audio}{$t`Set ${label}`}{@render audioNote()}{:else if fact.ws}<T msg="Set # to #" cmt="First # is a field label plus writing-system abbreviation, second # is the new value">{label}{@render wsAbbr(fact.ws)}{#snippet second()}{@render chip(fact.value, true)}{/snippet}</T>{:else}{$t`Set ${label} to`}{@render chip(fact.value)}{/if}
{:else if fact.kind === 'setHomograph'}
  {$t`Set homograph number to`}{@render chip(fact.value)}
{:else if fact.kind === 'clearField'}
  {@const label = fieldLabel(fact.entity, fact.fieldId)}
  {$t`Cleared ${label}`}{#if fact.audio}{@render audioNote()}{:else if fact.ws}{@render wsAbbr(fact.ws)}{/if}
{:else if fact.kind === 'changeField'}
  {@const label = fieldLabel(fact.entity, fact.fieldId)}
  {#if target}{$t`Changed ${label} to`}{@render labelChip(target.text, target.homographNumber)}{:else}{$t`Changed ${label}`}{/if}
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
    {$t`Removed ${noun}`}{#if target}{@render labelChip(target.text, target.homographNumber)}{/if}
  {:else}
    {$t`Removed from ${fieldLabel(fact.entity, fact.fieldId)}`}
  {/if}
{:else if fact.kind === 'replaceItem'}
  {@const label = itemNoun(fact.fieldId) ?? fieldLabel(fact.entity, fact.fieldId)}
  {#if fact.label}{$t`Changed ${label} to`}{@render chip(fact.label)}{:else}{$t`Changed ${label}`}{/if}
{:else if fact.kind === 'create'}
  {#if fact.entity === 'entry'}
    {$t`Created ${entityNoun('entry')}`}{#if entry}{@render entryToken(entry, true)}{:else if fact.label}{@render nameToken(fact.label, 0, true)}{:else}{@render noHeadword()}{/if}
  {:else if fact.entity === 'sense'}
    <!-- "headword Added sense gloss": the subject is the parent entry, `target` the new sense's label -->
    {@const name = target?.text ?? fact.label}
    {$t`Added ${entityNoun('sense')}`}{#if name}{@render nameToken(name, 0, true)}{/if}
  {:else}
    <!-- "headword › gloss · Added example": the parent sense leads.
         An audio-only example has no readable sentence text, so show the audio marker instead of a media URI. -->
    {@const name = fact.label}
    {$t`Added ${entityNoun('example')}`}{#if name}{@render nameToken(name, 0, true)}{:else if fact.audioOnly}{@render audioNote()}{/if}
  {/if}
{:else if fact.kind === 'delete'}
  <!-- A deleted sense is named in the target (it has no position left); a deleted example by its parent sense -->
  {#if fact.entity === 'entry' && entry}{$t`Deleted ${entityNoun('entry')}`}{@render entryToken(entry, true)}
  {:else if fact.entity === 'sense' && (entry || target)}{$t`Deleted ${entityNoun('sense')}`}{@render qualifiedToken(target && {label: target.text})}
  {:else if fact.entity === 'example' && (entry || sense?.label || sense?.number)}{$t`Deleted ${entityNoun('example')} from`}{@render qualifiedToken(sense)}
  {:else}{$t`Deleted ${entityNoun(fact.entity)}`}{/if}
{:else if fact.kind === 'reorder'}
  {#if target}{$t`Reordered ${collectionItemNoun(fact.collection)}`}{@render nameToken(target.text, target.homographNumber, true)}{:else}{$t`Reordered ${collectionNoun(fact.collection)}`}{/if}
{:else if fact.kind === 'moveSense'}
  {$t`Moved from another entry`}
{:else if fact.kind === 'moveExample'}
  {$t`Moved from another sense`}
{:else if fact.kind === 'componentLink'}
  {#if fact.action === 'add'}
    {$t`Added component`}{#if target}{@render labelChip(target.text, target.homographNumber)}{/if}
  {:else}{$t`Removed component`}{#if target}{@render labelChip(target.text, target.homographNumber)}{/if}
  {/if}
{:else if fact.kind === 'setDefaultTranslation'}
  {$t`Set default translation`}
{:else if fact.kind === 'createObject'}
  {@const name = info?.objectName ?? fact.label}
  {$t`Created ${objectNoun(fact.object)}`}{#if name}{@render nameToken(name, 0, true)}{/if}
{:else if fact.kind === 'editObject'}
  {@const name = info?.objectName ?? fact.label}
  {$t`Edited ${objectNoun(fact.object)}`}{#if name}{@render nameToken(name, 0, true)}{/if}
{:else if fact.kind === 'editObjectField'}
  {@const objectType = capitalize(objectNoun(fact.object))}
  {objectType}{#if info?.objectName}{@render nameToken(info.objectName, 0, true)}{/if}{#if fact.cleared}{$t`Cleared ${fact.field}`}{#if fact.ws}{@render wsAbbr(fact.ws)}{/if}{:else if fact.value !== undefined}{@const value = fact.value}{#if fact.ws}<T msg="Set # to #" cmt="First # is a field label plus writing-system abbreviation, second # is the new value">{fact.field}{@render wsAbbr(fact.ws)}{#snippet second()}{@render chip(value, true)}{/snippet}</T>{:else}{$t`Set ${fact.field} to`}{@render chip(value)}{/if}{:else}{$t`Changed ${fact.field}`}{/if}
{:else if fact.kind === 'deleteObject'}
  {$t`Deleted ${objectNoun(fact.object)}`}{#if info?.objectName}{@render nameToken(info.objectName, 0, true)}{/if}
{:else if fact.kind === 'sensePicture'}
  {#if fact.action === 'add'}{$t`Added picture`}{:else if fact.action === 'remove'}{$t`Removed picture`}{:else if fact.action === 'update'}{$t`Updated picture`}{:else}{$t`Reordered pictures`}{/if}
{:else if fact.kind === 'setMainPublication'}
  {$t`Set as the main publication`}
{:else if fact.kind === 'bulkCreate'}
  {$t`Created ${fact.count} ${bulkNoun(fact.noun)}`}
{:else if fact.kind === 'mediaResource'}
  {@const noun = fact.audio ? $t`audio recording` : $t`media file`}
  {#if fact.action === 'delete'}{$t`Deleted ${noun}`}{:else if fact.action === 'upload'}{$t`Uploaded ${noun}`}{:else}{$t`Added ${noun}`}{/if}{#if fact.audio}{@render audioNote()}{/if}
{:else if fact.kind === 'comment'}
  {#if fact.action === 'add'}{$t`Added comment`}{:else if fact.action === 'edit'}{$t`Edited comment`}{:else}{$t`Deleted comment`}{/if}{#if fact.text}{@render chip(fact.text)}{/if}
{:else if fact.kind === 'commentThread'}
  {#if fact.action === 'close'}{$t`Closed comment thread`}{:else if fact.action === 'reopen'}{$t`Reopened comment thread`}{:else}{$t`Deleted comment thread`}{/if}
{:else}
  {fact.text}
{/if}
