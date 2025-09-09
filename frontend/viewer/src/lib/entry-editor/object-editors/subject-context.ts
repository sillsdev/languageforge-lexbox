import {Context} from 'runed';
import type {IEntry, IExampleSentence, ISense} from '$lib/dotnet-types';

interface EditorPrimitiveSubject {
  readonly current: IEntry | ISense | IExampleSentence | undefined
}
export const subjectContext = new Context<EditorPrimitiveSubject>('subject-context');

export function initSubjectContext(subject: () => IEntry | ISense | IExampleSentence) {
  const editorPrimitiveSubject: EditorPrimitiveSubject = {
    get current() {
      return subject?.();
    }
  };
  subjectContext.set(editorPrimitiveSubject);
  return editorPrimitiveSubject;
}

export function useSubjectContext() {
  return subjectContext.getOr(undefined);
}
