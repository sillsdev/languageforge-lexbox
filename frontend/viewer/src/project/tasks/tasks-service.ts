import {asString, useWritingSystemService, type WritingSystemService} from '$project/data';
import {useProjectContext} from '$project/project-context.svelte';
import {getEntityConfig, type FieldId} from '$lib/views/entity-config';
import {vt, type ViewText} from '$lib/views/view-text';
import {gt, msg} from 'svelte-i18n-lingui';
import type {IEntry, IExampleSentence, IMultiString, IRichMultiString, IRichString, ISense, IWritingSystem, WritingSystemType} from '$lib/dotnet-types';
import {defaultExampleSentence, defaultSense, defaultTranslation, firstTruthy, isEntry, isSense} from '$lib/utils';
import {TaskSubject} from './subject.svelte';
import {subscribeLanguageChange} from '$lib/i18n';
import type {Overrides} from '$lib/views/view-data';

const symbol = Symbol.for('fw-lite-tasks');

export interface ContextField {
  fieldId: FieldId;
  entity: 'entry' | 'sense' | 'example';
  rich: boolean;
  /** The subject's entity, so the field can be bound through its reactive proxy. */
  owner: Record<string, IMultiString | IRichMultiString>;
  writingSystems: IWritingSystem[];
}

const contextTextFields: Partial<Record<FieldId, Pick<ContextField, 'entity' | 'rich'> & {type: 'vernacular' | 'analysis'}>> = {
  lexemeForm: {entity: 'entry', type: 'vernacular', rich: false},
  citationForm: {entity: 'entry', type: 'vernacular', rich: false},
  gloss: {entity: 'sense', type: 'analysis', rich: false},
  definition: {entity: 'sense', type: 'analysis', rich: true},
  sentence: {entity: 'example', type: 'vernacular', rich: true},
};

export function useTasksService() {
  const projectContext = useProjectContext();
  const writingSystemService = useWritingSystemService();
  return projectContext.getOrAdd(symbol, () => new TasksService(writingSystemService));
}

export interface Task {
  id: string;
  /** Shown read-only above the subject, if they're on the subject's entity. */
  contextFields: FieldId[];
  /** Already translated, unlike fieldLabel and description: tasks are rebuilt on language change. */
  subject: ViewText;
  /** Overrides the field's own label when the task isn't quite the field, e.g. Headword. */
  fieldLabel?: ViewText;
  description?: ViewText;
  subjectType: 'entry' | 'sense' | 'example-sentence';
  subjectFields: FieldId[];
  /** Shown and saved with the subject, but never required. */
  optionalFields?: FieldId[];
  subjectWritingSystemId?: string;
  subjectWritingSystemType?: WritingSystemType;
  /** The form's writing systems. Defaults to only the subject's. */
  overrides?: Overrides;
  /** Already translated, see subject. */
  prompt: ViewText;
  taskKind: 'provide-missing';
  gridifyFilter?: string;
  getSubjectValue: (subject: IEntry | ISense | IExampleSentence) => string | undefined;
  isComplete: (subject: IEntry | ISense | IExampleSentence) => boolean;
}

export function taskLabel(task: Task): ViewText {
  if (task.fieldLabel) return task.fieldLabel;
  const entity = task.subjectType === 'example-sentence' ? 'example' : task.subjectType;
  return (getEntityConfig(entity) as Record<string, {label: ViewText}>)[task.subjectFields[0]].label;
}

export class TasksService {

  constructor(private writingSystemService: WritingSystemService) {
  }

  public listTasks(): Task[] {
    return [
      ...this.entryTasks(),
      ...this.senseTasks(),
      ...this.exampleSentenceTasks()
    ];
  }

  /** The task for one field in one writing system, e.g. Lexeme form in French. Ignores combined tasks like Headword. */
  public findTask(field: FieldId, wsId: string): Task | undefined {
    return this.listTasks().find(task =>
      task.subjectWritingSystemId === wsId && task.subjectFields.length === 1 && task.subjectFields[0] === field);
  }

  public senseTasks() {
    subscribeLanguageChange();
    return TasksService.makeSenseTasks(this.writingSystemService.analysis);
  }

  public static *makeSenseTasks(analysis: IWritingSystem[]) {
    for (const writingSystem of analysis) {
      const taskSenseGloss: Task = {
        id: `sense-no-gloss-${writingSystem.wsId}`,
        description: vt(msg`A short translation of the sense`, msg`A short translation of the meaning`),
        contextFields: ['gloss', 'definition', 'lexemeForm', 'citationForm'],
        subject: gt`Missing Gloss ${writingSystem.abbreviation}`,
        subjectType: 'sense',
        subjectFields: ['gloss'],
        subjectWritingSystemId: writingSystem.wsId,
        subjectWritingSystemType: writingSystem.type,
        taskKind: 'provide-missing',
        prompt: writingSystem.isAudio ? gt`Record a Gloss` : gt`Type a Gloss`,
        gridifyFilter: `Senses=null|Senses.Gloss[${writingSystem.wsId}]=`,
        getSubjectValue: s => TasksService.getSubjectValue(taskSenseGloss, s),
        isComplete: s => !!TasksService.getSubjectValue(taskSenseGloss, s)
      };
      yield taskSenseGloss;
      const taskSenseDefinition: Task = {
        id: `sense-no-definition-${writingSystem.wsId}`,
        description: vt(msg`A full explanation of the sense`, msg`A full explanation of the meaning`),
        contextFields: ['gloss', 'definition', 'lexemeForm', 'citationForm'],
        subject: gt`Missing Definition ${writingSystem.abbreviation}`,
        subjectType: 'sense',
        subjectFields: ['definition'],
        subjectWritingSystemId: writingSystem.wsId,
        subjectWritingSystemType: writingSystem.type,
        taskKind: 'provide-missing',
        prompt: writingSystem.isAudio ? gt`Record a Definition` : gt`Type a Definition`,
        gridifyFilter: `Senses=null|Senses.Definition[${writingSystem.wsId}]=`,
        getSubjectValue: s => TasksService.getSubjectValue(taskSenseDefinition, s),
        isComplete: s => !!TasksService.getSubjectValue(taskSenseDefinition, s)
      };
      yield taskSenseDefinition;
    }
    const taskMissingPartOfSpeech: Task = {
      id: 'missing-part-of-speech',
      description: msg`Noun, verb and so on`,
      contextFields: ['gloss', 'definition', 'lexemeForm', 'citationForm'],
      subject: vt(gt`Missing Grammatical info.`, gt`Missing Part of speech`),
      subjectType: 'sense',
      subjectFields: ['partOfSpeechId'],
      prompt: vt(gt`Pick Grammatical info.`, gt`Pick a Part of speech`),
      taskKind: 'provide-missing',
      gridifyFilter: 'Senses.PartOfSpeechId=',
      getSubjectValue: s => firstTruthy(analysis, ws => asString((s as ISense).partOfSpeech?.name[ws.wsId])) ,
      isComplete: s => !!(s as ISense).partOfSpeechId
    };
    yield taskMissingPartOfSpeech;
    const taskMissingSemanticDomain: Task = {
      id: 'missing-semantic-domain',
      description: vt(msg`Topics the sense belongs to`, msg`Topics the meaning belongs to`),
      contextFields: ['gloss', 'definition', 'lexemeForm', 'citationForm'],
      subject: gt`Missing Semantic domains`,
      subjectType: 'sense',
      subjectFields: ['semanticDomains'],
      prompt: gt`Pick one or more Semantic domains`,
      taskKind: 'provide-missing',
      gridifyFilter: 'Senses=null|Senses.SemanticDomains=null',
      getSubjectValue: s => TasksService.getSemanticDomainsValue(s as ISense),
      isComplete: s => !!(s as ISense).semanticDomains?.length
    };
    yield taskMissingSemanticDomain;
  }

  public entryTasks() {
    subscribeLanguageChange();
    return TasksService.makeEntryTasks(this.writingSystemService.vernacular);
  }

  public static *makeEntryTasks(vernacular: IWritingSystem[]) {
    for (const writingSystem of vernacular) {
      const taskHeadword: Task = {
        id: `entry-no-headword-${writingSystem.wsId}`,
        fieldLabel: msg`Headword`,
        description: vt(msg`The entry's main form, for entries that have none yet`, msg`The word's main form, for words that have none yet`),
        contextFields: ['lexemeForm', 'citationForm', 'gloss', 'definition'],
        subject: gt`Missing Headword ${writingSystem.abbreviation}`,
        subjectType: 'entry',
        subjectFields: ['lexemeForm', 'citationForm'],
        subjectWritingSystemId: writingSystem.wsId,
        subjectWritingSystemType: writingSystem.type,
        prompt: writingSystem.isAudio
          ? vt(gt`Record a Lexeme form or Citation form`, gt`Record Word or Display as`)
          : vt(gt`Type a Lexeme form or Citation form`, gt`Fill in Word or Display as`),
        taskKind: 'provide-missing',
        gridifyFilter: `LexemeForm[${writingSystem.wsId}]=,CitationForm[${writingSystem.wsId}]=`,
        getSubjectValue: s => TasksService.getHeadwordValue(s as IEntry, writingSystem.wsId),
        isComplete: s => !!TasksService.getHeadwordValue(s as IEntry, writingSystem.wsId)
      };
      yield taskHeadword;
      const taskLexemeForm: Task = {
        id: `entry-no-lexeme-form-${writingSystem.wsId}`,
        description: vt(msg`The base form of the entry`, msg`The base form of the word`),
        contextFields: ['lexemeForm', 'citationForm', 'gloss', 'definition'],
        subject: vt(gt`Missing Lexeme form ${writingSystem.abbreviation}`, gt`Missing Word ${writingSystem.abbreviation}`),
        subjectType: 'entry',
        subjectFields: ['lexemeForm'],
        subjectWritingSystemId: writingSystem.wsId,
        subjectWritingSystemType: writingSystem.type,
        prompt: writingSystem.isAudio
          ? vt(gt`Record a Lexeme form`, gt`Record a Word`)
          : vt(gt`Type a Lexeme form`, gt`Type a Word`),
        taskKind: 'provide-missing',
        gridifyFilter: `LexemeForm[${writingSystem.wsId}]=`,
        getSubjectValue: s => TasksService.getSubjectValue(taskLexemeForm, s),
        isComplete: s => !!TasksService.getSubjectValue(taskLexemeForm, s)
      };
      yield taskLexemeForm;
      const taskCitationForm: Task = {
        id: `entry-no-citation-form-${writingSystem.wsId}`,
        description: vt(msg`How the entry is shown in the dictionary`, msg`How the word is shown in the dictionary`),
        contextFields: ['lexemeForm', 'citationForm', 'gloss', 'definition'],
        subject: vt(gt`Missing Citation form ${writingSystem.abbreviation}`, gt`Missing Display as ${writingSystem.abbreviation}`),
        subjectType: 'entry',
        subjectFields: ['citationForm'],
        subjectWritingSystemId: writingSystem.wsId,
        subjectWritingSystemType: writingSystem.type,
        prompt: writingSystem.isAudio
          ? vt(gt`Record a Citation form`, gt`Record Display as`)
          : vt(gt`Type a Citation form`, gt`Fill in Display as`),
        taskKind: 'provide-missing',
        gridifyFilter: `CitationForm[${writingSystem.wsId}]=`,
        getSubjectValue: s => TasksService.getSubjectValue(taskCitationForm, s),
        isComplete: s => !!TasksService.getSubjectValue(taskCitationForm, s)
      };
      yield taskCitationForm;
    }
  }

  public exampleSentenceTasks() {
    subscribeLanguageChange();
    return [
      ...TasksService.makeExampleSentenceTasks(this.writingSystemService.vernacular),
      ...TasksService.makeTranslationTasks(this.writingSystemService.analysis, this.writingSystemService.vernacular),
    ];
  }

  public static *makeExampleSentenceTasks(vernacular: IWritingSystem[]) {
    for (const writingSystem of vernacular) {
      const taskExample: Task = {
        id: `example-sentence-${writingSystem.wsId}`,
        description: vt(msg`A sentence that uses the entry`, msg`A sentence that uses the word`),
        fieldLabel: msg`Example sentence`,
        contextFields: ['gloss', 'definition'],
        subject: gt`Missing Example sentence ${writingSystem.abbreviation}`,
        subjectType: 'example-sentence',
        subjectFields: ['sentence'],
        optionalFields: ['translations'],
        subjectWritingSystemId: writingSystem.wsId,
        subjectWritingSystemType: writingSystem.type,
        // translations stay in the view's analysis writing systems
        overrides: {vernacular: [{wsId: writingSystem.wsId}]},
        prompt: writingSystem.isAudio
          ? gt`Record an example sentence, and type a translation if you can`
          : gt`Type an example sentence, and a translation if you can`,
        taskKind: 'provide-missing',
        gridifyFilter: `Senses.ExampleSentences=null|Senses.ExampleSentences.Sentence[${writingSystem.wsId}]=`,
        getSubjectValue: s => TasksService.getSubjectValue(taskExample, s),
        isComplete: s => !!TasksService.getSubjectValue(taskExample, s)
      };
      yield taskExample;
    }
  }

  public static *makeTranslationTasks(analysis: IWritingSystem[], vernacular: IWritingSystem[]) {
    const hasSentence = vernacular.length
      ? `(${vernacular.map(ws => `Senses.ExampleSentences.Sentence[${ws.wsId}]!=`).join('|')})`
      : '';
    for (const writingSystem of analysis) {
      const taskTranslation: Task = {
        id: `example-translation-${writingSystem.wsId}`,
        description: msg`What the example sentence means`,
        contextFields: ['sentence'],
        subject: gt`Missing Translation ${writingSystem.abbreviation}`,
        subjectType: 'example-sentence',
        subjectFields: ['translations'],
        subjectWritingSystemId: writingSystem.wsId,
        subjectWritingSystemType: writingSystem.type,
        prompt: writingSystem.isAudio ? gt`Record a translation of the example sentence` : gt`Translate the example sentence`,
        taskKind: 'provide-missing',
        // the two conditions can match different examples of one entry; rare enough to accept
        gridifyFilter: [hasSentence, `Senses.ExampleSentences.Translations.Text[${writingSystem.wsId}]=`].filter(Boolean).join(', '),
        getSubjectValue: s => TasksService.getTranslationValue(s as IExampleSentence, writingSystem.wsId),
        isComplete: s => !!TasksService.getTranslationValue(s as IExampleSentence, writingSystem.wsId)
      };
      yield taskTranslation;
    }
  }

  /** The task's context fields on the subject's entity, each in every writing system it has a value in, ignoring the view. */
  public static contextFields(task: Task, subject: TaskSubject, writingSystems: Record<'vernacular' | 'analysis', IWritingSystem[]>): ContextField[] {
    const entity = task.subjectType === 'example-sentence' ? 'example' : task.subjectType;
    const object = entity === 'entry' ? subject.entry : entity === 'sense' ? subject.sense : subject.exampleSentence;
    if (!object) return [];
    return task.contextFields.flatMap(fieldId => {
      const field = contextTextFields[fieldId];
      if (field?.entity !== entity) return [];
      const owner = object as unknown as ContextField['owner'];
      const withValue = writingSystems[field.type].filter(ws => asString(owner[fieldId][ws.wsId]));
      return withValue.length ? [{fieldId, entity, rich: field.rich, owner, writingSystems: withValue}] : [];
    });
  }

  private static getTranslationValue(example: IExampleSentence, wsId: string): string | undefined {
    return firstTruthy(example.translations, t => asString(t.text[wsId]));
  }

  private static getHeadwordValue(entry: IEntry, wsId: string): string | undefined {
    //matches the headword convention: citation form wins, else lexeme form
    return asString(entry.citationForm[wsId]) || asString(entry.lexemeForm[wsId]);
  }

  private static getSemanticDomainsValue(sense: ISense): string | undefined {
    const domains = sense.semanticDomains;
    if (!domains?.length) return undefined;
    return domains.map(d => d.code).join(', ');
  }

  private static verifyHasValue(task: Task, subject: IEntry | ISense | IExampleSentence | undefined): boolean {
    if (!subject) return false;
    const value = TasksService.getSubjectValue(task, subject);
    return !!value;
  }

  private static getSubjectValue(task: Task, subject: IEntry | ISense | IExampleSentence | undefined): string | undefined {
    if (!subject) return undefined;
    const field = task.subjectFields[0];
    // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment,@typescript-eslint/no-unsafe-member-access,@typescript-eslint/no-explicit-any
    const fieldValue = (subject as any)[field];
    if (task.subjectWritingSystemId) {
      // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access
      return asString(fieldValue[task.subjectWritingSystemId] as string | undefined | IRichString)
    }
    return asString(fieldValue as string | undefined | IRichString);
  }

  public static findNextSubjectIndex(task: Task, subjectParent: IEntry | ISense | IExampleSentence, startAt: number): number {
    if (isEntry(subjectParent)) {
      for (let i = startAt; i < subjectParent.senses.length; i++) {
        const sense = subjectParent.senses[i];
        if (task.subjectType == 'sense') {
          if (!task.getSubjectValue(sense)) return i;
        } else {
          //if there's no example sentence then one needs to be created
          if (sense.exampleSentences.length === 0) return i;
          //type must be example-sentence, so we want to return the index to the first sense with an example sentence which is pending
          const hasExampleTodo = TasksService.findNexExampleSentence(task, sense, 0) < sense.exampleSentences.length;
          if (hasExampleTodo) return i;
        }
      }
      return subjectParent.senses.length;
    } else if (isSense(subjectParent)) {
      return TasksService.findNexExampleSentence(task, subjectParent, startAt);
    } else {
      throw new Error('Invalid subject type');
    }
  }

  private static findNexExampleSentence(task: Task, parent: ISense, startAt: number) {
    if (task.subjectType !== 'example-sentence') return -1;
    for (let i = startAt; i < parent.exampleSentences.length; i++) {
      const example = parent.exampleSentences[i];
      if (!task.getSubjectValue(example)) return i;
    }
    return parent.exampleSentences.length;
  }

  public static subjects(task: Task, entry?: IEntry): TaskSubject[] {
    if (!entry) return [];
    if (task.subjectType === 'entry') {
      return [new TaskSubject(entry, undefined, undefined, s => task.getSubjectValue(s.entry))];
    }
    const subjects: TaskSubject[] = [];
    if (task.subjectType === 'sense') {
      let senses = entry.senses;
      if (senses.length === 0) senses = [defaultSense(entry.id)];
      for (const sense of senses) {
        if (task.getSubjectValue(sense)) continue;
        subjects.push(new TaskSubject(entry, sense, undefined, s => task.getSubjectValue(s.sense!)));
      }
    } else if (task.subjectType === 'example-sentence') {
      const translating = task.subjectFields.includes('translations');
      for (const sense of entry.senses) {
        let examples = sense.exampleSentences;
        // there's nothing to translate in a new example
        if (examples.length === 0 && !translating) examples = [defaultExampleSentence(sense.id)];
        for (let example of examples) {
          if (task.getSubjectValue(example)) continue;
          if (translating && !Object.values(example.sentence).some(asString)) continue;
          // a real translation rather than the editor's draft, so isComplete sees what's typed
          if (translating && !example.translations.length) example = {...example, translations: [defaultTranslation()]};
          subjects.push(new TaskSubject(entry, sense, example, (s) => task.getSubjectValue(s.exampleSentence!)));
        }
      }
    }
    return subjects;
  }
}
