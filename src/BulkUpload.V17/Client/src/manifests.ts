/**
 * Extension manifests for Bulk Upload
 * Registers section and dashboard with Umbraco 17
 */

import type { ManifestDashboard, ManifestSection } from '@umbraco-cms/backoffice/extension-registry';

const sectionManifest: ManifestSection = {
  type: 'section',
  alias: 'BulkUpload.Section',
  name: 'Bulk Upload',
  meta: {
    label: 'Bulk Upload',
    pathname: 'bulk-upload'
  }
};

const dashboardManifest: ManifestDashboard = {
  type: 'dashboard',
  alias: 'BulkUpload.Dashboard',
  name: 'Bulk Upload Dashboard',
  element: () => import('./components/bulk-upload-dashboard.element.js'),
  weight: -10,
  meta: {
    label: 'Bulk Upload',
    pathname: 'bulk-upload-dashboard'
  },
  conditions: [
    {
      alias: 'Umb.Condition.SectionAlias',
      match: 'BulkUpload.Section'
    }
  ]
};

export const manifests = [sectionManifest, dashboardManifest];
