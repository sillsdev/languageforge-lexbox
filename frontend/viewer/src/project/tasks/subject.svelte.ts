import type {IEntry, IExampleSentence, ISense} from '$lib/dotnet-types';

export class TaskSubject {
  entry: IEntry = $state() as IEntry;
  sense?: ISense = $state() as ISense;
  exampleSentence?: IExampleSentence = $state() as IExampleSentence;
  get subject():  string | undefined {
    return this.getSubject?.(this);
  }

  constructor(entry: IEntry, sense?: ISense, exampleSentence?: IExampleSentence, private getSubject?: (taskSubject: TaskSubject) => string | undefined) {
    this.entry = entry;
    this.sense = sense;
    this.exampleSentence = exampleSentence;
  }
}
