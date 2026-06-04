# Refactor: Common Picture Upload Control

## Vấn đề

3 chỗ upload ảnh trong hệ thống đang dùng 3 approach hoàn toàn khác nhau:

| | Product/Create | GoodsReceipt/Create | DeliveryNote/Confirm |
|---|---|---|---|
| **Upload method** | Client-side base64 | Pre-upload → `/Picture/Upload` | Multipart với form |
| **Form field** | `ImageFile.Base64Data` | `PictureIds[]` (Guid[]) | `pictureFile` (IFormFile) |
| **Max ảnh** | 1 | 2 | 1 |
| **Drag & drop** | Không | Có | Không |
| **jQuery validation** | Không | Không | Có (`data-val-required`) |
| **Code location** | Inline `_UploadPicture.cshtml` | Class `PicturePicker` trong `GoodsReceiptCreateController.js` | Inline trong `_DeliveryNote.ConfirmDelivered.cshtml` |

### Vấn đề cụ thể

- **DeliveryNote** (trigger chính): modal "Xác nhận giao hàng thành công" dùng `<input type="file">` thô — không có preview đẹp, không drag&drop, không đồng bộ với hệ thống
- **Product** dùng base64 → POST body rất lớn (data URL), không scalable
- **GoodsReceipt** có approach đúng nhất nhưng `PicturePicker` bị gắn chặt vào module riêng, không tái sử dụng được

---

## Quyết định kiến trúc

### Standard pattern: Pre-upload → Picture ID

```
User chọn/drop file
    → JS upload ngay lên POST /Picture/Upload
    → Server lưu vào bảng Picture, trả về { id, dataUrl }
    → UI hiện thumbnail + nút xóa
    → Form submit chỉ gửi PictureIds[] (Guid[])
```

**Lý do:**
- Ảnh được lưu trước submit → không mất khi form validation fail
- Form body nhẹ (chỉ Guid[])
- `/Picture/Upload` endpoint và bảng `Picture` đã có sẵn
- GoodsReceipt đang dùng pattern này ổn định

### jQuery unobtrusive validation (client + server)

**Client-side:** Hidden sentinel input:
```html
<input type="hidden" name="{fieldName}-count"
       data-val="true"
       data-val-required="Vui lòng tải lên ít nhất 1 hình"
       value="" />
<!-- JS set value="1" khi upload xong, set value="" khi xóa hết -->
```

**Server-side:** Partial render `ModelState` errors vào `<span data-valmsg-for>`:
```html
<span class="text-danger small field-validation-valid"
      data-valmsg-for="{fieldName}"
      data-valmsg-replace="true"></span>
```
Controller action add `ModelState.AddModelError("{fieldName}", "...")` khi PictureIds rỗng mà Required = true.

---

## Confirmed answers

| Câu hỏi | Trả lời |
|---|---|
| DeliveryNote backend lưu ảnh ở đâu? | Bảng `Picture` (giống GoodsReceipt) |
| Product migrate hay giữ base64? | **Migrate** sang Picture ID |
| DeliveryNote max ảnh? | **Tùy chọn** — nhiều ảnh bằng chứng, cấu hình qua `MaxFiles` |
| Validation scope? | **Cả client lẫn server** |

---

## Kế hoạch thực hiện

### Phase 1 — JS module `image-uploader.js`

**File mới:** `wwwroot/modules/image-uploader.js`

Standalone ES class, không phụ thuộc framework:

```js
export class ImageUploader {
    // container: HTMLElement
    // fieldName: string — tên field submit (PictureIds)
    // maxFiles: number
    // required: boolean
    // uploadUrl: string — mặc định '/Picture/Upload'
    // existingIds: string[] — Guid[] để pre-fill khi load lại form
    constructor({ container, fieldName, maxFiles, required, uploadUrl, existingIds })
    init()      // render UI, attach events, load existing pictures
    destroy()   // cleanup listeners (quan trọng với Bootstrap modal reuse)
    getCount()  // số ảnh hiện tại
}
```

Khởi tạo qua `data-*` (zero-config, không cần JS thêm):
```html
<div data-image-uploader
     data-field-name="PictureIds"
     data-max-files="5"
     data-required="true"
     data-existing-ids="guid1,guid2">
</div>
```

Features:
- Upload ngay khi chọn/drop file
- Progress spinner per slot trong khi upload
- Thumbnail + nút xóa sau upload thành công
- Tự render thêm/bớt slot theo `maxFiles`
- Update hidden validation sentinel (client-side validation)
- Drag & drop support
- Re-init safe (Bootstrap modal reuse — gọi `destroy()` khi modal hide)
- CSRF token trong request header

### Phase 2 — Razor partial `_ImageUploader.cshtml`

**File mới:** `Views/Shared/_ImageUploader.cshtml`

**Model mới:** `Models/Common/ImageUploaderModel.cs`
```csharp
public class ImageUploaderModel {
    public string FieldName { get; set; } = "PictureIds";
    public string? Label { get; set; }
    public string? HintText { get; set; }
    public int MaxFiles { get; set; } = 1;
    public bool Required { get; set; } = false;
    public string RequiredMessage { get; set; } = "Vui lòng tải lên ít nhất 1 hình ảnh.";
    public IList<Guid> ExistingPictureIds { get; set; } = [];
}
```

HTML structure:
```
[Label + hint text]
[div.img-uploader  data-image-uploader ...]   ← JS target
  [div.img-uploader__thumbnails]              ← thumbnails (JS inject)
  [div.img-uploader__slots]                  ← dropzone slots (JS inject)
[hidden PictureIds[] per uploaded image]      ← JS inject khi upload xong
[hidden validation sentinel]                  ← data-val-required nếu Required=true
[span data-valmsg-for="{FieldName}"]         ← server + client error message
```

CSS prefix `.img-uploader__*` → thêm vào `site.css` hoặc file riêng.

Cách dùng:
```razor
@await Html.PartialAsync("_ImageUploader", new ImageUploaderModel {
    FieldName = "PictureIds",
    Label = "Hình ảnh bằng chứng",
    MaxFiles = 5,
    Required = true,
    ExistingPictureIds = Model.PictureIds
})
```

### Phase 3 — Migrate GoodsReceipt/Create *(ít risk nhất — validation pattern)*

**Backend:** Không thay đổi (đã dùng `PictureIds[]`).

**Frontend:**
- Xóa class `PicturePicker` khỏi `GoodsReceiptCreateController.js`
- `Views/GoodsReceipt/Create.cshtml`: thay HTML cũ bằng `_ImageUploader` partial (`MaxFiles=2`)
- Module tự init qua `data-image-uploader`

**Verify:** Form submit vẫn gửi `PictureIds[]` đúng; build xanh.

### Phase 4 — Migrate DeliveryNote/Details *(mục tiêu chính)*

**Backend changes:**
- `MarkDeliveredCommand`: đổi `IFormFile PictureFile` → `IList<Guid> PictureIds`
- `DeliveryNoteController.MarkDelivered`:
  - Bỏ đọc `IFormFile`
  - Thêm `ModelState.AddModelError("PictureIds", "...")` nếu `PictureIds` rỗng và required
  - Bỏ `enctype="multipart/form-data"` không còn cần thiết
- Handler/AppService: nhận list picture IDs, lưu vào bảng liên kết (tương tự GoodsReceipt)

**Frontend:**
- `Views/Shared/_DeliveryNote.ConfirmDelivered.cshtml`:
  - Xóa `<input type="file" id="deliveryProofPicture">` + `#previewContainer`
  - Thêm `_ImageUploader` partial (`FieldName="PictureIds"`, `Required=true`, `MaxFiles` tùy chọn)
- `DeliveryNoteController.js`: bỏ FileReader preview handler

### Phase 5 — Migrate Product/Create *(backend refactor lớn nhất)*

**Backend changes:**
- `CreateProductModel`: đổi `Base64ImageModel ImageFile` → `Guid? PictureId`
- `CreateProductCommand`: tương tự
- `ProductAppService.CreateAsync`: dùng picture ID thay vì convert base64 → lưu vào bảng liên kết giữa Product và Picture
- Xóa `Base64ImageModel` nếu không còn nơi nào dùng

**Frontend:**
- Xóa `Views/Product/_UploadPicture.cshtml`
- `Views/Product/Create.cshtml` + `Edit.cshtml`: dùng `_ImageUploader` partial (`MaxFiles=1`)

---

## Thứ tự implement

```
Phase 1: image-uploader.js      (nền tảng JS)
Phase 2: _ImageUploader.cshtml  (nền tảng Razor + Model)
Phase 3: GoodsReceipt/Create    (validate pattern, ít risk)
Phase 4: DeliveryNote/Details   (mục tiêu chính)
Phase 5: Product/Create         (backend refactor lớn hơn)
```

---

## Files sẽ thay đổi

### Tạo mới
- `wwwroot/modules/image-uploader.js`
- `Views/Shared/_ImageUploader.cshtml`
- `Models/Common/ImageUploaderModel.cs`

### Xóa
- `Views/Product/_UploadPicture.cshtml`
- `Models/Common/Base64ImageModel.cs` *(nếu không còn nơi nào dùng)*

### Sửa
- `wwwroot/modules/GoodsReceiptCreateController.js` — xóa `PicturePicker`
- `Views/GoodsReceipt/Create.cshtml`
- `Views/Shared/_DeliveryNote.ConfirmDelivered.cshtml`
- `wwwroot/modules/DeliveryNoteController.js`
- `Views/Product/Create.cshtml` (và `Edit.cshtml` nếu có)
- `Models/Catalog/CreateProductModel.cs`
- `Commands/Products/CreateProductCommand.cs`
- `Application/Products/ProductAppService.cs`
- `Commands/DeliveryNotes/MarkDeliveredCommand.cs`
- `Controllers/DeliveryNoteController.cs`
- `site.css` — thêm `.img-uploader__*` styles
