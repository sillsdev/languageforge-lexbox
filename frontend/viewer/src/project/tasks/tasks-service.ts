import {asString, useWritingSystemService, type WritingSystemService} from '$project/data';
import {useProjectContext} from '$project/project-context.svelte';
import type {FieldId} from '$lib/entry-editor/field-data';
import {gt} from 'svelte-i18n-lingui';
import type {IEntry, IExampleSentence, IRichString, ISense, IWritingSystem, WritingSystemType} from '$lib/dotnet-types';
import {defaultExampleSentence, defaultSense, firstTruthy, isEntry, isSense} from '$lib/utils';
import {TaskSubject} from './subject.svelte';
import {subscribeLanguageChange} from '$lib/i18n';

const symbol = Symbol.for('fw-lite-tasks');

export function useTasksService() {
  const projectContext = useProjectContext();
  const writingSystemService = useWritingSystemService();
  return projectContext.getOrAdd(symbol, () => new TasksService(writingSystemService));
}

export interface Task {
  id: string;
  contextFields: FieldId[];
  subject: string;
  subjectType: 'entry' | 'sense' | 'example-sentence';
  subjectFields: FieldId[];
  subjectWritingSystemId?: string;
  subjectWritingSystemType?: WritingSystemType;
  prompt: string;
  taskKind: 'provide-missing';
  gridifyFilter?: string;
  getSubjectValue: (subject: IEntry | ISense | IExampleSentence) => string | undefined;
  isComplete: (subject: IEntry | ISense | IExampleSentence) => boolean;
}

export class TasksService {

  constructor(private writingSystemService: WritingSystemService) {
  }

  public listTasks(): Task[] {
    return [
      ...this.senseTasks(),
      ...this.exampleSentenceTasks()
    ];
  }

  public senseTasks() {
    subscribeLanguageChange();
    return TasksService.makeSenseTasks(this.writingSystemService.analysis);
  }

  public static *makeSenseTasks(analysis: IWritingSystem[]) {
    const taskMissingPartOfSpeech: Task = {
      id: 'missing-part-of-speech',
      contextFields: ['gloss', 'definition', 'lexemeForm', 'citationForm'],
      subject: gt`Missing Part of Speech`,
      subjectType: 'sense',
      subjectFields: ['partOfSpeechId'],
      prompt: gt`Pick a Part of Speech`,
      taskKind: 'provide-missing',
      gridifyFilter: `Senses.PartOfSpeechId=`,
      getSubjectValue: s => firstTruthy(analysis, ws => asString((s as ISense).partOfSpeech?.name[ws.wsId])) ,
      isComplete: s => !!(s as ISense).partOfSpeechId
    };
    yield taskMissingPartOfSpeech;
    for (const writingSystem of analysis) {
      const taskSenseGloss: Task = {
        id: `sense-no-gloss-${writingSystem.wsId}`,
        contextFields: ['gloss', 'definition', 'lexemeForm', 'citationForm'],
        subject: gt`Missing Gloss ${writingSystem.abbreviation}`,
        subjectType: 'sense',
        subjectFields: ['gloss'],
        subjectWritingSystemId: writingSystem.wsId,
        subjectWritingSystemType: writingSystem.type,
        taskKind: 'provide-missing',
        prompt: gt`Type a Gloss`,
        gridifyFilter: `Senses=null|Senses.Gloss[${writingSystem.wsId}]=`,
        getSubjectValue: s => TasksService.getSubjectValue(taskSenseGloss, s),
        isComplete: s => !!TasksService.getSubjectValue(taskSenseGloss, s)
      };
      yield taskSenseGloss;
      const taskSenseDefinition: Task = {
        id: `sense-no-definition-${writingSystem.wsId}`,
        contextFields: ['gloss', 'definition', 'lexemeForm', 'citationForm'],
        subject: gt`Missing Definition ${writingSystem.abbreviation}`,
        subjectType: 'sense',
        subjectFields: ['definition'],
        subjectWritingSystemId: writingSystem.wsId,
        subjectWritingSystemType: writingSystem.type,
        taskKind: 'provide-missing',
        prompt: gt`Type a Definition`,
        gridifyFilter: `Senses=null|Senses.Definition[${writingSystem.wsId}]=`,
        getSubjectValue: s => TasksService.getSubjectValue(taskSenseDefinition, s),
        isComplete: s => !!TasksService.getSubjectValue(taskSenseDefinition, s)
      };
      yield taskSenseDefinition;
    }
  }

  public exampleSentenceTasks() {
    subscribeLanguageChange();
    return TasksService.makeExampleSentenceTasks(this.writingSystemService.vernacular);
  }

  public static *makeExampleSentenceTasks(vernacular: IWritingSystem[]) {
    for (const writingSystem of vernacular) {
      const taskExample: Task = {
        id: `example-sentence-${writingSystem.wsId}`,
        contextFields: ['gloss', 'definition'],
        subject: gt`Missing Example sentence ${writingSystem.abbreviation}`,
        subjectType: 'example-sentence',
        subjectFields: ['sentence'],
        subjectWritingSystemId: writingSystem.wsId,
        subjectWritingSystemType: writingSystem.type,
        prompt: gt`Type an example sentence`,
        taskKind: 'provide-missing',
        gridifyFilter: `Senses.ExampleSentences=null|Senses.ExampleSentences.Sentence[${writingSystem.wsId}]=`,
        getSubjectValue: s => TasksService.getSubjectValue(taskExample, s),
        isComplete: s => !!TasksService.getSubjectValue(taskExample, s)
      };
      yield taskExample;
    }
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
      for (const sense of entry.senses) {
        let examples = sense.exampleSentences;
        if (examples.length === 0) examples = [defaultExampleSentence(sense.id)];
        for (const example of examples) {
          if (task.getSubjectValue(example)) continue;
          subjects.push(new TaskSubject(entry, sense, example, (s) => task.getSubjectValue(s.exampleSentence!)));
        }
      }
    }
    return subjects;
  }
}
