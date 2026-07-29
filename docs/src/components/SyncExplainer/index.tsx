import {useEffect, useRef, useState, type ReactNode} from 'react';

import {
  BADGE_LABELS,
  CLOUD_LABELS,
  GHOST_LABEL,
  INITIAL,
  LEGEND,
  LEG_TRIGGERS,
  NODE_LABELS,
  QUESTIONS_HEADING,
  SCENARIOS,
  STEPPER_LABELS,
  TOKEN_TAGS,
  type CheckableNodeId,
  type LegId,
  type NodeId,
  type Scenario,
  type Step,
  type TokenTarget,
} from './syncScenarios';
import styles from './styles.module.css';

const NODE_TOKEN_TARGETS: readonly TokenTarget[] = ['device', 'liteCopy', 'classicCopy', 'colleague'];

type MeasurableId = NodeId | LegId;

interface TokenPos {
  visible: boolean;
  x: number;
  y: number;
}

const HIDDEN_TOKEN: TokenPos = {visible: false, x: 0, y: 0};

function cx(...classes: (string | false | undefined)[]): string {
  return classes.filter(Boolean).join(' ');
}

/** Only INITIAL.text uses this; step sentences are plain text. */
function withBold(text: string): ReactNode[] {
  return text.split(/\*\*(.+?)\*\*/g).map((part, i) => (i % 2 ? <b key={i}>{part}</b> : part));
}

function NodeBox({
  label,
  icon,
  hl,
  dim,
  checked,
  elRef,
}: {
  label: {kind: string; name: string};
  icon?: ReactNode;
  hl: boolean;
  dim: boolean;
  checked: boolean;
  elRef: (el: HTMLDivElement | null) => void;
}): ReactNode {
  return (
    <div ref={elRef} className={cx(styles.node, hl && styles.hl, dim && styles.dim)}>
      <span className={cx(styles.check, checked && styles.on)} aria-hidden="true">
        ✓
      </span>
      {icon && <div className={styles.icon}>{icon}</div>}
      <div className={styles.k}>{label.kind}</div>
      <div className={styles.n}>{label.name}</div>
    </div>
  );
}

function Connector({
  num,
  kind,
  trigger,
  hl,
  dim,
  off,
  inner,
  elRef,
}: {
  num: number;
  kind: 'auto' | 'manual' | 'their';
  trigger: string;
  hl: boolean;
  dim: boolean;
  off?: boolean;
  inner?: boolean;
  elRef: (el: HTMLDivElement | null) => void;
}): ReactNode {
  return (
    <div
      ref={elRef}
      className={cx(
        styles.connector,
        styles[kind],
        inner && styles.inner,
        hl && styles.hl,
        dim && styles.dim,
        off && styles.off,
      )}>
      <span className={styles.legnum}>{num}</span>
      <div className={styles.track}>
        <span className={styles.ah2} />
      </div>
      <span className={styles.trigger}>{trigger}</span>
    </div>
  );
}

const phoneIcon = (
  <svg
    width="26"
    height="26"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.7"
    strokeLinecap="round">
    <rect x="7" y="2.5" width="10" height="19" rx="2.2" />
    <line x1="10.5" y1="18.5" x2="13.5" y2="18.5" />
  </svg>
);

const desktopIcon = (
  <svg
    width="26"
    height="26"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.7"
    strokeLinecap="round"
    strokeLinejoin="round">
    <rect x="3" y="4" width="18" height="12.5" rx="1.8" />
    <line x1="8" y1="20.5" x2="16" y2="20.5" />
    <line x1="12" y1="16.5" x2="12" y2="20.5" />
  </svg>
);

const cloudIcon = (
  <svg
    width="20"
    height="20"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.7"
    strokeLinecap="round"
    strokeLinejoin="round">
    <path d="M17.5 18.5h-11a4.5 4.5 0 1 1 .9-8.9 6 6 0 0 1 11.6 1.6 3.7 3.7 0 0 1-1.5 7.3z" />
  </svg>
);

export default function SyncExplainer(): ReactNode {
  const [scenario, setScenario] = useState<Scenario | null>(null);
  const [stepIndex, setStepIndex] = useState(0);
  const [tokens, setTokens] = useState<[TokenPos, TokenPos]>([HIDDEN_TOKEN, HIDDEN_TOKEN]);
  const [resizeTick, setResizeTick] = useState(0);

  const stageRef = useRef<HTMLDivElement | null>(null);
  const capTitleRef = useRef<HTMLHeadingElement | null>(null);
  const elements = useRef<Partial<Record<MeasurableId, HTMLDivElement | null>>>({});
  const setEl = (id: MeasurableId) => (el: HTMLDivElement | null) => {
    elements.current[id] = el;
  };

  const step: Step | null = scenario ? scenario.steps[stepIndex] : null;
  const lastStep = scenario ? stepIndex === scenario.steps.length - 1 : false;

  const has = (list: readonly string[] | undefined, id: string): boolean => !!list?.includes(id);
  const isHl = (id: NodeId | LegId): boolean => has(step?.legs, id) || has(step?.hlNodes, id);
  const isDim = (id: NodeId | LegId): boolean => has(step?.dim, id);
  const isChecked = (id: CheckableNodeId): boolean => has(step?.check, id);

  useEffect(() => {
    const stage = stageRef.current;
    if (!stage) return;
    const stageRect = stage.getBoundingClientRect();
    const place = (at: TokenTarget | undefined, offset: number, previous: TokenPos): TokenPos => {
      const el = at ? elements.current[at] : null;
      if (!at || !el) return {...previous, visible: false};
      const rect = el.getBoundingClientRect();
      // Nodes: perch the dot on the top edge so it never covers the label. Legs: sit on the line.
      const y = NODE_TOKEN_TARGETS.includes(at)
        ? rect.top - stageRect.top - 12
        : rect.top - stageRect.top + rect.height / 2 - 8;
      return {visible: true, x: rect.left - stageRect.left + rect.width / 2 - 8 + offset, y};
    };
    const coLocated = !!step?.t1 && step.t1 === step.t2;
    setTokens((previous) => [
      place(step?.t1, coLocated ? -12 : 0, previous[0]),
      place(step?.t2, coLocated ? 12 : 0, previous[1]),
    ]);
  }, [step, resizeTick]);

  useEffect(() => {
    let timer: ReturnType<typeof setTimeout>;
    const onResize = (): void => {
      clearTimeout(timer);
      timer = setTimeout(() => setResizeTick((n) => n + 1), 120);
    };
    window.addEventListener('resize', onResize);
    return () => {
      clearTimeout(timer);
      window.removeEventListener('resize', onResize);
    };
  }, []);

  useEffect(() => {
    if (!scenario) return;
    const onKeyDown = (e: KeyboardEvent): void => {
      const target = e.target as HTMLElement | null;
      // Don't steal arrow keys from the search box or any other text field.
      if (target?.isContentEditable || ['INPUT', 'TEXTAREA', 'SELECT'].includes(target?.tagName ?? '')) return;
      if (e.key === 'ArrowRight') setStepIndex((i) => Math.min(i + 1, scenario.steps.length - 1));
      if (e.key === 'ArrowLeft') setStepIndex((i) => Math.max(i - 1, 0));
    };
    document.addEventListener('keydown', onKeyDown);
    return () => document.removeEventListener('keydown', onKeyDown);
  }, [scenario]);

  const selectScenario = (s: Scenario): void => {
    setScenario(s);
    setStepIndex(0);
    capTitleRef.current?.focus({preventScroll: true});
  };

  const badge = step?.badge;

  return (
    <div className={styles.root}>
      <div className={styles.stage} ref={stageRef}>
        <div className={styles.topology}>
          <div className={styles.col}>
            <NodeBox
              label={NODE_LABELS.device}
              icon={phoneIcon}
              hl={isHl('device')}
              dim={isDim('device')}
              checked={isChecked('device')}
              elRef={setEl('device')}
            />
            <span className={cx(styles.ghost, step?.ghost && styles.on)}>{GHOST_LABEL}</span>
          </div>

          <Connector
            num={1}
            kind="auto"
            trigger={step?.offline ? LEG_TRIGGERS.offline : LEG_TRIGGERS.leg1}
            hl={isHl('leg1')}
            dim={isDim('leg1')}
            off={step?.offline}
            elRef={setEl('leg1')}
          />

          <div
            ref={setEl('cloud')}
            className={cx(styles.cloud, isHl('cloud') && styles.hl, isDim('cloud') && styles.dim)}>
            <div className={styles.cloudHead}>
              <span className={styles.n}>
                {cloudIcon} {CLOUD_LABELS.name}
              </span>
              <span className={styles.sub}>{CLOUD_LABELS.sub}</span>
            </div>
            <div className={styles.copies}>
              <NodeBox
                label={NODE_LABELS.liteCopy}
                hl={isHl('liteCopy')}
                dim={isDim('liteCopy')}
                checked={isChecked('liteCopy')}
                elRef={setEl('liteCopy')}
              />
              <Connector
                num={2}
                kind="manual"
                inner
                trigger={LEG_TRIGGERS.leg2}
                hl={isHl('leg2')}
                dim={isDim('leg2')}
                elRef={setEl('leg2')}
              />
              <NodeBox
                label={NODE_LABELS.classicCopy}
                hl={isHl('classicCopy')}
                dim={isDim('classicCopy')}
                checked={isChecked('classicCopy')}
                elRef={setEl('classicCopy')}
              />
            </div>
          </div>

          <Connector
            num={3}
            kind="their"
            trigger={LEG_TRIGGERS.leg3}
            hl={isHl('leg3')}
            dim={isDim('leg3')}
            elRef={setEl('leg3')}
          />

          <div className={styles.col}>
            <NodeBox
              label={NODE_LABELS.colleague}
              icon={desktopIcon}
              hl={isHl('colleague')}
              dim={isDim('colleague')}
              checked={isChecked('colleague')}
              elRef={setEl('colleague')}
            />
          </div>
        </div>

        {([
          ['you', TOKEN_TAGS.you, tokens[0]],
          ['them', TOKEN_TAGS.them, tokens[1]],
        ] as const).map(([who, tag, pos]) => (
          <div
            key={who}
            className={cx(styles.token, styles[who], pos.visible && styles.on)}
            style={{transform: `translate(${pos.x}px,${pos.y}px)`}}>
            <span className={styles.dot} />
            <span className={styles.tag}>{tag}</span>
          </div>
        ))}

        <div className={styles.legend} aria-hidden="true">
          {LEGEND.map((item) => (
            <span
              key={item.kind}
              className={
                item.kind === 'auto' ? styles.lAuto : item.kind === 'you' ? styles.lYou : styles.lThem
              }>
              <i /> {item.text}
            </span>
          ))}
        </div>
      </div>

      <div className={styles.questions}>
        <h2 id="sync-explainer-questions">{QUESTIONS_HEADING}</h2>
        <div className={styles.chips} role="group" aria-labelledby="sync-explainer-questions">
          {SCENARIOS.map((s) => (
            <button
              key={s.id}
              type="button"
              aria-pressed={scenario?.id === s.id}
              onClick={() => selectScenario(s)}>
              {s.q}
            </button>
          ))}
        </div>
      </div>

      <div className={styles.caption} aria-live="polite">
        {badge && <span className={cx(styles.badge, styles[badge])}>{BADGE_LABELS[badge]}</span>}
        <h3 ref={capTitleRef} tabIndex={-1}>
          {scenario ? scenario.q : INITIAL.title}
        </h3>
        <p>{step ? step.text : withBold(INITIAL.text)}</p>
        {scenario && (
          <div className={styles.stepper}>
            <div className={styles.dots} aria-hidden="true">
              {scenario.steps.map((s, i) => (
                <i key={s.text} className={cx(i === stepIndex && styles.cur)} />
              ))}
            </div>
            <span className={styles.count}>
              {STEPPER_LABELS.count(stepIndex + 1, scenario.steps.length)}
            </span>
            <span className={styles.grow} />
            <button
              type="button"
              disabled={stepIndex === 0}
              onClick={() => setStepIndex((i) => Math.max(i - 1, 0))}>
              {STEPPER_LABELS.back}
            </button>
            <button
              type="button"
              className={styles.primary}
              disabled={lastStep}
              onClick={() => setStepIndex((i) => Math.min(i + 1, scenario.steps.length - 1))}>
              {STEPPER_LABELS.next}
            </button>
            {lastStep && (
              <button
                type="button"
                onClick={() => {
                  setScenario(null);
                  setStepIndex(0);
                }}>
                {STEPPER_LABELS.reset}
              </button>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
