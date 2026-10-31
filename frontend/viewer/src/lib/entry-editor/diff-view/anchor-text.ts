import type {IEntry, IExampleSentence, IMultiString, IRichMultiString, ISense, IWritingSystem} from '$lib/dotnet-types';
import type {View} from '$lib/views/view-data';
import type {WritingSystemService} from '$project/data';
import type {ObjectType} from './diff-fields';

/** The text that names an entity on each side of a change, for a {@link DiffAnchor}. */
export type AnchorText = {
  before?: string;
  after?: string;
  beforeHomograph?: number;
  afterHomograph?: number;
  /** Muted text after the name, the same on both sides, e.g. a writing system's code */
  detail?: string;
};

type WsService = Pick<WritingSystemService, 'viewBestHeadword' | 'first' | 'viewAnalysis' | 'viewVernacular' | 'pickBestAlternative'>
  & {readonly analysis: IWritingSystem[]; readonly vernacular: IWritingSystem[]};

export function entryAnchor(ws: WsService, view: View, before?: IEntry, after?: IEntry): AnchorText {
  return {
    before: before && ws.viewBestHeadword(before, view),
    after: after && ws.viewBestHeadword(after, view),
    beforeHomograph: before?.homographNumber,
    afterHomograph: after?.homographNumber,
  };
}

/** The gloss names a sense; one without a gloss on either side goes by its definition. */
export function senseAnchor(ws: WsService, view: View, before?: ISense, after?: ISense): AnchorText {
  function best(value: IMultiString | IRichMultiString | undefined): string | undefined {
    if (!value) return undefined;
    return ws.first(value, ws.viewAnalysis(view)) || ws.first(value, ws.analysis);
  }
  return best(before?.gloss) || best(after?.gloss)
    ? {before: best(before?.gloss), after: best(after?.gloss)}
    : {before: best(before?.definition), after: best(after?.definition)};
}

export function exampleAnchor(ws: WsService, view: View, before?: IExampleSentence, after?: IExampleSentence): AnchorText {
  function sentence(example: IExampleSentence | undefined): string | undefined {
    if (!example) return undefined;
    return ws.first(example.sentence, ws.viewVernacular(view)) || ws.first(example.sentence, ws.vernacular);
  }
  return {before: sentence(before), after: sentence(after)};
}

type Named = {name?: IMultiString | string; code?: string; wsId?: string};

/** One field per object type names it: a semantic domain goes by "code name", a writing system by its name plus its code. */
export function objectAnchor(ws: WsService, type: ObjectType, before?: object, after?: object): AnchorText {
  function label(object: object | undefined): string | undefined {
    if (!object) return undefined;
    const {name, code} = object as Named;
    const text = typeof name === 'string' ? name : name && ws.pickBestAlternative(name, 'analysis');
    if (type === 'SemanticDomain') return [code, text].filter(Boolean).join(' ') || undefined;
    return text || undefined;
  }
  const b = before as Named | undefined;
  const a = after as Named | undefined;
  return {
    before: label(before),
    after: label(after),
    detail: type === 'WritingSystem' ? (a?.wsId ?? b?.wsId) : undefined,
  };
}
