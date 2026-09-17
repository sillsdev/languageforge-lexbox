import { logger } from '@papi/frontend';
import { useLocalizedStrings } from '@papi/frontend/react';
import type { IProjectModel } from 'lexicon';
import { Check } from 'lucide-react';
import {
  Alert,
  AlertDescription,
  Button,
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandList,
  Label,
  Spinner,
  TooltipProvider,
} from 'platform-bible-react';
import { type ReactElement, useEffect, useMemo, useState } from 'react';
import { formatReplacementString, getErrorMessage } from 'platform-bible-utils';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';
import type { DownloadAndSelectResult, DownloadResult } from '../utils/fw-lite-api';
import DeleteConfirm from './delete-confirm';
import LexiconRow from './lexicon-row';

interface LexiconPickerProps {
  loading?: boolean;
  localProjects?: IProjectModel[];
  /** Remote projects not yet downloaded (lexicon.remoteProjects dedupes them against local ones). */
  remoteProjects?: IProjectModel[];
  /** Whether any Lexbox server is signed in (drives the empty-state hint). */
  signedIn?: boolean;
  /** The project's applied lexicon, when known; pre-selected and marked with a "Current" badge. */
  appliedCode?: string;
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
  downloadAndSelect: (authority: string, lexiconCode: string) => Promise<DownloadAndSelectResult>;
  /** Deletes a local CRDT lexicon; the caller refreshes the lists. */
  deleteLexicon: (lexiconCode: string) => Promise<void>;
  /** The chosen lexicon was stored for the project; the parent tracks it and triggers the banner. */
  onSaved: (name: string, code: string) => void;
  /** Called when a download starts/ends so the parent can lock account controls while it runs. */
  onDownloadingChange?: (downloading: boolean) => void;
}

// Local codes are unique, so a local row's key is its code; remote codes can collide across servers.
function keyFor(project: IProjectModel, local: boolean): string {
  return local ? project.code : `${project.server?.id ?? 'remote'}/${project.code}`;
}

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
  appliedCode,
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
  onDownloadingChange,
}: LexiconPickerProps): ReactElement {
  const [localizedStrings] = useLocalizedStrings(LOCALIZED_STRING_KEYS);

  const [error, setError] = useState('');
  const [pendingKey, setPendingKey] = useState('');
  const [busy, setBusy] = useState<'none' | 'saving' | 'downloading'>('none');
  const [pendingDelete, setPendingDelete] = useState<IProjectModel | undefined>();
  const [deleting, setDeleting] = useState(false);
  const [deleteNotice, setDeleteNotice] = useState('');

  // Let the parent lock the account controls while a download runs: logging out mid-download would
  // abort it and disturb the auth state.
  useEffect(() => {
    onDownloadingChange?.(busy === 'downloading');
  }, [busy, onDownloadingChange]);

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

  const serverGroups = useMemo(() => {
    const byServer = new Map<string, IProjectModel[]>();
    (remoteProjects ?? []).forEach((p) => {
      const name = p.server?.displayName ?? p.server?.id ?? 'remote';
      const list = byServer.get(name);
      if (list) list.push(p);
      else byServer.set(name, [p]);
    });
    return [...byServer].map(([name, list]) => [name, list.sort(byName)] as const);
  }, [remoteProjects]);

  const selectedKey = pendingKey || appliedCode || '';
  const selected = entries.get(selectedKey);

  const messageForFailure = (result: DownloadResult): string => {
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
  };

  const deletability = (project: IProjectModel, local: boolean): boolean => local && !!project.crdt;

  const beginDelete = (project: IProjectModel) => {
    setError('');
    setDeleteNotice('');
    onClearSaved?.();
    setPendingDelete(project);
  };

  const doDelete = () => {
    if (!pendingDelete) return;
    const name = pendingDelete.name || pendingDelete.code;
    const key = keyFor(pendingDelete, true);
    setDeleting(true);
    // eslint-disable-next-line promise/catch-or-return
    deleteLexicon(pendingDelete.code)
      .then(() => {
        setDeleteNotice(
          formatReplacementString(localizedStrings['%lexicon_selectLexicon_deletedStatus%'], {
            name,
          }),
        );
        setPendingKey((prev) => (prev === key ? '' : prev));
        return undefined;
      })
      .catch((e) => {
        logger.error('Error deleting lexicon:', getErrorMessage(e));
        setError(getErrorMessage(e));
      })
      .finally(() => {
        setPendingDelete(undefined);
        setDeleting(false);
      });
  };

  const confirm = () => {
    if (!selected) return;
    const { project, needsDownload } = selected;
    const name = project.name || project.code;
    setError('');
    setDeleteNotice('');

    if (!needsDownload) {
      setBusy('saving');
      // eslint-disable-next-line promise/catch-or-return
      selectLexicon(project.code)
        .then(({ cancelled }) => {
          if (!cancelled) {
            setPendingKey(project.code);
            onSaved(name, project.code);
          }
          return undefined;
        })
        .catch((e) => {
          logger.error(localizedStrings['%lexicon_selectLexicon_saveError%'], getErrorMessage(e));
          setError(getErrorMessage(e));
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
      .then(({ result, success, cancelled, error: failureError }) => {
        if (cancelled) return undefined;
        if (success) {
          // The downloaded lexicon is now a local row, whose key is its code.
          setPendingKey(project.code);
          onSaved(name, project.code);
        }
        // Prefer the backend's own reason (e.g. a sync failure) when it sent one.
        else setError(failureError || messageForFailure(result));
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
  };

  const renderItem = (project: IProjectModel, local: boolean): ReactElement => {
    const key = keyFor(project, local);
    return (
      <LexiconRow
        key={key}
        project={project}
        local={local}
        itemKey={key}
        isChosen={key === selectedKey}
        isApplied={key === appliedCode}
        deletable={deletability(project, local)}
        onSelect={() => {
          setError('');
          setDeleteNotice('');
          onClearSaved?.();
          setPendingKey(key);
        }}
        onBeginDelete={() => beginDelete(project)}
        strings={localizedStrings}
      />
    );
  };

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
    !!selected && !selected.needsDownload && selected.project.code === appliedCode;
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

        <Command className="tw:rounded-md tw:border tw:flex tw:flex-col tw:flex-1 tw:min-h-24 tw:overflow-hidden">
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
          <DeleteConfirm
            project={pendingDelete}
            deleting={deleting}
            onConfirm={doDelete}
            onCancel={() => setPendingDelete(undefined)}
            strings={localizedStrings}
          />
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

            {!!deleteNotice && (
              <p className="tw:text-xs tw:text-muted-foreground tw:shrink-0" role="status">
                {deleteNotice}
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
