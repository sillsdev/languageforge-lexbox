import {describe, it, vi, beforeEach, type Mock, expect, afterEach} from 'vitest';
import {type IEntry, type IExampleSentence, type ISense, WritingSystemType} from '$lib/dotnet-types';
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

const exampleTask = [...TasksService.makeExampleSentenceTasks([{
  id: 'en',
  type: WritingSystemType.Vernacular,
  wsId: 'en',
  isAudio: false,
  name: '',
  abbreviation: 'Eng',
  font: '',
  exemplars: []
}])][0];
const senseTask = [...TasksService.makeSenseTasks([{
  id: 'en',
  type: WritingSystemType.Analysis,
  wsId: 'en',
  isAudio: false,
  name: '',
  abbreviation: 'Eng',
  font: '',
  exemplars: []
}])][0];

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
        const subjects = TasksService.subjects(exampleTask, entry);
        expect(subjects).toStrictEqual([{entry, sense, exampleSentence: example, subject: undefined}]);
      });
      it('should skip filled examples', () => {
        let sense: ISense;
        let example: IExampleSentence;
        const entry = newEntry({
          senses: [sense = newSense({
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
        const subjects = TasksService.subjects(exampleTask, entry);
        expect(subjects).toStrictEqual([{entry, sense, exampleSentence: example, subject: undefined}]);
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
        const subjects = TasksService.subjects(exampleTask, entry);
        expect(subjects).toStrictEqual([{entry, sense, exampleSentence: example, subject: undefined}]);
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
        const subjects = TasksService.subjects(senseTask, entry);
        expect(subjects).toStrictEqual([{entry, sense, subject: undefined}]);
      });

      it('subject should update with changes', () => {
        let sense: ISense;
        const entry = newEntry({
          senses: [sense = newSense({})]
        });
        const [subject] = TasksService.subjects(senseTask, entry);
        subject.sense!.gloss['en'] = 'hello';
        expect(subject).toStrictEqual({entry, sense, subject: 'hello'});
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
        const subjects = TasksService.subjects(senseTask, entry);
        expect(subjects).toStrictEqual([{entry, sense, subject: undefined}]);
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


  });
});
