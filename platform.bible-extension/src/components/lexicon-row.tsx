import type { IProjectModel } from 'lexicon';
import { Check, Trash2 } from 'lucide-react';
import {
  Badge,
  CommandItem,
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuTrigger,
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from 'platform-bible-react';
import type { ReactElement } from 'react';

/** Props for one lexicon row. The picker computes identity/selection; this component just draws. */
interface LexiconRowProps {
  project: IProjectModel;
  local: boolean;
  /** The row's unique key (server-or-local + code); also what cmdk filters on. */
  itemKey: string;
  /** The pending pick — shows the check. */
  isChosen: boolean;
  /** The project's applied lexicon — shows the persistent "Current" badge. */
  isApplied: boolean;
  /** When true, right-click offers a delete (any local CRDT lexicon). */
  deletable: boolean;
  onSelect: () => void;
  onBeginDelete: () => void;
  strings: Record<string, string>;
}

/** One row in the lexicon list: check (pending), name/code, "Current"/"FieldWorks" badges. */
export default function LexiconRow({
  project,
  local,
  itemKey,
  isChosen,
  isApplied,
  deletable,
  onSelect,
  onBeginDelete,
  strings,
}: LexiconRowProps): ReactElement {
  const name = project.name || project.code;
  const isFieldWorks = !!(local && project.fwdata && !project.crdt);
  // Case-insensitive so "Happy"/"happy" doesn't show a pointless code line.
  const showCode = project.code.toLowerCase() !== name.toLowerCase();
  const item = (
    <CommandItem
      // cmdk filters on this value; include the key so items stay unique when names collide.
      value={`${name} ${project.code} ${itemKey}`}
      onSelect={onSelect}
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
          <span className="tw:sr-only">{strings['%lexicon_selectLexicon_selectedIndicator%']}</span>
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
              {strings['%lexicon_selectLexicon_badgeCurrent%']}
            </Badge>
          )}
          {isFieldWorks && (
            <Tooltip>
              <TooltipTrigger asChild>
                <Badge className="tw:font-normal tw:text-muted-foreground" variant="outline">
                  {strings['%lexicon_selectLexicon_badgeFieldWorks%']}
                </Badge>
              </TooltipTrigger>
              <TooltipContent>
                {strings['%lexicon_selectLexicon_badgeFieldWorksTip%']}
              </TooltipContent>
            </Tooltip>
          )}
        </div>
      )}
    </CommandItem>
  );
  if (!deletable) return item;
  return (
    <ContextMenu>
      <ContextMenuTrigger asChild>{item}</ContextMenuTrigger>
      <ContextMenuContent>
        <ContextMenuItem onSelect={onBeginDelete}>
          <Trash2 aria-hidden className="tw:h-4 tw:w-4 tw:me-2" />
          {strings['%lexicon_selectLexicon_delete%']}
        </ContextMenuItem>
      </ContextMenuContent>
    </ContextMenu>
  );
}
