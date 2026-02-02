/**
 * Bulk Upload for Umbraco 17
 * Entry point for extension registration
 */

import { manifests } from './manifests.js';
import { UmbEntryPointOnInit } from '@umbraco-cms/backoffice/extension-api';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';
import { apiContext } from './api/api-context.js';

export const onInit: UmbEntryPointOnInit = (host, extensionRegistry) => {
  // Set up API authentication context
  host.consumeContext(UMB_AUTH_CONTEXT, async (authContext) => {
    if (!authContext) return;

    const config = authContext.getOpenApiConfiguration();

    apiContext.setConfig({
      baseUrl: config.base,
      token: config.token,
      credentials: config.credentials,
    });
  });

  // Register manifests
  extensionRegistry.registerMany(manifests);
};

export * from './components/bulk-upload-dashboard.element.js';
