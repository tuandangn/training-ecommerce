import { APIRequestContext, APIResponse, expect } from '@playwright/test';

export type OrderWorkflowSeed = {
  scenarioId: string;
  quantity: number;
  customerName: string;
  customerPhone: string;
  shippingAddress: string;
  vendorName: string;
  warehouseName: string;
  productName: string;
  unitPrice: number;
  unitCost: number;
};

export type OrderWorkflowState = {
  scenarioId: string;
  orderCode?: string;
  purchaseOrderCode?: string;
  deliveryNoteCode?: string;
  orderStatus: string;
  purchaseOrderStatus: string;
  deliveryStatus: string;
  orderedQuantity: number;
  receivedQuantity: number;
  deliveredQuantity: number;
};

export class E2EClient {
  private readonly token = process.env.E2E_TOKEN || 'local-e2e-token';

  constructor(private readonly request: APIRequestContext) {}

  async reset(scenarioId: string) {
    const response = await this.request.post('/__e2e/reset', {
      headers: this.headers(),
      data: { scenarioId },
    });
    await this.expectOk(response, 'reset E2E data');
  }

  async seedOrderWorkflow(scenarioId: string, quantity: number, directShip: boolean): Promise<OrderWorkflowSeed> {
    const response = await this.request.post('/__e2e/seed/order-workflow', {
      headers: this.headers(),
      data: { scenarioId, quantity, directShip },
    });
    await this.expectOk(response, 'seed order workflow');
    return await response.json();
  }

  async getOrderWorkflowState(scenarioId: string): Promise<OrderWorkflowState> {
    const response = await this.request.get(`/__e2e/state/order-workflow/${scenarioId}`, {
      headers: this.headers(),
    });
    await this.expectOk(response, 'read order workflow state');
    return await response.json();
  }

  private headers() {
    return { 'X-E2E-Token': this.token };
  }

  private async expectOk(response: APIResponse, action: string) {
    const body = await response.text();
    expect(response.ok(), `${action} failed with HTTP ${response.status()}: ${body}`).toBeTruthy();
  }
}
