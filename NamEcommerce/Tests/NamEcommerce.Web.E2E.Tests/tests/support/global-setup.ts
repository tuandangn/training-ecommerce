import { chromium, FullConfig } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';
import { LoginPage } from '../pages/LoginPage';

export default async function globalSetup(config: FullConfig) {
  const baseURL = config.projects[0].use.baseURL as string;
  const username = process.env.E2E_USERNAME || 'admin12';
  const password = process.env.E2E_PASSWORD || 'adminadmin';
  const storageStatePath = path.join(__dirname, '..', '.auth', 'admin.json');

  fs.mkdirSync(path.dirname(storageStatePath), { recursive: true });

  const browser = await chromium.launch();
  const page = await browser.newPage({ baseURL });

  const loginPage = new LoginPage(page);
  await loginPage.goto();
  await loginPage.login(username, password);

  await page.context().storageState({ path: storageStatePath });
  await browser.close();
}
