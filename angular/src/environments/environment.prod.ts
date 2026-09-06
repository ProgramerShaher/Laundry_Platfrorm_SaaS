import { Environment } from '@abp/ng.core';

const baseUrl = 'http://localhost:4200';

export const environment = {
  production: true,
  application: {
    baseUrl,
    name: 'laundry_SaaS',
    logoUrl: '',
  },
  oAuthConfig: {
    issuer: 'https://localhost:44312/',
    redirectUri: baseUrl,
    clientId: 'laundry_SaaS_App',
    responseType: 'code',
    scope: 'offline_access laundry_SaaS',
    requireHttps: true
  },
  apis: {
    default: {
      url: 'https://localhost:44312',
      rootNamespace: 'laundry_SaaS',
    },
  },
} as Environment;
