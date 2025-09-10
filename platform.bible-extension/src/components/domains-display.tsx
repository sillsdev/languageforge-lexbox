// Modified from paranext-core/extensions/src/platform-lexical-tools/src/components/dictionary/domains-display.component.tsx

import { ISemanticDomain } from 'fw-lite-extension';
import { Network } from 'lucide-react';
import { domainText } from '../utils/entry-display-text';

/** Props for the DomainsDisplay component */
type DomainsDisplayProps = {
  /** Domains to display */
  domains: ISemanticDomain[];
  /** Function to trigger when a domain is clicked */
  onClickDomain?: (domain: ISemanticDomain) => void;
};

/**
 * Renders a list of domains for a dictionary entry or sense.
 *
 * The component displays each domain as a rounded, colored pill with a small icon. The text of the
 * pill is the code of the domain, followed by the label.
 */
export default function DomainsDisplay({ domains, onClickDomain }: DomainsDisplayProps) {
  return (
    <div className="tw-mt-2 tw-flex tw-flex-wrap tw-gap-2">
      {domains.map((domain) => (
        <button
          className="tw-rounded tw-bg-accent tw-px-2 tw-py-0.5 tw-text-xs tw-accent-foreground tw-flex tw-items-center tw-gap-1"
          key={domain.code}
          onClick={() => onClickDomain?.(domain)}
          type="button"
        >
          <Network className="tw-inline tw-mr-1 tw-h-3 tw-w-3" />
          <span>{domainText(domain)}</span>
        </button>
      ))}
    </div>
  );
}
