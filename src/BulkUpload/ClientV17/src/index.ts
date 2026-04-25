/**
 * Bulk Upload for Umbraco 17
 * Entry point for API auth wiring. Section + dashboard are declared in umbraco-package.json.
 */

import { UmbEntryPointOnInit } from '@umbraco-cms/backoffice/extension-api';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';
import { apiContext } from './api/api-context.js';

export const onInit: UmbEntryPointOnInit = (host) => {
  host.consumeContext(UMB_AUTH_CONTEXT, async (authContext) => {
    if (!authContext) return;

    const config = authContext.getOpenApiConfiguration();

    apiContext.setConfig({
      baseUrl: config.base,
      token: config.token,
      credentials: config.credentials,
    });
  });
};

export * from './components/bulk-upload-dashboard.element.js';
