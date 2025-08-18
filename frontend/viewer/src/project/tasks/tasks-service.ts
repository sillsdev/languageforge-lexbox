import {asString, useWritingSystemService, type WritingSystemService} from '$lib/writing-system-service.svelte';
import {useProjectContext} from '$lib/project-context.svelte';
import type {FieldId} from '$lib/entry-editor/field-data';
import {gt} from 'svelte-i18n-lingui';
import {type IEntry, type IExampleSentence, type IRichString, type ISense, WritingSystemType} from '$lib/dotnet-types';
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
  subjectFields: FieldId[];
  subjectWritingSystemId: string;
  subjectWritingSystemType: WritingSystemType;
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
        id: `sense-${analysis.wsId}`,
        contextFields: ['gloss', 'definition', 'lexemeForm', 'citationForm'],
        subject: gt`Missing Sense Gloss ${analysis.abbreviation}`,
        subjectType: 'sense',
        subjectFields: ['gloss'],
        subjectWritingSystemId: analysis.wsId,
        subjectWritingSystemType: analysis.type,
        taskKind: 'provide-missing',
        prompt: gt`Type a Gloss`,
        gridifyFilter: `Senses=null|Senses.Gloss[${analysis.wsId}]=`,
        getSubjectValue: s => TasksService.getSubjectValue(taskSense, s)
      }
      yield taskSense;
    }
  }

  public *exampleSentenceTasks() {
    for (const vernacular of this.writingSystemService.vernacular) {
      const taskExample: Task = {
        id: `example-sentence-${vernacular.wsId}`,
        contextFields: ['gloss', 'definition'],
        subject: gt`Missing Example sentence ${vernacular.abbreviation}`,
        subjectType: 'example-sentence',
        subjectFields: ['sentence'],
        subjectWritingSystemId: vernacular.wsId,
        subjectWritingSystemType: vernacular.type,
        prompt: gt`Type an example sentence`,
        taskKind: 'provide-missing',
        gridifyFilter: `Senses.ExampleSentences=null|Senses.ExampleSentences.Sentence[${vernacular.wsId}]=`,
        getSubjectValue: s => TasksService.getSubjectValue(taskExample, s)
      };
      yield taskExample;
    }
  }

  private static getSubjectValue(task: Task, subject: IEntry | ISense | IExampleSentence): string | undefined {
    const field = task.subjectFields[0];
    // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access,@typescript-eslint/no-explicit-any
    return asString((subject as any)[field][task.subjectWritingSystemId] as string | undefined | IRichString);
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
}
