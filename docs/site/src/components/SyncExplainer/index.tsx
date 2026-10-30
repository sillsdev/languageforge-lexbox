import {
  useEffect,
  useRef,
  useState,
  type Dispatch,
  type KeyboardEvent,
  type ReactNode,
  type SetStateAction,
} from 'react';
import {useHistory, useLocation} from '@docusaurus/router';
import useBrokenLinks from '@docusaurus/useBrokenLinks';
import useIsomorphicLayoutEffect from '@docusaurus/useIsomorphicLayoutEffect';
import Details from '@theme/Details';
import ThemedImage from '@theme/ThemedImage';
import useBaseUrl from '@docusaurus/useBaseUrl';

import {
  ALL_ANSWERS_SUMMARY,
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
  TRIGGER_INFO,
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

interface Point {
  x: number;
  y: number;
}

interface TokenPos extends Point {
  visible: boolean;
  /** Jump straight to the spot instead of gliding there from a stale position. */
  snap: boolean;
  /** Top-left of the tag, relative to the token. Null hides the tag. */
  tag: Point | null;
}

interface Tokens {
  scenarioId: string | null;
  list: [TokenPos, TokenPos];
}

interface Box {
  left: number;
  top: number;
  right: number;
  bottom: number;
}

const overlapArea = (a: Box, b: Box): number =>
  Math.max(0, Math.min(a.right, b.right) - Math.max(a.left, b.left)) *
  Math.max(0, Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top));

const HIDDEN_TOKEN: TokenPos = {visible: false, snap: true, x: 0, y: 0, tag: null};
const DOT_SIZE = 16;
const TAG_GAP = 4;

const cx = (...classes: (string | false | undefined)[]): string => classes.filter(Boolean).join(' ');

/** Only INITIAL.text uses this; step sentences are plain text. */
const withBold = (text: string): ReactNode[] => {
  const nodes: ReactNode[] = [];
  let pos = 0;
  text.split(/\*\*(.+?)\*\*/g).forEach((part, i) => {
    const bold = i % 2 === 1;
    const key = pos;
    // Advance past the part plus its ** markers so keys stay unique source positions.
    pos += part.length + (bold ? 4 : 0);
    if (part) nodes.push(bold ? <b key={key}>{part}</b> : <span key={key}>{part}</span>);
  });
  return nodes;
};

const prefersReducedMotion = (): boolean =>
  typeof window !== 'undefined' && Boolean(window.matchMedia?.('(prefers-reduced-motion: reduce)').matches);

const scenarioFromHash = (hash: string): Scenario | undefined => SCENARIOS.find((s) => `#${s.id}` === hash);

const NodeBox = ({
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
}): ReactNode => (
  <div ref={elRef} className={cx(styles.node, hl && styles.hl, dim && styles.dim)}>
    <span className={cx(styles.check, checked && styles.on)} aria-hidden="true">
      ✓
    </span>
    {icon && <div className={styles.icon}>{icon}</div>}
    <div className={styles.k}>{label.kind}</div>
    <div className={styles.n}>{label.name}</div>
  </div>
);

const Connector = ({
  num,
  kind,
  trigger,
  info,
  infoOpen,
  onToggleInfo,
  hl,
  dim,
  off,
  inner,
  elRef,
}: {
  num: number;
  kind: 'auto' | 'manual' | 'their';
  trigger: string;
  info?: {title: string; body: string};
  infoOpen?: boolean;
  onToggleInfo?: () => void;
  hl: boolean;
  dim: boolean;
  off?: boolean;
  inner?: boolean;
  elRef: (el: HTMLDivElement | null) => void;
}): ReactNode => (
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
    {info ? (
      <span className={styles.infoAnchor}>
        <button
          type="button"
          className={cx(styles.trigger, styles.hasInfo)}
          aria-expanded={Boolean(infoOpen)}
          onClick={onToggleInfo}>
          {trigger}
          <span className={styles.infoMark} aria-hidden="true">
            i
          </span>
        </button>
        {/* Kept mounted so the live region announces on open, not on insert. */}
        <span className={cx(styles.infoPop, infoOpen && styles.on)} role="status" aria-live="polite">
          {infoOpen && (
            <>
              <span className={styles.infoTitle}>{info.title}</span> {info.body}
            </>
          )}
        </span>
      </span>
    ) : (
      <span className={styles.trigger}>{trigger}</span>
    )}
  </div>
);

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

// Product logos, so readers can match the boxes to the apps they know. The device box keeps
// the phone icon: FieldWorks Lite has no logo of its own yet, and the Lexbox logo must not
// stand in for it.
const FlexIcon = () => <img src={useBaseUrl('/img/flex-logo.png')} alt="" width={26} height={26} />;

const LexboxIcon = () => (
  <ThemedImage
    alt=""
    width={20}
    height={20}
    sources={{light: useBaseUrl('/img/lexbox-logo-light.svg'), dark: useBaseUrl('/img/lexbox-logo-dark.svg')}}
  />
);

interface StepQueries {
  isHl: (id: NodeId | LegId) => boolean;
  isDim: (id: NodeId | LegId) => boolean;
  isChecked: (id: CheckableNodeId) => boolean;
}

const Topology = ({
  step,
  ghostReserved,
  queries: {isHl, isDim, isChecked},
  setEl,
  openInfo,
  onToggleInfo,
}: {
  step: Step | null;
  ghostReserved: boolean;
  queries: StepQueries;
  setEl: (id: MeasurableId) => (el: HTMLDivElement | null) => void;
  openInfo: LegId | null;
  onToggleInfo: (id: LegId) => void;
}): ReactNode => (
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
      <span className={cx(styles.ghost, ghostReserved && styles.reserved, step?.ghost && styles.on)}>
        {GHOST_LABEL}
      </span>
    </div>

    <Connector
      num={1}
      kind="auto"
      trigger={step?.offline ? LEG_TRIGGERS.offline : LEG_TRIGGERS.leg1}
      info={TRIGGER_INFO.leg1}
      infoOpen={openInfo === 'leg1'}
      onToggleInfo={() => onToggleInfo('leg1')}
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
          <LexboxIcon /> {CLOUD_LABELS.name}
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
          info={TRIGGER_INFO.leg2}
          infoOpen={openInfo === 'leg2'}
          onToggleInfo={() => onToggleInfo('leg2')}
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
      info={TRIGGER_INFO.leg3}
      infoOpen={openInfo === 'leg3'}
      onToggleInfo={() => onToggleInfo('leg3')}
      hl={isHl('leg3')}
      dim={isDim('leg3')}
      elRef={setEl('leg3')}
    />

    <div className={styles.col}>
      <NodeBox
        label={NODE_LABELS.colleague}
        icon={<FlexIcon />}
        hl={isHl('colleague')}
        dim={isDim('colleague')}
        checked={isChecked('colleague')}
        elRef={setEl('colleague')}
      />
    </div>
  </div>
);

const Stepper = ({
  scenario,
  stepIndex,
  setStepIndex,
  onReset,
}: {
  scenario: Scenario;
  stepIndex: number;
  setStepIndex: Dispatch<SetStateAction<number>>;
  onReset: () => void;
}): ReactNode => {
  const lastStep = stepIndex === scenario.steps.length - 1;
  // aria-disabled instead of disabled: a disabled button drops keyboard focus to <body>.
  return (
    <div className={styles.stepper}>
      <div className={styles.dots} aria-hidden="true">
        {scenario.steps.map((_, i) => (
          <i key={i} className={cx(i === stepIndex && styles.cur)} />
        ))}
      </div>
      <span className={styles.count}>{STEPPER_LABELS.count(stepIndex + 1, scenario.steps.length)}</span>
      <span className={styles.grow} />
      <button
        type="button"
        aria-disabled={stepIndex === 0}
        onClick={() => setStepIndex((i) => Math.max(i - 1, 0))}>
        {STEPPER_LABELS.back}
      </button>
      <button
        type="button"
        className={styles.primary}
        aria-disabled={lastStep}
        onClick={() => setStepIndex((i) => Math.min(i + 1, scenario.steps.length - 1))}>
        {STEPPER_LABELS.next}
        <span aria-hidden="true"> ›</span>
      </button>
      {lastStep && (
        <button type="button" onClick={onReset}>
          {STEPPER_LABELS.reset}
        </button>
      )}
    </div>
  );
};

const AllAnswers = (): ReactNode => (
  <Details className={styles.allAnswers} summary={<summary>{ALL_ANSWERS_SUMMARY}</summary>}>
    {SCENARIOS.map((s) => (
      <div key={s.id}>
        <p className={styles.allAnswersQ}>
          <strong>{s.q}</strong>
        </p>
        <ol>
          {s.steps.map((step, i) => (
            <li key={i}>{step.text}</li>
          ))}
        </ol>
      </div>
    ))}
  </Details>
);

const SyncExplainer = (): ReactNode => {
  const [scenario, setScenario] = useState<Scenario | null>(null);
  const [stepIndex, setStepIndex] = useState(0);
  const [tokens, setTokens] = useState<Tokens>({scenarioId: null, list: [HIDDEN_TOKEN, HIDDEN_TOKEN]});
  const [resizeTick, setResizeTick] = useState(0);
  const [openInfo, setOpenInfo] = useState<LegId | null>(null);

  const location = useLocation();
  const history = useHistory();
  const brokenLinks = useBrokenLinks();
  // Scenario ids are link targets (support replies link to them) but no element carries them,
  // so register them for the broken-anchor check.
  SCENARIOS.forEach((s) => brokenLinks.collectAnchor(s.id));

  const stageRef = useRef<HTMLDivElement | null>(null);
  const playerRef = useRef<HTMLDivElement | null>(null);
  const capTitleRef = useRef<HTMLHeadingElement | null>(null);
  const chipsRef = useRef<HTMLDivElement | null>(null);
  const tagRefs = useRef<(HTMLSpanElement | null)[]>([]);
  const keepScrollY = useRef<number | null>(null);
  const elements = useRef<Partial<Record<MeasurableId, HTMLDivElement | null>>>({});
  const setEl = (id: MeasurableId) => (el: HTMLDivElement | null) => {
    elements.current[id] = el;
  };

  const step: Step | null = scenario ? scenario.steps[stepIndex] : null;
  const ghostReserved = scenario?.steps.some((s) => s.ghost) ?? false;

  const has = (list: readonly string[] | undefined, id: string): boolean =>
    Boolean(list?.includes(id));
  const queries: StepQueries = {
    isHl: (id) => has(step?.legs, id) || has(step?.hlNodes, id),
    isDim: (id) => has(step?.dim, id),
    isChecked: (id) => has(step?.check, id),
  };

  // Layout effect so every rect is read in the same frame, before the caller scrolls the player.
  useIsomorphicLayoutEffect(() => {
    const stage = stageRef.current;
    if (!stage) return;
    const stageRect = stage.getBoundingClientRect();
    const rel = (r: DOMRect): Box => ({
      left: r.left - stageRect.left,
      top: r.top - stageRect.top,
      right: r.right - stageRect.left,
      bottom: r.bottom - stageRect.top,
    });
    const around = ({x, y}: Point, radius: number): Box => ({
      left: x - radius,
      top: y - radius,
      right: x + radius,
      bottom: y + radius,
    });

    const keepClear: Box[] = [
      ...stage.querySelectorAll(
        `.${styles.k}, .${styles.n}, .${styles.sub}, .${styles.legnum}, .${styles.trigger}, .${styles.ghost}.${styles.on}`,
      ),
    ].map((el) => rel(el.getBoundingClientRect()));
    // Measured from the center because a check may still be mid scale-in.
    stage.querySelectorAll<HTMLElement>(`.${styles.check}.${styles.on}`).forEach((el) => {
      const box = rel(el.getBoundingClientRect());
      keepClear.push(around({x: (box.left + box.right) / 2, y: (box.top + box.bottom) / 2}, el.offsetWidth / 2));
    });
    const icons = [...stage.querySelectorAll(`.${styles.icon}`)].map((el) => rel(el.getBoundingClientRect()));
    const coverage = (box: Box, others: Box[]): number =>
      others.reduce((sum, other) => sum + overlapArea(box, other), 0);
    // Includes the dot's surface-colored ring.
    const dotRadius = DOT_SIZE / 2 + 4;

    // Slides the dot along its node's top edge (or its leg's track) to the spot nearest the
    // middle that covers no text: in the vertical layout a leg's pill sits right on the next node.
    const dotAt = (at: TokenTarget | undefined, offset: number): Point | null => {
      const target = at ? elements.current[at] : null;
      if (!at || !target) return null;
      const isNode = NODE_TOKEN_TARGETS.includes(at);
      const track = isNode ? target : target.querySelector(`.${styles.track}`);
      if (!track) return null;
      const box = rel(track.getBoundingClientRect());
      const middle = {
        x: (box.left + box.right) / 2 + offset,
        y: isNode ? box.top - 4 : (box.top + box.bottom) / 2,
      };
      const alongX = isNode || box.right - box.left >= box.bottom - box.top;
      const room = Math.max(0, (alongX ? box.right - box.left : box.bottom - box.top) / 2 - (isNode ? 12 : 0));
      let best = middle;
      let bestCost = Infinity;
      for (let shift = 0; shift <= room; shift += 4) {
        for (const d of shift ? [shift, -shift] : [0]) {
          const spot = alongX ? {x: middle.x + d, y: middle.y} : {x: middle.x, y: middle.y + d};
          const cost = coverage(around(spot, dotRadius), keepClear);
          if (cost < bestCost) {
            best = spot;
            bestCost = cost;
          }
          if (cost === 0) return spot;
        }
      }
      return best;
    };
    const coLocated = step?.t1 !== undefined && step.t1 === step.t2;
    const dots: (Point | null)[] = [];
    [step?.t1, step?.t2].forEach((at, i) => {
      const dot = dotAt(at, coLocated ? (i ? 12 : -12) : 0);
      if (dot) keepClear.push(around(dot, dotRadius));
      dots.push(dot);
    });

    // Tries the spots around the dot; a tag that would cover text or leave the stage is hidden
    // rather than moved away from its dot. Covering an icon is allowed when nothing else fits.
    const placeTag = (dot: Point, el: HTMLElement | null | undefined): Box | null => {
      if (!el) return null;
      const w = el.offsetWidth;
      const h = el.offsetHeight;
      const r = DOT_SIZE / 2 + TAG_GAP;
      const spots: Point[] = [
        {x: dot.x - r - w, y: dot.y - h / 2},
        {x: dot.x + r, y: dot.y - h / 2},
        {x: dot.x - w / 2, y: dot.y - r - h},
        {x: dot.x - w / 2, y: dot.y + r},
        {x: dot.x - r - w, y: dot.y - r - h},
        {x: dot.x + r, y: dot.y - r - h},
        {x: dot.x - r - w, y: dot.y + r},
        {x: dot.x + r, y: dot.y + r},
      ];
      let best: Box | null = null;
      let bestCost = Infinity;
      for (const {x, y} of spots) {
        const box = {left: x, top: y, right: x + w, bottom: y + h};
        const outside = x < 0 || y < 0 || box.right > stageRect.width || box.bottom > stageRect.height;
        if (outside || coverage(box, keepClear) > 0) continue;
        const cost = coverage(box, icons);
        if (cost < bestCost) {
          best = box;
          bestCost = cost;
        }
        if (cost === 0) break;
      }
      if (best) keepClear.push(best);
      return best;
    };

    const placed = dots.map((dot, i) => {
      if (!dot) return null;
      const x = dot.x - DOT_SIZE / 2;
      const y = dot.y - DOT_SIZE / 2;
      // Two tags on one spot can't both sit next to their own dot; the colors still tell them apart.
      const tag = coLocated ? null : placeTag(dot, tagRefs.current[i]);
      return {x, y, tag: tag && {x: tag.left - x, y: tag.top - y}};
    });
    const scenarioId = scenario?.id ?? null;
    setTokens((previous) => {
      const fresh = previous.scenarioId !== scenarioId;
      const next = (i: 0 | 1): TokenPos => {
        const prev = previous.list[i];
        const p = placed[i];
        if (!p) return {...prev, visible: false};
        return {...p, visible: true, snap: fresh || !prev.visible};
      };
      return {scenarioId, list: [next(0), next(1)]};
    });
  }, [scenario, step, resizeTick]);

  // Observe the stage, not the window: the docs column also resizes when the sidebar collapses.
  useEffect(() => {
    const stage = stageRef.current;
    if (!stage) return undefined;
    let timer: ReturnType<typeof setTimeout>;
    const observer = new ResizeObserver(() => {
      clearTimeout(timer);
      timer = setTimeout(() => setResizeTick((n) => n + 1), 120);
    });
    observer.observe(stage);
    return () => {
      clearTimeout(timer);
      observer.disconnect();
    };
  }, []);

  // A toggletip must never linger over a diagram that's mid-transition.
  useEffect(() => {
    setOpenInfo(null);
  }, [stepIndex, scenario]);

  useEffect(() => {
    if (!openInfo) return undefined;
    const onPointerDown = (e: PointerEvent): void => {
      if (!(e.target as HTMLElement).closest(`.${styles.infoAnchor}`)) setOpenInfo(null);
    };
    const onKeyDown = (e: globalThis.KeyboardEvent): void => {
      if (e.key === 'Escape') setOpenInfo(null);
    };
    document.addEventListener('pointerdown', onPointerDown);
    document.addEventListener('keydown', onKeyDown);
    return () => {
      document.removeEventListener('pointerdown', onPointerDown);
      document.removeEventListener('keydown', onKeyDown);
    };
  }, [openInfo]);

  const toggleInfo = (id: LegId): void => setOpenInfo((cur) => (cur === id ? null : id));

  const showScenario = (s: Scenario, focusCaption: boolean): void => {
    setScenario(s);
    setStepIndex(0);
    // Chips sit below the player, so bring the diagram + stepper back into view.
    // Focus first, scroll last, so the scroll wins if a browser ignores
    // preventScroll and would otherwise jump to the focused caption.
    requestAnimationFrame(() => {
      if (focusCaption) capTitleRef.current?.focus({preventScroll: true});
      playerRef.current?.scrollIntoView({
        block: 'start',
        behavior: prefersReducedMotion() ? 'auto' : 'smooth',
      });
    });
  };

  useEffect(() => {
    const linked = scenarioFromHash(location.hash);
    if (linked && linked.id !== scenario?.id) showScenario(linked, false);
  }, [location.hash]);

  // Docusaurus scrolls to the top when the hash is cleared, in a layout effect of an ancestor
  // (so after this one). The microtask runs after it and before paint.
  useIsomorphicLayoutEffect(() => {
    const y = keepScrollY.current;
    if (y === null) return;
    keepScrollY.current = null;
    queueMicrotask(() => window.scrollTo({top: y, behavior: 'instant'}));
  }, [location.hash]);

  const selectScenario = (s: Scenario): void => {
    showScenario(s, true);
    if (location.hash !== `#${s.id}`) history.replace({...location, hash: `#${s.id}`});
  };

  const reset = (): void => {
    setScenario(null);
    setStepIndex(0);
    chipsRef.current?.querySelector('button')?.focus();
    if (scenarioFromHash(location.hash)) {
      keepScrollY.current = window.scrollY;
      history.replace({...location, hash: ''});
    }
  };

  const onKeyDown = (e: KeyboardEvent<HTMLDivElement>): void => {
    if (!scenario || e.defaultPrevented || e.altKey || e.ctrlKey || e.metaKey || e.shiftKey) return;
    if (e.key === 'ArrowRight') setStepIndex((i) => Math.min(i + 1, scenario.steps.length - 1));
    if (e.key === 'ArrowLeft') setStepIndex((i) => Math.max(i - 1, 0));
  };

  const badge = step?.badge;

  return (
    <>
      <div className={styles.root} onKeyDown={onKeyDown}>
        <div className={styles.player} ref={playerRef}>
          <div className={styles.stage} ref={stageRef}>
            <Topology
              step={step}
              ghostReserved={ghostReserved}
              queries={queries}
              setEl={setEl}
              openInfo={openInfo}
              onToggleInfo={toggleInfo}
            />

            {([
              ['you', TOKEN_TAGS.you, tokens.list[0]],
              ['them', TOKEN_TAGS.them, tokens.list[1]],
            ] as const).map(([who, tag, pos], i) => (
              <div
                key={who}
                className={cx(styles.token, styles[who], pos.visible && styles.on, pos.snap && styles.snap)}
                style={{transform: `translate(${pos.x}px,${pos.y}px)`}}>
                <span className={styles.dot} />
                <span
                  ref={(el) => {
                    tagRefs.current[i] = el;
                  }}
                  className={cx(styles.tag, !pos.tag && styles.hidden)}
                  style={pos.tag ? {left: pos.tag.x, top: pos.tag.y} : undefined}>
                  {tag}
                </span>
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

          <div className={cx(styles.caption, Boolean(scenario) && styles.active)} aria-live="polite">
            {badge && <span className={cx(styles.badge, styles[badge])}>{BADGE_LABELS[badge]}</span>}
            <h3 ref={capTitleRef} tabIndex={-1}>
              {scenario ? scenario.q : INITIAL.title}
            </h3>
            <p>{step ? step.text : withBold(INITIAL.text)}</p>
            {scenario && (
              <Stepper scenario={scenario} stepIndex={stepIndex} setStepIndex={setStepIndex} onReset={reset} />
            )}
          </div>
        </div>

        <div className={styles.questions}>
          <div className={styles.qlabel} id="sync-explainer-questions">
            {QUESTIONS_HEADING}
          </div>
          <div className={styles.chips} role="group" aria-labelledby="sync-explainer-questions" ref={chipsRef}>
            {SCENARIOS.map((s) => (
              <button
                key={s.id}
                type="button"
                aria-current={scenario?.id === s.id ? 'true' : undefined}
                onClick={() => selectScenario(s)}>
                {s.q}
              </button>
            ))}
          </div>
        </div>
      </div>
      <AllAnswers />
    </>
  );
};

export default SyncExplainer;
