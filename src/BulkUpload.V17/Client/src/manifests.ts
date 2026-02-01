/**
 * Extension manifests for Bulk Upload
 * Registers section and section view with Umbraco 17
 */

import type { ManifestSection, ManifestSectionView } from '@umbraco-cms/backoffice/extension-registry';

const sectionManifest: ManifestSection = {
  type: 'section',
  alias: 'BulkUpload.Section',
  name: 'Bulk Upload',
  meta: {
    label: 'Bulk Upload',
    pathname: 'bulk-upload'
  }
};

const sectionViewManifest: ManifestSectionView = {
  type: 'sectionView',
  alias: 'BulkUpload.SectionView',
  name: 'Bulk Upload Dashboard',
  element: () => import('./components/bulk-upload-dashboard.element.js'),
  weight: -10,
  meta: {
    label: 'Bulk Upload',
    pathname: 'overview',
    icon: 'icon-cloud-upload'
  },
  conditions: [
    {
      alias: 'Umb.Condition.SectionAlias',
      match: 'BulkUpload.Section'
    }
  ]
};

export const manifests = [sectionManifest, sectionViewManifest];
