import { logger } from '@papi/frontend';
import { useLocalizedStrings } from '@papi/frontend/react';
import type { IProjectModel } from 'lexicon';
import { Check, Trash2 } from 'lucide-react';
import {
  Alert,
  AlertDescription,
  Badge,
  Button,
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuTrigger,
  Label,
  Spinner,
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from 'platform-bible-react';
import {
  type KeyboardEvent,
  type ReactElement,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import { formatReplacementString, getErrorMessage } from 'platform-bible-utils';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';
import type { DownloadResult } from '../utils/fw-lite-api';

/** PAPI rejections read "JSON-RPC Request error (0): <real message>"; show only the real message. */
function userFacingError(e: unknown): string {
  return getErrorMessage(e).replace(/^JSON-RPC Request error \(\d+\): /, '');
}

/** Props for the LexiconPicker component */
interface LexiconPickerProps {
  loading?: boolean;
  localProjects?: IProjectModel[];
  /** Remote projects not yet downloaded (lexicon.remoteProjects dedupes them against local ones). */
  remoteProjects?: IProjectModel[];
  /** Whether any Lexbox server is signed in (drives the empty-state hint). */
  signedIn?: boolean;
  /** The project's applied lexicon, when known; pre-selected and marked with a "Current" badge. */
  initialCode?: string;
  /** The Paratext project's short name, shown in the heading when known. */
  projectName?: string;
  /** Name of the lexicon whose save just landed; shows the one-time confirmation line. */
  savedName?: string;
  /** Clears the confirmation line (the user is picking again). */
  onClearSaved?: () => void;
  /** True when localProjects is a language-filtered subset (shows the filter bar). */
  languageFiltered?: boolean;
  /** True when a language was known but nothing matched it, so the full list is shown with a note. */
  languageNoMatch?: boolean;
  /** The language tag the local list was filtered by, or that nothing matched. */
  filterLangTag?: string;
  /** Asks the parent to refetch the local list without the language filter. */
  onShowAll?: () => void;
  onCreateNew?: () => void;
  selectLexicon: (lexiconCode: string) => Promise<{ cancelled?: boolean }>;
  downloadAndSelect: (
    authority: string,
    lexiconCode: string,
  ) => Promise<{ result: DownloadResult; success: boolean; cancelled?: boolean }>;
  /** Deletes the local copy of a downloaded lexicon; the caller refreshes the lists. */
  deleteLexicon: (lexiconCode: string) => Promise<void>;
  /** The chosen lexicon was stored for the project; the parent tracks it and triggers the banner. */
  onSaved: (name: string, code: string) => void;
}

// Codes can collide across servers, so key options by server (or "local") plus code.
function keyFor(project: IProjectModel, local: boolean): string {
  return `${local ? 'local' : (project.server?.id ?? 'remote')}/${project.code}`;
}

// Case-insensitive alphabetical by display name, code as tiebreak.
function byName(a: IProjectModel, b: IProjectModel): number {
  return (
    (a.name || a.code).localeCompare(b.name || b.code, undefined, { sensitivity: 'base' }) ||
    a.code.localeCompare(b.code)
  );
}

/**
 * A searchable list for choosing the lexicon to use with the current Paratext project — local
 * lexicons, or remote ones that download on selection.
 */
export default function LexiconPicker({
  loading = false,
  localProjects,
  remoteProjects,
  signedIn = false,
  initialCode,
  projectName,
  savedName,
  onClearSaved,
  languageFiltered = false,
  languageNoMatch = false,
  filterLangTag,
  onShowAll,
  onCreateNew,
  selectLexicon,
  downloadAndSelect,
  deleteLexicon,
  onSaved,
}: LexiconPickerProps): ReactElement {
  const [localizedStrings] = useLocalizedStrings(LOCALIZED_STRING_KEYS);

  const [error, setError] = useState('');
  const [selectedKey, setSelectedKey] = useState(initialCode ? `local/${initialCode}` : '');
  const [busy, setBusy] = useState<'none' | 'saving' | 'downloading'>('none');
  const [pendingDelete, setPendingDelete] = useState<IProjectModel | undefined>();
  const [deleting, setDeleting] = useState(false);
  // Informational line under the button (deletion outcome, blocked-delete reason).
  const [notice, setNotice] = useState('');

  // Scroll the applied lexicon's row into view once, when it first mounts (on open). A callback ref
  // so it fires as soon as the row exists; guarded so later re-renders don't yank the scroll.
  const didScrollToCurrent = useRef(false);
  const currentRowRef = useCallback((node: HTMLDivElement | null) => {
    if (node && !didScrollToCurrent.current) {
      didScrollToCurrent.current = true;
      node.scrollIntoView({ block: 'nearest' });
    }
  }, []);

  // After a save, the just-chosen lexicon is the project's current one: point the selection at its
  // (now local) row so it shows checked with the primary button disabled. Handles the remote case,
  // where the selected key was the server row that the refresh turns into a local one.
  useEffect(() => {
    if (savedName && initialCode) setSelectedKey(`local/${initialCode}`);
  }, [savedName, initialCode]);

  // Human-readable name for the language the list was filtered by.
  const languageLabel = useMemo(() => {
    if (!filterLangTag) return '';
    try {
      return (
        new Intl.DisplayNames(navigator.language, { type: 'language' }).of(filterLangTag) ??
        filterLangTag
      );
    } catch {
      return filterLangTag;
    }
  }, [filterLangTag]);

  // Key -> project + whether choosing it means downloading first.
  const entries = useMemo(() => {
    const map = new Map<string, { project: IProjectModel; needsDownload: boolean }>();
    (localProjects ?? []).forEach((p) =>
      map.set(keyFor(p, true), { project: p, needsDownload: false }),
    );
    (remoteProjects ?? []).forEach((p) =>
      map.set(keyFor(p, false), { project: p, needsDownload: true }),
    );
    return map;
  }, [localProjects, remoteProjects]);

  // Stable alphabetical order within every group. The current lexicon isn't pinned to the top — it
  // keeps its place and is marked with a "Current" badge, so the list doesn't reshuffle as the
  // selection changes.
  const sortedLocal = useMemo(() => [...(localProjects ?? [])].sort(byName), [localProjects]);

  // One group per server with undownloaded projects, keyed by display name.
  const serverGroups = useMemo(() => {
    const byServer = new Map<string, IProjectModel[]>();
    (remoteProjects ?? []).forEach((p) => {
      const name = p.server?.displayName ?? p.server?.id ?? 'remote';
      byServer.set(name, [...(byServer.get(name) ?? []), p]);
    });
    return [...byServer.entries()].map(([name, list]) => [name, [...list].sort(byName)] as const);
  }, [remoteProjects]);

  const selected = entries.get(selectedKey);

  // Only a synced copy may be deleted (re-downloadable); the project's current lexicon is blocked
  // (but gets a disabled menu item with the reason, rather than silently no menu).
  const deletability = useCallback(
    (project: IProjectModel, local: boolean): 'yes' | 'current' | 'no' => {
      if (!(local && project.crdt && project.server)) return 'no';
      return project.code === initialCode ? 'current' : 'yes';
    },
    [initialCode],
  );

  const messageForFailure = useCallback(
    (result: DownloadResult): string => {
      switch (result) {
        case 'Forbidden':
          return localizedStrings['%lexicon_selectLexicon_downloadForbidden%'];
        case 'NotFound':
          return localizedStrings['%lexicon_selectLexicon_downloadNotFound%'];
        // Download itself was fine; selection is what failed.
        case 'Success':
        case 'AlreadyDownloaded':
          return localizedStrings['%lexicon_selectLexicon_selectFailed%'];
        default:
          return localizedStrings['%lexicon_selectLexicon_downloadFailed%'];
      }
    },
    [localizedStrings],
  );

  const confirm = useCallback(() => {
    if (!selected) return;
    const { project, needsDownload } = selected;
    const name = project.name || project.code;
    setError('');
    setNotice('');

    if (!needsDownload) {
      setBusy('saving');
      // eslint-disable-next-line promise/catch-or-return
      selectLexicon(project.code)
        .then(({ cancelled }) => {
          if (!cancelled) onSaved(name, project.code);
          return undefined;
        })
        .catch((e) => {
          logger.error(localizedStrings['%lexicon_selectLexicon_saveError%'], getErrorMessage(e));
          setError(userFacingError(e));
        })
        .finally(() => setBusy('none'));
      return;
    }

    const authority = project.server?.id;
    if (!authority) {
      setError(localizedStrings['%lexicon_selectLexicon_downloadFailed%']);
      return;
    }
    setBusy('downloading');
    // eslint-disable-next-line promise/catch-or-return
    downloadAndSelect(authority, project.code)
      .then(({ result, success, cancelled }) => {
        if (cancelled) return undefined;
        if (success) onSaved(name, project.code);
        else setError(messageForFailure(result));
        return undefined;
      })
      .catch((e) => {
        logger.error(
          localizedStrings['%lexicon_selectLexicon_downloadFailed%'],
          getErrorMessage(e),
        );
        setError(localizedStrings['%lexicon_selectLexicon_downloadFailed%']);
      })
      .finally(() => setBusy('none'));
  }, [downloadAndSelect, localizedStrings, messageForFailure, onSaved, selectLexicon, selected]);

  const beginDelete = useCallback((project: IProjectModel) => {
    setError('');
    setNotice('');
    setPendingDelete(project);
  }, []);

  const doDelete = useCallback(() => {
    if (!pendingDelete) return;
    const name = pendingDelete.name || pendingDelete.code;
    const key = keyFor(pendingDelete, true);
    setDeleting(true);
    // eslint-disable-next-line promise/catch-or-return
    deleteLexicon(pendingDelete.code)
      .then(() => {
        setNotice(
          formatReplacementString(localizedStrings['%lexicon_selectLexicon_deletedStatus%'], {
            name,
          }),
        );
        setSelectedKey((prev) => (prev === key ? '' : prev));
        return undefined;
      })
      .catch((e) => {
        logger.error('Error deleting local copy:', getErrorMessage(e));
        setError(userFacingError(e));
      })
      .finally(() => {
        setPendingDelete(undefined);
        setDeleting(false);
      });
  }, [deleteLexicon, localizedStrings, pendingDelete]);

  // Delete-key path to the delete flow: cmdk items never receive DOM focus, so a context menu
  // alone isn't keyboard-reachable. Only fires when the key wouldn't edit the filter text.
  const onCommandKeyDown = useCallback(
    (e: KeyboardEvent) => {
      if (e.key !== 'Delete' || pendingDelete) return;
      const input = e.target instanceof HTMLInputElement ? e.target : undefined;
      if (input && input.selectionStart !== input.value.length) return;
      if (!selected) return;
      const del = deletability(selected.project, !selected.needsDownload);
      if (del === 'yes') {
        e.preventDefault();
        beginDelete(selected.project);
      } else if (del === 'current') {
        // Same reason the context menu shows; don't swallow the key silently.
        e.preventDefault();
        setNotice(localizedStrings['%lexicon_selectLexicon_deleteDisabledCurrent%']);
      }
    },
    [beginDelete, deletability, localizedStrings, pendingDelete, selected],
  );

  const renderItem = useCallback(
    (project: IProjectModel, local: boolean): ReactElement => {
      const key = keyFor(project, local);
      const name = project.name || project.code;
      const isChosen = key === selectedKey;
      // The applied (current) lexicon, marked persistently so it stays legible when the pending
      // pick (the check) moves to another row. Remote rows are never the applied lexicon.
      const isApplied = local && project.code === initialCode;
      const isFieldWorks = !!(local && project.fwdata && !project.crdt);
      // Case-insensitive so "Happy"/"happy" doesn't show a pointless code line.
      const showCode = project.code.toLowerCase() !== name.toLowerCase();
      const item = (
        <CommandItem
          key={key}
          // Scroll the applied lexicon into view on open (replaces pinning it to the top).
          ref={isApplied ? currentRowRef : undefined}
          // cmdk filters on this value; include the key so items stay unique when names collide.
          value={`${name} ${project.code} ${key}`}
          onSelect={() => {
            setError('');
            setNotice('');
            onClearSaved?.();
            setSelectedKey(key);
          }}
          // items-center so the check and the right-rail badges vertically center across the row,
          // including when a second (code) line is present.
          className="tw:flex tw:items-center tw:gap-2"
        >
          <Check
            aria-hidden
            className={`tw:h-4 tw:w-4 tw:shrink-0 ${isChosen ? '' : 'tw:invisible'}`}
          />
          <div className="tw:flex-1 tw:min-w-0">
            <span className="tw:block tw:truncate" title={name}>
              {name}
            </span>
            {isChosen && (
              // cmdk's aria-selected tracks the highlighted item, not the checked one.
              <span className="tw:sr-only">
                {localizedStrings['%lexicon_selectLexicon_selectedIndicator%']}
              </span>
            )}
            {showCode && (
              <div className="tw:text-xs tw:text-muted-foreground tw:truncate" title={project.code}>
                {project.code}
              </div>
            )}
          </div>
          {(isApplied || isFieldWorks) && (
            <div className="tw:ms-auto tw:flex tw:items-center tw:gap-1 tw:shrink-0">
              {isApplied && (
                // Outline, not a filled variant: the theme collapses secondary/muted/accent to one
                // color, so a filled badge would vanish into the row-hover background. font-medium +
                // foreground text keeps it more prominent than the muted FieldWorks badge.
                <Badge className="tw:font-medium" variant="outline">
                  {localizedStrings['%lexicon_selectLexicon_badgeCurrent%']}
                </Badge>
              )}
              {isFieldWorks && (
                <Tooltip>
                  <TooltipTrigger asChild>
                    <Badge className="tw:font-normal tw:text-muted-foreground" variant="outline">
                      {localizedStrings['%lexicon_selectLexicon_badgeFieldWorks%']}
                    </Badge>
                  </TooltipTrigger>
                  <TooltipContent>
                    {localizedStrings['%lexicon_selectLexicon_badgeFieldWorksTip%']}
                  </TooltipContent>
                </Tooltip>
              )}
            </div>
          )}
        </CommandItem>
      );
      const del = deletability(project, local);
      if (del === 'no') return item;
      return (
        <ContextMenu key={key}>
          <ContextMenuTrigger asChild>{item}</ContextMenuTrigger>
          <ContextMenuContent>
            <ContextMenuItem disabled={del === 'current'} onSelect={() => beginDelete(project)}>
              <Trash2 aria-hidden className="tw:h-4 tw:w-4 tw:me-2" />
              {localizedStrings['%lexicon_selectLexicon_deleteLocalCopy%']}
            </ContextMenuItem>
            {del === 'current' && (
              // Not a tooltip: disabled Radix items are unreachable by pointer and keyboard.
              <div className="tw:max-w-60 tw:px-2 tw:pb-1.5 tw:text-xs tw:text-muted-foreground">
                {localizedStrings['%lexicon_selectLexicon_deleteDisabledCurrent%']}
              </div>
            )}
          </ContextMenuContent>
        </ContextMenu>
      );
    },
    [
      beginDelete,
      currentRowRef,
      deletability,
      initialCode,
      localizedStrings,
      onClearSaved,
      selectedKey,
    ],
  );

  if (busy === 'downloading') {
    return (
      <div className="tw:flex tw:flex-col tw:gap-2 tw:p-4" role="status" aria-live="polite">
        <div className="tw:flex tw:items-center tw:gap-2">
          <Spinner className="tw:h-4 tw:w-4" />
          <h3 className="tw:font-semibold">
            {localizedStrings['%lexicon_selectLexicon_downloading%']}{' '}
            {selected?.project.name || selected?.project.code} ...
          </h3>
        </div>
        <p className="tw:text-sm tw:text-muted-foreground">
          {localizedStrings['%lexicon_selectLexicon_downloadWait%']}
        </p>
      </div>
    );
  }

  const hasOptions = entries.size > 0;
  // When the pending pick is the applied lexicon there's nothing to change, so the primary action
  // stays disabled (the "Current" badge already marks it in-list). Remote rows are never applied.
  const isCurrentSelection =
    !!selected && !selected.needsDownload && selected.project.code === initialCode;
  // eslint-disable-next-line no-nested-ternary
  const confirmLabel = error
    ? localizedStrings['%lexicon_selectLexicon_retry%']
    : selected?.needsDownload
      ? localizedStrings['%lexicon_selectLexicon_downloadAndUse%']
      : localizedStrings['%lexicon_selectLexicon_use%'];

  return (
    <TooltipProvider>
      <div className="tw:flex tw:flex-col tw:flex-1 tw:min-h-0 tw:gap-3 tw:p-4">
        <Label className="tw:shrink-0">
          {projectName
            ? formatReplacementString(
                localizedStrings['%lexicon_selectLexicon_headingForProject%'],
                { project: projectName },
              )
            : localizedStrings['%lexicon_selectLexicon_heading%']}
        </Label>

        <Command
          className="tw:rounded-md tw:border tw:flex tw:flex-col tw:flex-1 tw:min-h-24 tw:overflow-hidden"
          onKeyDown={onCommandKeyDown}
        >
          <CommandInput
            placeholder={localizedStrings['%lexicon_selectLexicon_filterPlaceholder%']}
          />
          {languageFiltered && (
            <div className="tw:flex tw:items-center tw:gap-1 tw:border-b tw:px-3 tw:py-1 tw:text-xs tw:text-muted-foreground tw:shrink-0">
              <span>
                {formatReplacementString(
                  localizedStrings['%lexicon_selectLexicon_languageFiltered%'],
                  { language: languageLabel },
                )}
              </span>
              <Button
                className="tw:h-auto tw:p-0 tw:text-xs"
                onClick={onShowAll}
                size="sm"
                type="button"
                variant="link"
              >
                {localizedStrings['%lexicon_selectLexicon_showAll%']}
              </Button>
            </div>
          )}
          {/* No local lexicon matched the project language, so everything is shown — say why. */}
          {!languageFiltered && languageNoMatch && (
            <div className="tw:border-b tw:px-3 tw:py-1 tw:text-xs tw:text-muted-foreground tw:shrink-0">
              {formatReplacementString(
                localizedStrings['%lexicon_selectLexicon_languageNoMatch%'],
                { language: languageLabel },
              )}
            </div>
          )}
          <CommandList
            className="tw:flex-1 tw:overflow-y-auto"
            // Inline because the library's own max-h-[300px] class can win over a tw: override.
            style={{ maxHeight: 'none' }}
          >
            <CommandEmpty>
              {/* eslint-disable-next-line no-nested-ternary */}
              {loading ? (
                localizedStrings['%lexicon_selectLexicon_loading%']
              ) : hasOptions ? (
                localizedStrings['%lexicon_selectLexicon_noMatch%']
              ) : (
                <>
                  {localizedStrings['%lexicon_selectLexicon_noneFound%']}
                  {!signedIn && (
                    <p className="tw:mt-1 tw:text-muted-foreground">
                      {localizedStrings['%lexicon_selectLexicon_emptySignIn%']}
                    </p>
                  )}
                </>
              )}
            </CommandEmpty>
            {!!localProjects?.length && (
              <CommandGroup heading={localizedStrings['%lexicon_selectLexicon_groupLocal%']}>
                {sortedLocal.map((p) => renderItem(p, true))}
              </CommandGroup>
            )}
            {serverGroups.map(([serverName, projects]) => (
              <CommandGroup
                key={serverName}
                heading={formatReplacementString(
                  localizedStrings['%lexicon_selectLexicon_groupOnServer%'],
                  { server: serverName },
                )}
              >
                {projects.map((p) => renderItem(p, false))}
              </CommandGroup>
            ))}
          </CommandList>
        </Command>

        {pendingDelete ? (
          <Alert
            className="tw:shrink-0"
            onKeyDown={(e) => {
              if (e.key === 'Escape') setPendingDelete(undefined);
            }}
          >
            <AlertDescription>
              {formatReplacementString(localizedStrings['%lexicon_selectLexicon_deleteConfirm%'], {
                name: pendingDelete.name || pendingDelete.code,
                server: pendingDelete.server?.displayName ?? '',
              })}
            </AlertDescription>
            <div className="tw:mt-2 tw:flex tw:gap-2">
              <Button disabled={deleting} onClick={doDelete} type="button" variant="destructive">
                {deleting && <Spinner className="tw:h-4 tw:w-4 tw:me-2" />}
                {localizedStrings['%lexicon_selectLexicon_deleteConfirmAction%']}
              </Button>
              <Button
                // Focus the safe choice; Esc (handled above) also cancels.
                // eslint-disable-next-line jsx-a11y/no-autofocus
                autoFocus
                disabled={deleting}
                onClick={() => setPendingDelete(undefined)}
                type="button"
                variant="outline"
              >
                {localizedStrings['%lexicon_button_cancel%']}
              </Button>
            </div>
          </Alert>
        ) : (
          <>
            {!!error && (
              <Alert className="tw:shrink-0" variant="destructive">
                <AlertDescription role="alert">{error}</AlertDescription>
              </Alert>
            )}

            <Button
              className="tw:shrink-0"
              disabled={!selected || busy !== 'none' || isCurrentSelection}
              onClick={confirm}
              type="button"
            >
              {busy === 'saving' && <Spinner className="tw:h-4 tw:w-4 tw:me-2" />}
              {confirmLabel}
            </Button>

            {/* One-time "it worked" line at the point of action; reverts to the steady-state helper
                below on the next interaction (the in-list "Current" badge carries the lasting truth). */}
            {savedName && !error && (
              <p
                className="tw:flex tw:items-center tw:gap-1.5 tw:text-xs tw:text-muted-foreground tw:shrink-0"
                role="status"
              >
                <Check aria-hidden className="tw:h-3.5 tw:w-3.5 tw:shrink-0" />
                {projectName
                  ? formatReplacementString(
                      localizedStrings['%lexicon_selectLexicon_savedTitleForProject%'],
                      { name: savedName, project: projectName },
                    )
                  : formatReplacementString(
                      localizedStrings['%lexicon_selectLexicon_savedTitle%'],
                      {
                        name: savedName,
                      },
                    )}
              </p>
            )}

            {isCurrentSelection && !error && !savedName && (
              <p className="tw:text-xs tw:text-muted-foreground tw:shrink-0">
                {projectName
                  ? formatReplacementString(
                      localizedStrings['%lexicon_selectLexicon_alreadyCurrentForProject%'],
                      { project: projectName },
                    )
                  : localizedStrings['%lexicon_selectLexicon_alreadyCurrent%']}
              </p>
            )}

            {!!selected?.needsDownload && !error && (
              <p className="tw:text-xs tw:text-muted-foreground tw:shrink-0">
                {localizedStrings['%lexicon_selectLexicon_remoteHelper%']}
              </p>
            )}

            {!!notice && (
              <p className="tw:text-xs tw:text-muted-foreground tw:shrink-0" role="status">
                {notice}
              </p>
            )}
          </>
        )}

        {!!onCreateNew && (
          // No divider: the gap spacing plus the outline variant already set this secondary action
          // apart from the primary button above.
          <Button
            className="tw:w-full tw:shrink-0"
            onClick={onCreateNew}
            type="button"
            variant="outline"
          >
            {localizedStrings['%lexicon_createLexicon_button%']}
          </Button>
        )}
      </div>
    </TooltipProvider>
  );
}
