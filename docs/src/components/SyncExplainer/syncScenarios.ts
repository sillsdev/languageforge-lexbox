/**
 * All the words and behaviour flags for the sync explainer. This is the only
 * file doc writers edit — adding, rewording or reordering a question never
 * touches index.tsx.
 *
 * Three caps are load-bearing, not taste:
 *  - at most ~8 questions: past that the chip row stops being scannable and
 *    people read none of them.
 *  - at most 6 steps per scenario: people stop pressing Next.
 *  - one sentence per step: the caption is read while looking at the diagram.
 *
 * A step is one sentence plus a few flags saying what the diagram shows while
 * that sentence is on screen. `text` is required; everything else is optional:
 *
 *   badge    who makes this happen (see BADGE_LABELS)
 *   legs     legs to light up
 *   hlNodes  nodes (or the cloud) to outline
 *   dim      nodes/legs to fade out
 *   check    nodes that get a green checkmark
 *   t1 / t2  where the "your edit" / "their edit" token sits; omit to hide it.
 *            Both on the same target is fine — they offset sideways.
 *   ghost    show the "teammates on FieldWorks Lite" pill
 *   offline  draw leg 1 as broken and relabel its trigger
 *
 * Only INITIAL.text supports `**bold**`; step sentences are plain text.
 */

export type CheckableNodeId = 'device' | 'liteCopy' | 'classicCopy' | 'colleague';
export type NodeId = CheckableNodeId | 'cloud';
export type LegId = 'leg1' | 'leg2' | 'leg3';
export type TokenTarget = CheckableNodeId | LegId;
export type BadgeKind = 'auto' | 'you' | 'them' | 'done';

export interface Step {
  text: string;
  badge?: BadgeKind;
  legs?: LegId[];
  hlNodes?: NodeId[];
  dim?: (NodeId | LegId)[];
  check?: CheckableNodeId[];
  t1?: TokenTarget;
  t2?: TokenTarget;
  ghost?: boolean;
  offline?: boolean;
}

export interface Scenario {
  id: string;
  q: string;
  steps: Step[];
}

export const SCENARIOS: Scenario[] = [
  {
    id: 'not-seen',
    q: 'I synced — why doesn’t my colleague see my changes?',
    steps: [
      {
        t1: 'device',
        badge: 'you',
        text: 'You edit an entry. It’s saved on your device right away — even with no internet.',
      },
      {
        t1: 'liteCopy',
        legs: ['leg1'],
        badge: 'auto',
        text: 'Moments later (once you’re online), your edit travels to the FieldWorks Lite copy in Lexbox — automatically.',
      },
      {
        t1: 'classicCopy',
        legs: ['leg2'],
        badge: 'you',
        text: 'The Sync button copies it across into the FieldWorks Classic copy. Someone has to press it — it never runs by itself.',
      },
      {
        t1: 'classicCopy',
        hlNodes: ['classicCopy'],
        text: 'Your edit is now waiting in Lexbox, in the FieldWorks Classic copy. It cannot go further on its own.',
      },
      {
        t1: 'colleague',
        legs: ['leg3'],
        badge: 'them',
        text: 'It reaches your colleague only when they run Send/Receive in FieldWorks Classic. Until then, they won’t see it.',
      },
      {
        t1: 'colleague',
        check: ['colleague'],
        badge: 'done',
        text: 'So: leg 2 needs someone to press Sync, and leg 3 needs your colleague. Neither happens by itself.',
      },
    ],
  },
  {
    id: 'internet',
    q: 'Do I need internet to work?',
    steps: [
      {
        t1: 'device',
        offline: true,
        dim: ['cloud', 'colleague', 'leg2', 'leg3'],
        badge: 'you',
        text: 'No. FieldWorks Lite works fully offline — every edit is saved on your device.',
      },
      {
        t1: 'device',
        offline: true,
        dim: ['cloud', 'colleague', 'leg2', 'leg3'],
        text: 'While you’re offline, your changes simply wait on your device. Nothing is lost, and you can keep working.',
      },
      {
        t1: 'liteCopy',
        legs: ['leg1'],
        badge: 'auto',
        text: 'When internet returns, FieldWorks Lite notices by itself and sends everything to Lexbox.',
      },
      {
        t1: 'liteCopy',
        check: ['liteCopy'],
        badge: 'done',
        text: 'You never have to remember what you changed offline — catching up is automatic.',
      },
    ],
  },
  {
    id: 'sync-button',
    q: 'What does the Sync button actually do?',
    steps: [
      {
        hlNodes: ['cloud'],
        badge: 'you',
        text: 'The Sync button works entirely inside Lexbox: it merges the project’s two copies — FieldWorks Lite and FieldWorks Classic.',
      },
      {
        hlNodes: ['cloud'],
        badge: 'you',
        text: 'It lives in two places — the Lexbox tab under Synchronize in FieldWorks Lite, and “Sync FieldWorks Lite” on your project’s page on the Lexbox website — and both do exactly the same thing.',
      },
      {
        t1: 'leg2',
        legs: ['leg2'],
        text: 'Changes flow both ways at once: your Lite edits go into the Classic copy, and any Classic changes come back into the Lite copy.',
      },
      {
        t1: 'device',
        legs: ['leg1'],
        badge: 'auto',
        text: 'Whatever came back for you then reaches your device automatically.',
      },
      {
        legs: ['leg3'],
        badge: 'done',
        text: 'What it does not do: touch your colleague’s computer. Their FieldWorks Classic updates only when they run Send/Receive.',
      },
    ],
  },
  {
    id: 'why-sr',
    q: 'Why is Send/Receive still needed in FieldWorks Classic?',
    steps: [
      {
        t2: 'colleague',
        badge: 'them',
        text: 'Your colleague edits in FieldWorks Classic. Their work is saved only on their computer.',
      },
      {
        t2: 'classicCopy',
        legs: ['leg3'],
        badge: 'them',
        text: 'Send/Receive is FieldWorks Classic’s only door to the outside: it uploads their work to Lexbox and downloads whatever is waiting for them.',
      },
      {
        t2: 'liteCopy',
        legs: ['leg2'],
        badge: 'you',
        text: 'The Sync button then merges their changes into the FieldWorks Lite copy…',
      },
      {
        t2: 'device',
        legs: ['leg1'],
        badge: 'auto',
        text: '…and from there, their changes reach your device automatically.',
      },
      {
        t2: 'device',
        check: ['device'],
        badge: 'done',
        text: 'FieldWorks Classic never talks to a server on its own — that’s by design. Send/Receive is how it joins in.',
      },
    ],
  },
  {
    id: 'both-edit',
    q: 'What if we both edit the same entry?',
    steps: [
      {
        t1: 'device',
        t2: 'colleague',
        text: 'You edit an entry in FieldWorks Lite while your colleague edits the same entry in FieldWorks Classic.',
      },
      {
        t1: 'liteCopy',
        t2: 'classicCopy',
        legs: ['leg1', 'leg3'],
        text: 'Both edits travel to Lexbox — yours automatically, theirs with Send/Receive.',
      },
      {
        t1: 'leg2',
        t2: 'leg2',
        legs: ['leg2'],
        badge: 'you',
        text: 'When the two copies are synced, the edits are merged. There is no error message and nothing to untangle by hand.',
      },
      {
        t1: 'leg2',
        t2: 'leg2',
        hlNodes: ['cloud'],
        text: 'If you changed different parts — you fixed the definition, they added an example — both changes are kept.',
      },
      {
        t1: 'leg2',
        t2: 'leg2',
        hlNodes: ['cloud'],
        badge: 'done',
        text: 'If you both changed the very same field, one version wins automatically (between Lite and Classic, the Classic value is kept). Syncing often keeps such overlaps rare and small.',
      },
    ],
  },
  {
    id: 'made-it',
    q: 'How do I know my changes made it?',
    steps: [
      {
        badge: 'you',
        text: 'Open Synchronize in FieldWorks Lite — the sidebar item with the little status dot.',
      },
      {
        legs: ['leg1'],
        t1: 'leg1',
        text: 'Its FieldWorks Lite tab reports leg 1: “Up to date” means everything on your device has reached Lexbox.',
      },
      {
        legs: ['leg2'],
        t1: 'leg2',
        text: 'The Lexbox tab reports leg 2: “No new data” means the two copies in Lexbox match.',
      },
      {
        legs: ['leg3'],
        badge: 'done',
        text: 'Leg 3 has no indicator on your side — whether your colleague has picked up the changes shows only on their computer, after their Send/Receive.',
      },
    ],
  },
  {
    id: 'fast-slow',
    q: 'Why do Lite teammates see my edit in seconds, but Classic colleagues don’t?',
    steps: [
      {
        t1: 'liteCopy',
        legs: ['leg1'],
        badge: 'auto',
        text: 'Your edit reaches Lexbox automatically, within moments.',
      },
      {
        t1: 'liteCopy',
        ghost: true,
        badge: 'auto',
        text: 'Teammates using FieldWorks Lite receive it from Lexbox the same way — automatically, usually seconds later.',
      },
      {
        t1: 'classicCopy',
        legs: ['leg2'],
        ghost: true,
        badge: 'you',
        text: 'The road to FieldWorks Classic, though, has two manual gates. Gate one: someone presses Sync.',
      },
      {
        t1: 'colleague',
        legs: ['leg3'],
        ghost: true,
        badge: 'them',
        text: 'Gate two: your colleague runs Send/Receive.',
      },
      {
        t1: 'colleague',
        ghost: true,
        check: ['colleague'],
        badge: 'done',
        text: 'Fast lane for Lite, two gates for Classic — that’s the whole difference.',
      },
    ],
  },
  {
    id: 'everything',
    q: 'Does everything in the project sync to FieldWorks Lite?',
    steps: [
      {
        hlNodes: ['liteCopy'],
        text: 'No — FieldWorks Lite carries the dictionary part of your project: entries, senses, example sentences, and so on.',
      },
      {
        hlNodes: ['classicCopy'],
        text: 'Everything else — interlinear texts, grammar, notebook data — lives only in the FieldWorks Classic copy.',
      },
      {
        legs: ['leg3'],
        badge: 'them',
        text: 'That data still travels safely between FieldWorks Classic users through Send/Receive — it just doesn’t appear in FieldWorks Lite.',
      },
      {
        badge: 'done',
        text: 'So Lite users see and edit the dictionary, and the rest of the project is left untouched.',
      },
    ],
  },
];

/** Shown before anyone picks a question, and again after "Ask another question". */
export const INITIAL = {
  title: 'An edit travels in three legs.',
  text:
    'Leg **1**: your device ⇄ Lexbox — automatic whenever you’re online. ' +
    'Leg **2**: between the two copies inside Lexbox — someone presses the **Sync** button. ' +
    'Leg **3**: Lexbox ⇄ your colleague — they run **Send/Receive** in FieldWorks Classic. ' +
    'Pick a question above to watch it happen.',
};

export const BADGE_LABELS: Record<BadgeKind, string> = {
  auto: 'happens by itself',
  you: 'someone must do this',
  them: 'your colleague does this',
  done: 'the point',
};

export const NODE_LABELS: Record<CheckableNodeId, {kind: string; name: string}> = {
  device: {kind: 'Your device', name: 'FieldWorks Lite'},
  liteCopy: {kind: 'Copy 1', name: 'FieldWorks Lite copy'},
  classicCopy: {kind: 'Copy 2', name: 'FieldWorks Classic copy'},
  colleague: {kind: 'Your colleague’s computer', name: 'FieldWorks Classic'},
};

export const CLOUD_LABELS = {
  name: 'Lexbox',
  sub: 'the server where your project is stored',
};

export const GHOST_LABEL = '✓ Teammates on FieldWorks Lite — automatic';

/** `offline` replaces leg 1's trigger label while a step sets that flag. */
export const LEG_TRIGGERS: Record<LegId, string> & {offline: string} = {
  leg1: 'automatic',
  leg2: 'the Sync button',
  leg3: 'their Send/Receive',
  offline: 'offline',
};

export const TOKEN_TAGS = {you: 'your edit', them: 'their edit'};

export const LEGEND = [
  {kind: 'auto', text: 'happens by itself'},
  {kind: 'you', text: 'someone presses Sync'},
  {kind: 'them', text: 'your colleague does it'},
] as const;

export const QUESTIONS_HEADING = 'What do you want to know?';

export const STEPPER_LABELS = {
  back: 'Back',
  next: 'Next',
  reset: 'Ask another question',
  /** Rendered as "Step 2 of 5". */
  count: (current: number, total: number) => `Step ${current} of ${total}`,
};
