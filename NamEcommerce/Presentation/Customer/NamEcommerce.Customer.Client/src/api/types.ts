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
  requiresOtp: boolean;
  challengeId?: string | null;
  maskedDestination?: string | null;
  mockOtp?: string | null;
  session?: CustomerSession | null;
};

export type CustomerLocation = {
  latitude: number;
  longitude: number;
  accuracyMeters?: number | null;
  capturedOnUtc: string;
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

export type OrderRequestList = {
  items: OrderRequestSummary[];
};

export type OrderRequestSummary = {
  id: string;
  code: string;
  status: number;
  totalAmount?: number | null;
  createdOn: string;
  expectedShippingDate?: string | null;
  reviewedOn?: string | null;
  convertedOrderId?: string | null;
  canConfirm: boolean;
};

export type OrderRequestDetails = OrderRequestSummary & {
  shippingAddress?: string | null;
  note?: string | null;
  adminNote?: string | null;
  items: OrderRequestItem[];
};

export type OrderRequestItem = {
  id: string;
  productId: string;
  productName: string;
  quantity: number;
  unitPrice?: number | null;
  subTotal?: number | null;
  isPriced: boolean;
};

export type ConversionResult = {
  success: boolean;
  message?: string | null;
  createdId?: string | null;
};

export type ProductList = {
  items: ProductPickerItem[];
  hasMore: boolean;
  pageSize: number;
};

export type ProductPickerItem = {
  id: string;
  name: string;
  categoryId?: string | null;
  categoryName?: string | null;
  pictureUrl?: string | null;
  unitPrice?: number | null;
  hasPurchased: boolean;
};

export type ProductCategoryList = {
  items: ProductCategory[];
};

export type ProductCategory = {
  id: string;
  name: string;
  parentId?: string | null;
};

export type OrderRequestDefaults = {
  shippingAddress?: string | null;
  shippingAddressSource?: string | null;
};

export type ContactInfo = {
  store: StoreContact;
  warehouses: WarehouseContact[];
};

export type StoreContact = {
  storeName: string;
  phoneNumber?: string | null;
  address?: string | null;
  email?: string | null;
  mapQuery?: string | null;
};

export type WarehouseContact = {
  id: string;
  name: string;
  phoneNumber?: string | null;
  address?: string | null;
  mapQuery?: string | null;
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
  reservedReturnQuantity: number;
  pendingPortalReturnQuantity: number;
  returnableQuantity: number;
};

export type ReturnRequestList = {
  items: ReturnRequestSummary[];
};

export type ReturnableItemList = {
  items: ReturnableItem[];
};

export type ReturnableItem = {
  productId: string;
  productName: string;
  unit: string;
  deliveredQuantity: number;
  reservedReturnQuantity: number;
  returnableQuantity: number;
  latestUnitPrice: number;
};

export type ReturnRequestSummary = {
  id: string;
  deliveryNoteId: string;
  deliveryNoteCode?: string | null;
  status: number;
  reason?: string | null;
  compensateInNextDelivery: boolean;
  adminNote?: string | null;
  createdOn: string;
  reviewedOn?: string | null;
  convertedCustomerReturnId?: string | null;
  totalRequestedQuantity: number;
  itemCount: number;
};

export type ReturnRequestDetails = ReturnRequestSummary & {
  items: ReturnRequestItem[];
};

export type ReturnRequestItem = {
  id: string;
  deliveryNoteItemId: string;
  productId: string;
  productName: string;
  requestedQuantity: number;
  reason?: string | null;
  evidencePictures: ReturnRequestEvidencePicture[];
};

export type ReturnRequestEvidencePicture = {
  pictureId: string;
  pictureUrl?: string | null;
  fileName?: string | null;
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
