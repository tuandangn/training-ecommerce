const MESSAGE_CATALOG: Record<string, string> = {
  "Error.CustomerPortal.OrderRequest.UnknownProducts": "Có sản phẩm không hợp lệ trong yêu cầu đặt hàng.",
  "Error.CustomerPortal.OrderRequest.NotFound": "Không tìm thấy yêu cầu đặt hàng.",
  "Error.CustomerPortal.OrderRequest.NotApproved": "Yêu cầu đặt hàng chưa được cửa hàng duyệt.",
  "Error.CustomerPortal.OrderRequest.NotFullyPriced": "Yêu cầu đặt hàng chưa được báo giá đầy đủ.",
  "Error.CustomerPortal.OrderRequest.CreateOrderFailed": "Không tạo được đơn hàng.",
  "Msg.CustomerPortal.OrderRequest.ConfirmedAndOrderCreated": "Đã xác nhận báo giá và tạo đơn hàng.",

  "Error.DeliveryNoteNotFound": "Không tìm thấy phiếu giao hàng.",
  "Error.DeliveryAcceptance.InvalidItem": "Dòng hàng xác nhận không hợp lệ.",
  "Error.DeliveryAcceptance.NegativeQuantity": "Số lượng xác nhận không hợp lệ.",
  "Error.DeliveryAcceptance.QuantityMismatch": "Số lượng xác nhận không khớp số lượng giao.",
  "Error.DeliveryAcceptance.RejectReasonRequired": "Vui lòng nhập lý do trả hàng cho toàn phiếu.",
  "Msg.CustomerPortal.DeliveryConfirmedWithReturnRequest": "Đã ghi nhận khách đã nhận hàng và tạo yêu cầu trả hàng.",
  "Msg.CustomerPortal.DeliveryConfirmed": "Đã ghi nhận khách đã nhận hàng.",
  "Msg.CustomerPortal.FeedbackSaved": "Đã ghi nhận phản hồi.",

  "Error.CustomerPortal.ReturnRequest.NoItems": "Yêu cầu trả hàng cần có ít nhất một dòng hàng.",
  "Error.CustomerPortal.ReturnRequest.NoDeliveredItems": "Không tìm thấy hàng đã giao có thể trả.",
  "Error.CustomerPortal.ReturnRequest.InvalidItem": "Dòng hàng trả không hợp lệ.",
  "Error.CustomerPortal.ReturnRequest.QuantityExceedsReturnable": "Số lượng trả vượt quá số lượng còn có thể trả.",
  "Error.CustomerPortal.ReturnRequest.NotFound": "Không tìm thấy yêu cầu trả hàng.",
  "Error.CustomerPortal.ReturnRequest.OnlyPendingCanCancel": "Chỉ có thể hủy yêu cầu trả hàng đang chờ xem xét.",
  "Error.CustomerPortal.ReturnRequest.CannotCancelCurrentState": "Không thể hủy yêu cầu trả hàng ở trạng thái hiện tại.",
  "Error.CustomerPortal.ReturnRequest.DeliveryNoteNotDelivered": "Chỉ có thể yêu cầu trả hàng sau khi phiếu giao đã hoàn tất giao hàng.",
  "Error.CustomerPortal.ReturnRequest.InvalidDeliveryNoteForReturn": "Phiếu giao hàng không hợp lệ để trả hàng.",
  "Error.CustomerPortal.ReturnRequest.ProductNotDelivered": "Sản phẩm này chưa có trong các hàng đã giao.",
  "Msg.CustomerPortal.ReturnRequest.Cancelled": "Đã hủy yêu cầu trả hàng.",

  "Error.CustomerPortal.ReturnEvidence.TooManyPictures": "Mỗi dòng hàng chỉ được gửi tối đa 3 ảnh.",
  "Error.CustomerPortal.ReturnEvidence.InvalidMimeType": "Ảnh hiện trạng chỉ nhận JPG, PNG hoặc WEBP.",
  "Error.CustomerPortal.ReturnEvidence.InvalidSize": "Ảnh hiện trạng không hợp lệ hoặc vượt quá 5MB.",
  "Error.CustomerPortal.ReturnEvidence.InvalidBase64": "Dữ liệu ảnh hiện trạng không hợp lệ.",
};

export function resolveApiMessage(message: string | null | undefined) {
  if (!message) return undefined;
  return MESSAGE_CATALOG[message] ?? message;
}
