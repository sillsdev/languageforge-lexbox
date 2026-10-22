/**
 * All the words and behaviour flags for the sync explainer. This is the only
 * file doc writers edit: adding, rewording or reordering a question never
 * touches index.tsx.
 *
 * Three caps are load-bearing, not taste:
 *  - at most ~8 questions: past that the chip row stops being scannable and
 *    people read none of them.
 *  - at most 6 steps per scenario: people stop pressing Next.
 *  - one or two short sentences per step: the caption is read while looking
 *    at the diagram.
 *
 * Scenario ids are URL hashes (/fw-lite/how-sync-works#not-seen) that support
 * replies link to, so never rename one.
 *
 * A step is its text plus a few flags saying what the diagram shows while
 * that text is on screen. `text` is required; everything else is optional:
 *
 *   badge    who makes this happen (see BADGE_LABELS)
 *   legs     legs to light up
 *   hlNodes  nodes (or the cloud) to outline
 *   dim      nodes/legs to fade out
 *   check    nodes that get a green checkmark
 *   t1 / t2  where the "your edit" / "their edit" token sits; omit to hide it.
 *            Both on the same target is fine: they offset sideways.
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
    q: 'I synced. Why doesn’t my colleague see my changes?',
    steps: [
      {
        t1: 'device',
        badge: 'you',
        text: 'You edit a word. It’s saved on your device right away, even with no internet.',
      },
      {
        t1: 'liteCopy',
        legs: ['leg1'],
        badge: 'auto',
        text: 'Moments later (once you’re online), your edit travels to the FieldWorks Lite copy in Lexbox by itself.',
      },
      {
        t1: 'classicCopy',
        legs: ['leg2'],
        badge: 'you',
        text: 'Pressing Sync on the Lexbox tab copies it into the FieldWorks Classic copy. Someone has to press it: it never runs by itself.',
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
        text: 'So: someone has to press Sync on the Lexbox tab, and your colleague has to run Send/Receive. Neither happens by itself.',
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
        text: 'No. FieldWorks Lite works fully offline. Every edit is saved on your device.',
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
        text: 'Once you’re back online, FieldWorks Lite sends everything to Lexbox at its next sync: after your next edit, when you open the project, or when you press Auto syncing.',
      },
      {
        t1: 'liteCopy',
        check: ['liteCopy'],
        badge: 'done',
        text: 'You never have to remember what you changed offline. To be sure it all went out, check that Synchronize says Up to date.',
      },
    ],
  },
  {
    id: 'sync-button',
    q: 'What does Sync on the Lexbox tab do?',
    steps: [
      {
        hlNodes: ['cloud'],
        badge: 'you',
        text: 'This Sync button works entirely inside Lexbox: it merges the project’s two copies, FieldWorks Lite and FieldWorks Classic. It only merges what has already reached Lexbox.',
      },
      {
        hlNodes: ['cloud'],
        badge: 'you',
        text: 'In FieldWorks Lite it’s on the Lexbox tab under Synchronize. People in the FieldWorks Lite beta also see “Sync FieldWorks Lite” on the project’s page on lexbox.org, which does the same thing.',
      },
      {
        t1: 'leg2',
        legs: ['leg2'],
        text: 'Changes flow both ways at once: your FieldWorks Lite edits go into the FieldWorks Classic copy, and any FieldWorks Classic changes come back into the FieldWorks Lite copy.',
      },
      {
        t1: 'device',
        legs: ['leg1'],
        badge: 'auto',
        text: 'Whatever came back for you then reaches your device by itself.',
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
        text: 'Send/Receive is how FieldWorks Classic talks to Lexbox: it uploads their work and downloads whatever is waiting for them.',
      },
      {
        t2: 'liteCopy',
        legs: ['leg2'],
        badge: 'you',
        text: 'Sync on the Lexbox tab then merges their changes into the FieldWorks Lite copy…',
      },
      {
        t2: 'device',
        legs: ['leg1'],
        badge: 'auto',
        text: '…and from there, their changes reach your device by themselves.',
      },
      {
        t2: 'device',
        check: ['device'],
        badge: 'done',
        text: 'FieldWorks Classic never talks to a server on its own. That’s by design: Send/Receive is how it joins in.',
      },
    ],
  },
  {
    id: 'both-edit',
    q: 'What if we both edit the same word?',
    steps: [
      {
        t1: 'device',
        t2: 'colleague',
        text: 'You edit a word in FieldWorks Lite while your colleague edits the same entry in FieldWorks Classic.',
      },
      {
        t1: 'liteCopy',
        t2: 'classicCopy',
        legs: ['leg1', 'leg3'],
        text: 'Both edits travel to Lexbox: yours by itself, theirs with Send/Receive.',
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
        text: 'If you changed different parts (you fixed the definition, they added an example), both changes are kept.',
      },
      {
        t1: 'leg2',
        t2: 'leg2',
        hlNodes: ['cloud'],
        badge: 'done',
        text: 'If you both changed the very same field, the FieldWorks Classic value wins (between two FieldWorks Lite users, the later change), and the other stays in the word’s History.',
      },
    ],
  },
  {
    id: 'made-it',
    q: 'How do I know my changes made it?',
    steps: [
      {
        badge: 'you',
        text: 'Open Synchronize in FieldWorks Lite: the sidebar item with the little status dot (on a phone, tap ☰ first).',
      },
      {
        legs: ['leg1'],
        t1: 'leg1',
        text: 'Its FieldWorks Lite tab is about your device and Lexbox: “Up to date” means everything on your device has reached Lexbox.',
      },
      {
        legs: ['leg2'],
        t1: 'leg2',
        text: 'The Lexbox tab is about the two copies in Lexbox: “No new data” on both sides means they match.',
      },
      {
        legs: ['leg3'],
        badge: 'done',
        text: 'Your colleague’s Send/Receive has no indicator on your side. Whether they have your changes shows only on their computer.',
      },
    ],
  },
  {
    id: 'fast-slow',
    q: 'Why do FieldWorks Lite teammates see my edit in seconds, but FieldWorks Classic colleagues don’t?',
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
        text: 'Teammates using FieldWorks Lite get it from Lexbox the same way, usually seconds later.',
      },
      {
        t1: 'classicCopy',
        legs: ['leg2'],
        ghost: true,
        badge: 'you',
        text: 'The road to FieldWorks Classic, though, has two manual steps. First, someone presses Sync on the Lexbox tab.',
      },
      {
        t1: 'colleague',
        legs: ['leg3'],
        ghost: true,
        badge: 'them',
        text: 'Then your colleague runs Send/Receive.',
      },
      {
        t1: 'colleague',
        ghost: true,
        check: ['colleague'],
        badge: 'done',
        text: 'Automatic for FieldWorks Lite, two manual steps for FieldWorks Classic: that’s the whole difference.',
      },
    ],
  },
  {
    id: 'everything',
    q: 'Does everything in the project sync to FieldWorks Lite?',
    steps: [
      {
        hlNodes: ['liteCopy'],
        text: 'No. FieldWorks Lite carries the dictionary part of your project: words, meanings, example sentences, and so on.',
      },
      {
        hlNodes: ['classicCopy'],
        text: 'Everything else (interlinear texts, grammar, notebook data) lives only in the FieldWorks Classic copy.',
      },
      {
        legs: ['leg3'],
        badge: 'them',
        text: 'That data still travels safely between FieldWorks Classic users through Send/Receive. It just doesn’t appear in FieldWorks Lite.',
      },
      {
        badge: 'done',
        text: 'So FieldWorks Lite users see and edit the dictionary, and the rest of the project is left untouched.',
      },
    ],
  },
];

/** Shown before anyone picks a question, and again after "Ask another question". */
export const INITIAL = {
  title: 'An edit travels in three steps.',
  text:
    '**1** Your device ⇄ Lexbox: automatic whenever you’re online. ' +
    '**2** Between the two copies inside Lexbox: someone presses **Sync** on the **Lexbox** tab. ' +
    '**3** Lexbox ⇄ your colleague: they run **Send/Receive** in FieldWorks Classic. ' +
    'Pick a question below to watch it happen.',
};

export const BADGE_LABELS: Record<BadgeKind, string> = {
  auto: 'happens by itself',
  you: 'someone must do this',
  them: 'your colleague does this',
  done: 'the point',
};

export const NODE_LABELS: Record<CheckableNodeId, {kind: string; name: string}> = {
  device: {kind: 'Your device', name: 'FieldWorks Lite'},
  liteCopy: {kind: 'Copy for', name: 'FieldWorks Lite'},
  classicCopy: {kind: 'Copy for', name: 'FieldWorks Classic'},
  colleague: {kind: 'Your colleague’s computer', name: 'FieldWorks Classic'},
};

export const CLOUD_LABELS = {
  name: 'Lexbox',
  sub: 'the server where your project is stored',
};

export const GHOST_LABEL = '✓ Teammates on FieldWorks Lite: automatic';

/** `offline` replaces leg 1's trigger label while a step sets that flag. */
export const LEG_TRIGGERS: Record<LegId, string> & {offline: string} = {
  leg1: 'automatic',
  leg2: 'Sync on Lexbox tab',
  leg3: 'their Send/Receive',
  offline: 'offline',
};

/**
 * Tap-to-reveal detail behind each leg's trigger pill (a toggletip in the
 * diagram). Keeps the "where do I find it" fact next to the leg it belongs to,
 * instead of only in the table below. One or two short sentences each.
 */
export const TRIGGER_INFO: Record<LegId, {title: string; body: string}> = {
  leg1: {
    title: 'Automatic',
    body: 'Runs by itself after every edit and when you open the project. Nothing you have to press (Auto syncing on the FieldWorks Lite tab just runs it now).',
  },
  leg2: {
    title: 'Sync on the Lexbox tab',
    body: 'In FieldWorks Lite: Synchronize → Lexbox tab (the second tab) → Sync. Not Auto syncing on the FieldWorks Lite tab, which only moves changes between your device and Lexbox. People in the FieldWorks Lite beta also have “Sync FieldWorks Lite” on the project page on lexbox.org.',
  },
  leg3: {
    title: 'Send/Receive',
    body: 'Happens on your colleague’s computer, when they run Send/Receive in FieldWorks Classic. There’s nothing to do on your side.',
  },
};

export const TOKEN_TAGS = {you: 'your edit', them: 'their edit'};

export const LEGEND = [
  {kind: 'auto', text: 'happens by itself'},
  {kind: 'you', text: 'someone must do this'},
  {kind: 'them', text: 'your colleague does this'},
] as const;

export const QUESTIONS_HEADING = 'What do you want to know?';

export const ALL_ANSWERS_SUMMARY = 'Read all answers as text';

export const STEPPER_LABELS = {
  back: 'Back',
  next: 'Next',
  reset: 'Ask another question',
  /** Rendered as "Step 2 of 5". */
  count: (current: number, total: number) => `Step ${current} of ${total}`,
};
