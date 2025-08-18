import {asString, useWritingSystemService, type WritingSystemService} from '$lib/writing-system-service.svelte';
import {useProjectContext} from '$lib/project-context.svelte';
import type {FieldId} from '$lib/entry-editor/field-data';
import {gt} from 'svelte-i18n-lingui';
import type {IEntry, IExampleSentence, IRichString, ISense} from '$lib/dotnet-types';
import {isEntry, isSense} from '$lib/utils';

const symbol = Symbol.for('fw-lite-tasks');
export function useTasksService() {
  const projectContext = useProjectContext();
  const writingSystemService = useWritingSystemService();
  return projectContext.getOrAdd(symbol, () => new TasksService(writingSystemService));
}

export interface TaskSubject {
  entryId: string;
  subject: string;
}

export interface Task {
  id: string;
  contextFields: FieldId[];
  subject?: string;
  subjectType: 'entry' | 'sense' | 'example-sentence';
  subjectFields?: FieldId[];
  subjectWritingSystemId: string;
  prompt: string;
  taskKind: 'provide-missing'
  gridifyFilter?: string;
  getSubjectValue: (subject: IEntry | ISense | IExampleSentence) => string | undefined;
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

  public *senseTasks() {
    for (const analysis of this.writingSystemService.analysis) {
      const taskSense: Task = {
        id: `sense-${analysis.abbreviation}`,
        contextFields: ['gloss', 'definition', 'lexemeForm', 'citationForm'],
        subject: gt`Missing Sense Gloss ${analysis.abbreviation}`,
        subjectType: 'sense',
        subjectFields: ['gloss'],
        subjectWritingSystemId: analysis.wsId,
        taskKind: 'provide-missing',
        prompt: gt`Type a Gloss`,
        gridifyFilter: `Senses=null,Senses.Gloss[${analysis.wsId}]=`,
        getSubjectValue: s => TasksService.getSubjectValue(taskSense, s)
      }
      yield taskSense;
    }
  }

  public *exampleSentenceTasks() {
    for (const vernacular of this.writingSystemService.vernacular) {
      const taskExample: Task = {
        id: `example-sentence-${vernacular.abbreviation}`,
        contextFields: ['gloss', 'definition'],
        subject: gt`Missing Example sentence ${vernacular.abbreviation}`,
        subjectType: 'example-sentence',
        subjectFields: ['sentence'],
        subjectWritingSystemId: vernacular.wsId,
        prompt: gt`Type an example sentence`,
        taskKind: 'provide-missing',
        gridifyFilter: `Senses.ExampleSentences=null,Senses.ExampleSentences.Sentence[${vernacular.wsId}]=`,
        getSubjectValue: s => TasksService.getSubjectValue(taskExample, s)
      };
      yield taskExample;
    }
  }

  private static getSubjectValue(task: Task, subject: IEntry | ISense | IExampleSentence): string | undefined {
    const field = task.contextFields[0];
    // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access,@typescript-eslint/no-explicit-any
    return asString((subject as any)[field][task.subjectWritingSystemId] as string | undefined | IRichString);
  }

  public static findNextSubjectIndex(task: Task, subjectParent: IEntry | ISense | IExampleSentence, startAt: number): number {
    if (isEntry(subjectParent)) {
      for (let i = startAt; i < subjectParent.senses.length; i++) {
        const sense = subjectParent.senses[i];
        if (!task.getSubjectValue(sense)) return i;
      }
      return subjectParent.senses.length;
    } else if (isSense(subjectParent)) {
      for (let i = startAt; i < subjectParent.exampleSentences.length; i++) {
        const example = subjectParent.exampleSentences[i];
        if (!task.getSubjectValue(example)) return i;
      }
      return subjectParent.exampleSentences.length;
    } else {
      throw new Error('Invalid subject type');
    }
  }
}
