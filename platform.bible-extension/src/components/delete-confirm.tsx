import type { IProjectModel } from 'lexicon';
import { Alert, AlertDescription, Button, Spinner } from 'platform-bible-react';
import type { ReactElement } from 'react';
import { formatReplacementString } from 'platform-bible-utils';

/** Props for the delete-a-local-copy confirmation. */
interface DeleteConfirmProps {
  project: IProjectModel;
  deleting: boolean;
  onConfirm: () => void;
  onCancel: () => void;
  strings: Record<string, string>;
}

/** Inline confirm for deleting a downloaded lexicon's local copy. */
export default function DeleteConfirm({
  project,
  deleting,
  onConfirm,
  onCancel,
  strings,
}: DeleteConfirmProps): ReactElement {
  return (
    <Alert
      className="tw:shrink-0"
      onKeyDown={(e) => {
        if (e.key === 'Escape') onCancel();
      }}
    >
      <AlertDescription>
        {formatReplacementString(strings['%lexicon_selectLexicon_deleteConfirm%'], {
          name: project.name || project.code,
          server: project.server?.displayName ?? '',
        })}
      </AlertDescription>
      <div className="tw:mt-2 tw:flex tw:gap-2">
        <Button disabled={deleting} onClick={onConfirm} type="button" variant="destructive">
          {deleting && <Spinner className="tw:h-4 tw:w-4 tw:me-2" />}
          {strings['%lexicon_selectLexicon_deleteConfirmAction%']}
        </Button>
        <Button
          // Focus the safe choice; Esc (handled above) also cancels.
          // eslint-disable-next-line jsx-a11y/no-autofocus
          autoFocus
          disabled={deleting}
          onClick={onCancel}
          type="button"
          variant="outline"
        >
          {strings['%lexicon_button_cancel%']}
        </Button>
      </div>
    </Alert>
  );
}
