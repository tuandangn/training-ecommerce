import { expect } from '@playwright/test';
import { E2EClient } from '../support/e2e-client';

type InventoryState = {
  stockAvailableQuantity: number;
  stockOnHandQuantity: number;
  stockReservedQuantity: number;
  globalReservedQuantity: number;
};

export class StockInventoryState {
  constructor(private readonly client: E2EClient) {
  }

  async checkInventoryState(scenarioId: string, matchingProps: Partial<InventoryState>) {
    await expect.poll(async () => await this.client.getCurrentInventoryState(scenarioId)).toMatchObject(matchingProps);
  }
}