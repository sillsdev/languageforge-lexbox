/* eslint-disable @typescript-eslint/naming-convention */
import {
  type IEntry, type IExampleSentence, type ISense, type JsonPatch,
  type QueryOptions,
  type WritingSystems,
  type WritingSystemType,
  type WritingSystem,
  type LexboxApiClient,
  type LexboxApiFeatures,
  type PartOfSpeech,
  type SemanticDomain
} from 'viewer/lexbox-api';
import { SEMANTIC_DOMAINS_EN } from './semantic-domains.en.generated-data';
import { writable } from 'svelte/store';

function prepareEntriesForUi(entries: IEntry[]): void {
  entries.forEach(entry => {
    entry.senses.forEach(sense => {
      sense.semanticDomains.forEach(sd => {
        sd.id = sd.code;
      });
      // @ts-expect-error partOfSpeech is only included on the server for the viewer.
      sense.partOfSpeechId = sense.partOfSpeech as string;
    });
  });
}

function preparePartsOfSpeedForUi(partsOfSpeech: PartOfSpeech[]): void {
  partsOfSpeech.forEach(pos => {
    pos.id = pos.name['__key'];
  });
}

export class LfClassicLexboxApi implements LexboxApiClient {
  constructor(private projectCode: string) {
  }

  SupportedFeatures(): LexboxApiFeatures {
    return {
      feedback: true,
      about: writable(aboutMarkdown),
    };
  }

  async GetWritingSystems(): Promise<WritingSystems> {
    const result = await fetch(`/api/lfclassic/${this.projectCode}/writingSystems`);
    return (await result.json()) as WritingSystems;
  }

  async GetEntries(_options: QueryOptions | undefined): Promise<IEntry[]> {
    //todo pass query options into query
    const result = await fetch(`/api/lfclassic/${this.projectCode}/entries${this.toQueryParams(_options)}`);
    const entries = (await result.json()) as IEntry[];
    prepareEntriesForUi(entries);
    return entries;
  }

  async SearchEntries(_query: string, _options: QueryOptions | undefined): Promise<IEntry[]> {
    //todo pass query options into query
    const result = await fetch(`/api/lfclassic/${this.projectCode}/entries/${encodeURIComponent(_query)}${this.toQueryParams(_options)}`);
    const entries = (await result.json()) as IEntry[];
    prepareEntriesForUi(entries);
    return entries;
  }

  private toQueryParams(options: QueryOptions | undefined): string {

    if (!options) return '';
    /* eslint-disable @typescript-eslint/no-unsafe-assignment */
    const asc = options.order.ascending ?? true;
    const params = new URLSearchParams({
      SortField: options.order.field,
      SortWritingSystem: options.order.writingSystem,
      Ascending: asc ? 'true' : 'false',
      Count: options.count.toString(),
      Offset: options.offset.toString()
    });
    if (options.exemplar) {
      params.set('ExemplarValue', options.exemplar.value);
      params.set('ExemplarWritingSystem', options.exemplar.writingSystem);
    }
    /* eslint-enable @typescript-eslint/no-unsafe-assignment */
    return '?' + params.toString();
  }

  async GetPartsOfSpeech(): Promise<PartOfSpeech[]> {
    const result = await fetch(`/api/lfclassic/${this.projectCode}/parts-of-speech`);
    const partsOfSpeech = (await result.json()) as PartOfSpeech[];
    preparePartsOfSpeedForUi(partsOfSpeech);
    return partsOfSpeech;
  }

  GetSemanticDomains(): Promise<SemanticDomain[]> {
    return Promise.resolve(SEMANTIC_DOMAINS_EN);
  }

  CreateWritingSystem(_type: WritingSystemType, _writingSystem: WritingSystem): Promise<void> {
    throw new Error('Method not implemented.');
  }

  UpdateWritingSystem(_wsId: string, _type: WritingSystemType, _update: JsonPatch): Promise<WritingSystem> {
    throw new Error('Method not implemented.');
  }

  GetEntry(_guid: string): Promise<IEntry> {
    throw new Error('Method not implemented.');
  }

  CreateEntry(_entry: IEntry): Promise<IEntry> {
    throw new Error('Method not implemented.');
  }

  UpdateEntry(_guid: string, _update: JsonPatch): Promise<IEntry> {
    throw new Error('Method not implemented.');
  }

  CreateSense(_entryGuid: string, _sense: ISense): Promise<ISense> {
    throw new Error('Method not implemented.');
  }

  UpdateSense(_entryGuid: string, _senseGuid: string, _update: JsonPatch): Promise<ISense> {
    throw new Error('Method not implemented.');
  }

  CreateExampleSentence(_entryGuid: string, _senseGuid: string, _exampleSentence: IExampleSentence): Promise<IExampleSentence> {
    throw new Error('Method not implemented.');
  }

  UpdateExampleSentence(_entryGuid: string, _senseGuid: string, _exampleSentenceGuid: string, _update: JsonPatch): Promise<IExampleSentence> {
    throw new Error('Method not implemented.');
  }

  GetExemplars(): Promise<string[]> {
    throw new Error('Method not implemented.');
  }

  DeleteEntry(_guid: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  DeleteSense(_entryGuid: string, _senseGuid: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  DeleteExampleSentence(_entryGuid: string, _senseGuid: string, _exampleSentenceGuid: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

}

const aboutMarkdown =
`## What is this?

This is a beta version of a new dictionary building tool that is currently under development.

The data you see here reflects the current data in the corresponding [Language Forge](https://languageforge.org/) project.

This read-only version of the new dictionary tool is primarily for gathering early feedback on its look and feel. So, please use the [Feedback](/api/feedback) button in the top right corner of the page.

## It can edit FieldWorks projects!

It's true! There's already another version of the tool that you can use today to open and edit your data in FieldWorks.
It's also loaded with additional features! We're calling it [FieldWorks Lite](https://lexbox.org/fw-lite).
So, please download and try out the alpha version of [FieldWorks Lite](https://lexbox.org/fw-lite) as well.

## Should I be excited?

Yes! FieldWorks Lite will be revolutionary in multiple ways. It will be:

- Cross-platform: it will work on Windows, Linux, Mac and eventually mobile
- Usable offline: you won't need an internet connection
- Collaborative: you will see any changes other users make as they work
- Faster than you're used to - we're quite confident about that 😀

Eventually, FieldWorks Lite will replace both [WeSay](https://software.sil.org/wesay/) and [Language Forge](https://languageforge.org/).

So, please send us your [feedback](/api/feedback). We want this tool to serve you as well as possible.

## FieldWorks Lite is not

- A replacement for [FieldWorks](https://software.sil.org/fieldworks/)
- A replacement for [Dictionary App Builder](https://software.sil.org/dictionaryappbuilder/)
- A replacement for [Webonary](https://www.webonary.org/)`;
