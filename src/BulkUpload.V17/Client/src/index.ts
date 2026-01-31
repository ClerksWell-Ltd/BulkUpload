/**
 * Bulk Upload for Umbraco 17
 * Entry point for extension registration
 */

import { manifests } from './manifests.js';
import { UmbEntryPointOnInit } from '@umbraco-cms/backoffice/extension-api';

export const onInit: UmbEntryPointOnInit = (host, extensionRegistry) => {
  extensionRegistry.registerMany(manifests);
};

export * from './components/bulk-upload-dashboard.element.js';
