import {describe, it, expect} from 'vitest';
import {type IEntry, type IExampleSentence, type ISemanticDomain, type ISense, type IWritingSystem, WritingSystemType} from '$lib/dotnet-types';
import {defaultEntry, defaultExampleSentence, defaultSense} from '$lib/utils';
import {TasksService} from './tasks-service';

function newEntry(e: Partial<IEntry>): IEntry {
  const entry = {
    ...defaultEntry(),
    ...e
  };
  for (const sense of entry.senses ?? []) {
    sense.entryId = entry.id;
  }
  return entry;
}

function newSense(s: Partial<ISense>): ISense {
  const sense = {
    ...defaultSense(''),
    ...s
  };
  for (const exampleSentence of sense.exampleSentences) {
    exampleSentence.senseId = sense.id;
  }
  return sense;
}

function newExample(e: Partial<IExampleSentence>): IExampleSentence {
  const example = {
    ...defaultExampleSentence(''),
    ...e
  };
  return example;
}

function ws(type: WritingSystemType): IWritingSystem {
  return {
    id: 'en',
    type,
    wsId: 'en',
    isAudio: false,
    name: '',
    abbreviation: 'Eng',
    font: '',
    exemplars: []
  };
}

function semanticDomain(code: string): ISemanticDomain {
  return {code} as ISemanticDomain;
}

const vernacularWs = ws(WritingSystemType.Vernacular);
const analysisWs = ws(WritingSystemType.Analysis);

//find tasks by id rather than positional index so the fixtures survive reordering of the generators
const exampleTask = [...TasksService.makeExampleSentenceTasks([vernacularWs])].find(t => t.id === 'example-sentence-en')!;
const senseTask = [...TasksService.makeSenseTasks([analysisWs])].find(t => t.id === 'sense-no-gloss-en')!;
const semanticDomainTask = [...TasksService.makeSenseTasks([analysisWs])].find(t => t.id === 'missing-semantic-domain')!;
const headwordTask = [...TasksService.makeEntryTasks([vernacularWs])].find(t => t.id === 'entry-no-headword-en')!;
const citationFormTask = [...TasksService.makeEntryTasks([vernacularWs])].find(t => t.id === 'entry-no-citation-form-en')!;
const lexemeFormTask = [...TasksService.makeEntryTasks([vernacularWs])].find(t => t.id === 'entry-no-lexeme-form-en')!;

describe('tasks service', () => {
  describe('subjects', () => {
    describe('example', () => {

      it('should return an example', () => {
        let sense: ISense;
        let example: IExampleSentence;
        const entry = newEntry({
          senses: [sense = newSense({
            exampleSentences: [example = newExample({})]
          })]
        });
        const [subject] = TasksService.subjects(exampleTask, entry);
        expect(subject.sense).toStrictEqual(sense);
        expect(subject.exampleSentence).toStrictEqual(example);
      });
      it('should skip filled examples', () => {

        let example: IExampleSentence;
        const entry = newEntry({
          senses: [newSense({
            exampleSentences: [
              newExample({
                sentence: {
                  en: {spans: [{text: 'hello', ws: 'en'}]}
                }
              }),
              example = newExample({})
            ]
          })]
        });
        const [subject] = TasksService.subjects(exampleTask, entry);
        expect(subject.exampleSentence).toStrictEqual(example);
      });
      it('should skip filled senses with examples', () => {
        let sense: ISense;
        let example: IExampleSentence;
        const entry = newEntry({
          senses: [
            newSense({
            exampleSentences: [
              newExample({
                sentence: {
                  en: {spans: [{text: 'hello', ws: 'en'}]}
                }
              })
            ]}),
            sense = newSense({
              exampleSentences: [example = newExample({})]
            })
          ]
        });
        const [subject] = TasksService.subjects(exampleTask, entry);
        expect(subject.sense).toStrictEqual(sense);
        expect(subject.exampleSentence).toStrictEqual(example);
      });

      it('should create a new example if none exist', () => {
        let sense: ISense;
        const entry = newEntry({
          senses: [
            sense = newSense({
              exampleSentences: []
            })
          ]
        });
        const [subject] = TasksService.subjects(exampleTask, entry);
        expect(subject.sense).toStrictEqual(sense);
        expect(subject.exampleSentence).toBeTruthy();
      });

    });
    describe('sense', () => {

      it('should return a sense', () => {
        let sense: ISense;
        const entry = newEntry({
          senses: [sense = newSense({})]
        });
        const [subject] = TasksService.subjects(senseTask, entry);
        expect(subject.entry).toStrictEqual(entry);
        expect(subject.sense).toStrictEqual(sense);
      });

      it('subject should update with changes', () => {
        const entry = newEntry({
          senses: [newSense({})]
        });
        const [subject] = TasksService.subjects(senseTask, entry);
        subject.sense!.gloss['en'] = 'hello';
        expect(subject.subject).toStrictEqual('hello');
      });

      it('should skip filled senses', () => {
        let sense: ISense;
        const entry = newEntry({
          senses: [
            newSense({
              gloss: {
                en: 'hello'
              }
            }),
            sense = newSense({})
          ]
        });
        const [subject] = TasksService.subjects(senseTask, entry);
        expect(subject.sense).toStrictEqual(sense);
      });

      it('should create a new sense if none exist', () => {
        const entry = newEntry({
          senses: []
        });
        const [subject] = TasksService.subjects(senseTask, entry);
        expect(subject.entry).toStrictEqual(entry);
        expect(subject.sense).toBeTruthy();
      });
    });

    describe('semantic domain', () => {

      it('should return a sense missing semantic domains', () => {
        let sense: ISense;
        const entry = newEntry({
          senses: [sense = newSense({semanticDomains: []})]
        });
        const [subject] = TasksService.subjects(semanticDomainTask, entry);
        expect(subject.sense).toStrictEqual(sense);
      });

      it('should skip senses that already have a semantic domain', () => {
        let sense: ISense;
        const entry = newEntry({
          senses: [
            newSense({semanticDomains: [semanticDomain('1.1')]}),
            sense = newSense({semanticDomains: []})
          ]
        });
        const [subject] = TasksService.subjects(semanticDomainTask, entry);
        expect(subject.sense).toStrictEqual(sense);
      });

      it('subject should show the domain codes once filled', () => {
        const entry = newEntry({
          senses: [newSense({semanticDomains: []})]
        });
        const [subject] = TasksService.subjects(semanticDomainTask, entry);
        subject.sense!.semanticDomains = [semanticDomain('1.1'), semanticDomain('2.3')];
        expect(subject.subject).toStrictEqual('1.1, 2.3');
      });
    });

    describe('entry', () => {

      it('citation form: should return the entry as subject', () => {
        const entry = newEntry({citationForm: {}});
        const [subject] = TasksService.subjects(citationFormTask, entry);
        expect(subject.entry).toStrictEqual(entry);
        expect(subject.sense).toBeUndefined();
      });

      it('citation form: subject should update with changes', () => {
        const entry = newEntry({citationForm: {}});
        const [subject] = TasksService.subjects(citationFormTask, entry);
        subject.entry.citationForm['en'] = 'hello';
        expect(subject.subject).toStrictEqual('hello');
      });

      it('lexeme form: should return the entry as subject', () => {
        const entry = newEntry({lexemeForm: {}});
        const [subject] = TasksService.subjects(lexemeFormTask, entry);
        expect(subject.entry).toStrictEqual(entry);
        expect(subject.sense).toBeUndefined();
      });

      it('lexeme form: subject should update with changes', () => {
        const entry = newEntry({lexemeForm: {}});
        const [subject] = TasksService.subjects(lexemeFormTask, entry);
        subject.entry.lexemeForm['en'] = 'world';
        expect(subject.subject).toStrictEqual('world');
      });

      it('headword: should return the entry when both forms are empty', () => {
        const entry = newEntry({lexemeForm: {}, citationForm: {}});
        const [subject] = TasksService.subjects(headwordTask, entry);
        expect(subject.entry).toStrictEqual(entry);
      });

      it('headword: is satisfied by a lexeme form', () => {
        const entry = newEntry({lexemeForm: {en: 'lex'}, citationForm: {}});
        expect(headwordTask.isComplete(entry)).toBe(true);
      });

      it('headword: is satisfied by a citation form', () => {
        const entry = newEntry({lexemeForm: {}, citationForm: {en: 'cite'}});
        expect(headwordTask.isComplete(entry)).toBe(true);
      });

      it('headword: prefers the citation form for the subject value', () => {
        const entry = newEntry({lexemeForm: {en: 'lex'}, citationForm: {en: 'cite'}});
        const [subject] = TasksService.subjects(headwordTask, entry);
        expect(subject.subject).toStrictEqual('cite');
      });

      it('headword: is incomplete when both forms are empty', () => {
        const entry = newEntry({lexemeForm: {}, citationForm: {}});
        expect(headwordTask.isComplete(entry)).toBe(false);
      });
    });


  });
});
