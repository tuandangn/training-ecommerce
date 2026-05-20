export type PublicDeliveryNote = {
  id: string;
  code: string;
  orderCode?: string | null;
  status: number;
  deliveryConfirmationStatus: number;
  createdOn: string;
  deliveredOn?: string | null;
  items: PublicDeliveryNoteItem[];
};

export type PublicDeliveryNoteItem = {
  id: string;
  productId: string;
  productName: string;
  quantity: number;
};

export type CustomerSession = {
  sessionId: string;
  customerId: string;
  customerName: string;
  phoneNumber?: string | null;
  email?: string | null;
  hasPassword: boolean;
  expiresOn: string;
};

export type OtpRequestResult = {
  success: boolean;
  message?: string | null;
  challengeId?: string | null;
  maskedDestination?: string | null;
  mockOtp?: string | null;
};

export type ActionResult = {
  success: boolean;
  message?: string | null;
};

export type Dashboard = {
  recentOrders: OrderSummary[];
  recentDeliveryNotes: DeliveryNoteSummary[];
  debtSummary: DebtSummary;
};

export type OrderSummary = {
  id: string;
  code: string;
  status: number;
  totalAmount: number;
  createdOn: string;
  expectedShippingDate?: string | null;
};

export type OrderDetails = OrderSummary & {
  shippingAddress?: string | null;
  note?: string | null;
  items: OrderItem[];
};

export type OrderItem = {
  id: string;
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  subTotal: number;
};

export type DeliveryNoteSummary = {
  id: string;
  code: string;
  orderCode?: string | null;
  status: number;
  deliveryConfirmationStatus: number;
  createdOn: string;
  deliveredOn?: string | null;
};

export type DeliveryNoteDetails = DeliveryNoteSummary & {
  items: DeliveryNoteItem[];
};

export type DeliveryNoteItem = {
  id: string;
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  subTotal: number;
};

export type DebtSummary = {
  totalDebtAmount: number;
  totalPaidAmount: number;
  totalRemainingAmount: number;
  depositBalance: number;
  debts: Debt[];
  recentPayments: Payment[];
};

export type Debt = {
  id: string;
  code: string;
  orderCode?: string | null;
  deliveryNoteCode?: string | null;
  totalAmount: number;
  paidAmount: number;
  remainingAmount: number;
  status: number;
  dueDate?: string | null;
};

export type Payment = {
  id: string;
  code: string;
  amount: number;
  paymentMethod: number;
  paymentType: number;
  paidOn: string;
};

export type PaymentIntent = {
  id: string;
  customerDebtId?: string | null;
  amount: number;
  provider: string;
  providerIntentId?: string | null;
  status: number;
  failureReason?: string | null;
  createdOn: string;
  completedOn?: string | null;
};
