# Refactor to Common Picture Upload Control

## Vấn đề

3 chỗ upload ảnh trong hệ thống đang dùng 3 approach hoàn toàn khác nhau:

| | Product/Create | GoodsReceipt/Create | DeliveryNote/Confirm |
|---|---|---|---|
| **Upload method** | Client-side base64 | Pre-upload → `/Picture/Upload` | Multipart với form |
| **Form field** | `ImageFile.Base64Data` | `PictureIds[]` (Guid[]) | `pictureFile` (IFormFile) |
| **Max ảnh** | 1 | 2 | 1 |
| **Drag & drop** | Không | Có | Không |
| **jQuery validation** | Không | Không | Có (`data-val-required`) |
| **Code location** | Inline trong `_UploadPicture.cshtml` | Embedded trong `GoodsReceiptCreateController.js` (class `PicturePicker`) | Inline trong `_DeliveryNote.ConfirmDelivered.cshtml` |

### Vấn đề cụ thể

- **DeliveryNote** (trigger chính): modal "Xác nhận giao hàng thành công" dùng `<input type="file">` thô — trông khác hẳn các trang khác, không có preview đẹp, không drag&drop
- **Product** dùng base64 → POST body rất lớn (data URL), không scalable
- **GoodsReceipt** có approach đúng nhất nhưng class `PicturePicker` bị gắn chặt vào file module riêng, không tái sử dụng được

---

## Quyết định kiến trúc

**Standard pattern: Pre-upload → Picture ID**

```
User chọn file
    → JS upload ngay lên POST /Picture/Upload
    → Server lưu file, trả về { id, dataUrl }
    → UI hiện thumbnail + nút xóa
    → Form submit chỉ gửi PictureIds[] (Guid[])
```

Lý do:
- Ảnh lưu trước submit → không mất khi form validation fail
- Form body nhẹ (chỉ Guid[])
- `/Picture/Upload` endpoint đã có sẵn
- GoodsReceipt đã dùng pattern này ổn định

**jQuery unobtrusive validation:**
```html
<!-- Hidden sentinel — JS cập nhật khi upload/xóa -->
<input type="hidden" name="pictureIds-count"
       data-val="true" data-val-required="Vui lòng tải lên ít nhất 1 hình" value="" />
```

---

## Kế hoạch thực hiện

### Phase 1 — JS module `image-uploader.js`

**File:** `wwwroot/modules/image-uploader.js`

Standalone ES class, không phụ thuộc framework:

```js
export class ImageUploader {
    constructor({ container, maxFiles, fieldName, required, uploadUrl, onCountChange })
    init()      // init từ DOM, load existing pictures
    destroy()   // cleanup listeners (quan trọng với Bootstrap modal)
}
```

Khởi tạo qua `data-*` attributes (zero-config):
```html
<div data-image-uploader
     data-field-name="PictureIds"
     data-max-files="3"
     data-required="true">
</div>
```

Features:
- Upload ngay khi chọn/drop file
- Progress spinner per slot
- Thumbnail + nút xóa
- Auto render thêm/bớt slot theo `maxFiles`
- Cập nhật hidden validation sentinel input
- Drag & drop support
- Re-init safe (Bootstrap modal reuse)

### Phase 2 — Razor partial `_ImageUploader.cshtml`

**File:** `Views/Shared/_ImageUploader.cshtml`

Model class `ImageUploaderModel`:
```csharp
public class ImageUploaderModel {
    public string FieldName { get; set; } = "PictureIds";
    public string? Label { get; set; }
    public string? HintText { get; set; }
    public int MaxFiles { get; set; } = 1;
    public bool Required { get; set; } = false;
    public string? RequiredMessage { get; set; }
    public IList<Guid> ExistingPictureIds { get; set; } = [];
}
```

HTML structure partial render:
```
[Label + hint text]
[div.img-uploader-thumbnails]   ← ảnh đã upload (thumbnail + nút xóa)
[div.img-uploader-slots]        ← dropzone slots
[hidden PictureIds per image]   ← được JS inject khi upload thành công
[hidden validation sentinel]    ← data-val-required nếu Required=true
[span validation message]
```

CSS prefix: `.img-uploader-*` → thêm vào `site.css`

### Phase 3 — Migrate GoodsReceipt/Create *(ít risk nhất)*

- Xóa class `PicturePicker` khỏi `GoodsReceiptCreateController.js`
- `Views/GoodsReceipt/Create.cshtml`: thay HTML cũ bằng `@await Html.PartialAsync("_ImageUploader", new ImageUploaderModel { MaxFiles = 2, FieldName = "PictureIds", ... })`
- Không cần thay đổi backend

### Phase 4 — Migrate DeliveryNote/Details *(mục tiêu chính)*

**Backend:**
- `MarkDeliveredCommand`: đổi `IFormFile PictureFile` → `Guid? PictureId`
- `DeliveryNoteController.MarkDelivered`: bỏ đọc `IFormFile`, nhận `PictureId` từ form field
- Bỏ `enctype="multipart/form-data"` khỏi form (không còn cần)
- Handler/service xử lý picture ID thay vì file stream

**Frontend:**
- `Views/Shared/_DeliveryNote.ConfirmDelivered.cshtml`: xóa `<input type="file">` + preview inline
- Thay bằng `_ImageUploader` partial: `MaxFiles=1, Required=true`

### Phase 5 — Migrate Product/Create *(backend refactor lớn nhất)*

**Backend:**
- `CreateProductModel`: đổi `Base64ImageModel ImageFile` → `Guid? PictureId`
- `CreateProductCommand`: tương tự
- `ProductAppService.CreateAsync`: dùng picture ID thay vì convert base64

**Frontend:**
- Xóa `Views/Product/_UploadPicture.cshtml`
- `Views/Product/Create.cshtml`: dùng `_ImageUploader` partial

---

## Thứ tự implement

```
Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5
  (JS)      (Razor)   (GR)      (DN)      (Product)
```

---

## Câu hỏi cần confirm

1. **DeliveryNote**: `MarkDelivered` handler xử lý `IFormFile` ở đâu — lưu vào Picture table hay local disk?
2. **Product**: migrate sang picture ID hay giữ base64? (Nếu giữ base64 thì Product sẽ không dùng pre-upload)
3. **DeliveryNote max ảnh**: vẫn là 1 hay cho phép nhiều hơn (3-5 ảnh chứng minh giao hàng)?
4. **Validation**: cần hỗ trợ server-side ModelState error hiển thị trong control không?
