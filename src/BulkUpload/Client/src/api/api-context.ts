/**
 * API Context for Umbraco authentication
 */

export interface ApiConfig {
  baseUrl: string;
  token: () => Promise<string>;
  credentials?: RequestCredentials;
}

class ApiContextManager {
  private config: ApiConfig | null = null;

  setConfig(config: ApiConfig): void {
    this.config = config;
  }

  getConfig(): ApiConfig | null {
    return this.config;
  }

  async getAuthHeaders(): Promise<HeadersInit> {
    if (!this.config) {
      return {};
    }

    const token = await this.config.token();
    return {
      'Authorization': `Bearer ${token}`
    };
  }

  getBaseUrl(): string {
    return this.config?.baseUrl || '';
  }

  getCredentials(): RequestCredentials | undefined {
    return this.config?.credentials;
  }
}

export const apiContext = new ApiContextManager();
