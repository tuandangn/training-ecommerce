import { FormEvent, useEffect, useState } from "react";
import { apiFetch } from "../api/client";
import type { OrderRequestDefaults, ProductCategory, ProductCategoryList, ProductList, ProductPickerItem } from "../api/types";
import { money } from "../app/format";
import { navigate } from "../app/routes";

type DraftItem = {
  product: ProductPickerItem;
  quantity: number;
};

export function NewOrderRequestPage() {
  const [shippingAddress, setShippingAddress] = useState("");
  const [shippingAddressSource, setShippingAddressSource] = useState("");
  const [note, setNote] = useState("");
  const [categories, setCategories] = useState<ProductCategory[]>([]);
  const [selectedCategoryId, setSelectedCategoryId] = useState("");
  const [products, setProducts] = useState<ProductPickerItem[]>([]);
  const [keywords, setKeywords] = useState("");
  const [items, setItems] = useState<DraftItem[]>([]);
  const [message, setMessage] = useState("");
  const [productError, setProductError] = useState("");
  const [hasMoreProducts, setHasMoreProducts] = useState(false);
  const [productPageSize, setProductPageSize] = useState(30);
  const [loadingProducts, setLoadingProducts] = useState(true);

  useEffect(() => {
    void loadInitialData();
  }, []);

  async function loadInitialData() {
    setLoadingProducts(true);
    setProductError("");

    const [categoryResult, defaultResult, productResult] = await Promise.allSettled([
      apiFetch<ProductCategoryList>("/api/products/categories"),
      apiFetch<OrderRequestDefaults>("/api/order-requests/defaults"),
      apiFetch<ProductList>("/api/products?pageSize=40&purchasedOnly=true"),
    ]);

    if (categoryResult.status === "fulfilled") {
      setCategories(categoryResult.value.items);
    }

    if (defaultResult.status === "fulfilled") {
      setShippingAddress((current) => current || defaultResult.value.shippingAddress || "");
      setShippingAddressSource(defaultResult.value.shippingAddressSource ?? "");
    }

    if (productResult.status === "fulfilled") {
      setProducts(productResult.value.items);
      setHasMoreProducts(productResult.value.hasMore);
      setProductPageSize(productResult.value.pageSize);
    } else {
      setProducts([]);
      setProductError("Không thể tải danh sách hàng hóa.");
    }

    setLoadingProducts(false);
  }

  async function loadProducts(search: string, categoryId = selectedCategoryId) {
    setLoadingProducts(true);
    setProductError("");
    try {
      const query = new URLSearchParams({ pageSize: "40" });
      query.set("purchasedOnly", categoryId ? "false" : "true");
      if (categoryId) query.set("categoryId", categoryId);
      if (search.trim()) query.set("keywords", search.trim());
      const result = await apiFetch<ProductList>(`/api/products?${query.toString()}`);
      setProducts(result.items);
      setHasMoreProducts(result.hasMore);
      setProductPageSize(result.pageSize);
    } catch {
      setProducts([]);
      setProductError("Không thể tải danh sách hàng hóa.");
    } finally {
      setLoadingProducts(false);
    }
  }

  function selectCategory(categoryId: string) {
    setSelectedCategoryId(categoryId);
    void loadProducts(keywords, categoryId);
  }

  function addProduct(product: ProductPickerItem) {
    setItems((current) => {
      const existing = current.find((item) => item.product.id === product.id);
      if (existing) {
        return current.map((item) => (item.product.id === product.id ? { ...item, quantity: item.quantity + 1 } : item));
      }

      return [...current, { product, quantity: 1 }];
    });
  }

  function updateQuantity(productId: string, quantity: number) {
    setItems((current) =>
      current.map((item) => (item.product.id === productId ? { ...item, quantity } : item))
    );
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    const validItems = items.filter((item) => item.quantity > 0);
    if (validItems.length === 0) {
      setMessage("Vui lòng chọn hàng hóa cần đặt.");
      return;
    }

    try {
      await apiFetch("/api/order-requests", {
        method: "POST",
        body: JSON.stringify({
          shippingAddress,
          note,
          items: validItems.map((item) => ({
            productId: item.product.id,
            quantity: item.quantity,
          })),
        }),
      });
      setItems([]);
      setMessage("Đã gửi yêu cầu đặt hàng thành công. Cửa hàng sẽ kiểm tra giá và duyệt trước khi xử lý.");
    } catch {
      setMessage("Không thể gửi yêu cầu đặt hàng.");
    }
  }

  return (
    <section className="stack">
      <div className="toolbar">
        <div>
          <h1 className="page-title">Đặt hàng</h1>
          <p className="page-subtitle">Chờ duyệt trước khi xử lý</p>
        </div>
        <button className="button" onClick={() => navigate("/orders")}>
          Danh sách đơn
        </button>
      </div>
      {message && <div className={message.includes("Không") || message.includes("Vui lòng") ? "notice" : "badge"}>{message}</div>}
      <form className="order-request-layout" onSubmit={submit}>
        <section className="card stack">
          <div className="field">
            <label>Địa chỉ giao</label>
            <input value={shippingAddress} onChange={(event) => setShippingAddress(event.target.value)} />
            {shippingAddressSource && <div className="muted-text">Tự điền từ {shippingAddressSource.toLowerCase()}.</div>}
          </div>
          <div className="field">
            <label>Ghi chú</label>
            <textarea value={note} onChange={(event) => setNote(event.target.value)} />
          </div>
          <div className="toolbar">
            <div>
              <div className="metric-label">Giỏ hàng</div>
              <strong>{items.length} mặt hàng</strong>
            </div>
            <button className="button primary" type="submit">
              Gửi yêu cầu
            </button>
          </div>
          <table className="table">
            <thead>
              <tr>
                <th>Hàng hóa</th>
                <th>Số lượng</th>
                <th>Đơn giá</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {items.length === 0 && (
                <tr>
                  <td colSpan={4}>Chưa chọn hàng hóa.</td>
                </tr>
              )}
              {items.map((item) => (
                <tr key={item.product.id}>
                  <td>
                    <strong>{item.product.name}</strong>
                    <div className="muted-text">{item.product.categoryName ?? "Chưa phân loại"}</div>
                  </td>
                  <td>
                    <input
                      className="quantity-input"
                      min="0"
                      step="0.01"
                      type="number"
                      value={item.quantity}
                      onChange={(event) => updateQuantity(item.product.id, Number(event.target.value))}
                    />
                  </td>
                  <td>{productPriceText(item.product)}</td>
                  <td>
                    <button
                      className="button"
                      type="button"
                      onClick={() => setItems((current) => current.filter((cartItem) => cartItem.product.id !== item.product.id))}
                    >
                      Xóa
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>

        <section className="card stack">
          <div className="toolbar">
            <div>
              <h2 className="page-title">Hàng hóa</h2>
              <p className="page-subtitle">Mặc định hiển thị hàng đã mua, chọn danh mục để tìm thêm hàng hóa</p>
            </div>
          </div>
          <div className="category-panel">
            <button
              className={selectedCategoryId === "" ? "category-chip active" : "category-chip"}
              type="button"
              onClick={() => selectCategory("")}
            >
              Đã mua
            </button>
            {categories.map((category) => (
              <button
                className={selectedCategoryId === category.id ? "category-chip active" : "category-chip"}
                key={category.id}
                type="button"
                onClick={() => selectCategory(category.id)}
              >
                {category.name}
              </button>
            ))}
          </div>
          <div className="product-search">
            <input
              placeholder="Lọc theo tên hàng hóa"
              value={keywords}
              onChange={(event) => setKeywords(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === "Enter") {
                  event.preventDefault();
                  void loadProducts(keywords);
                }
              }}
            />
            <button className="button" type="button" onClick={() => loadProducts(keywords)}>
              Tìm
            </button>
          </div>
          {loadingProducts && <div>Đang tải hàng hóa...</div>}
          {productError && <div className="notice">{productError}</div>}
          {!loadingProducts && !productError && hasMoreProducts && (
            <div className="badge">Đang hiển thị {productPageSize} hàng hóa đầu tiên, nhập thêm từ khóa để lọc hẹp.</div>
          )}
          {!loadingProducts && !productError && products.length === 0 && (
            <div className="notice">Chưa có hàng hóa phù hợp. Vui lòng liên hệ cửa hàng nếu cần đặt hàng ngay.</div>
          )}
          <div className="product-grid">
            {products.map((product) => (
              <ProductCard key={product.id} product={product} onPick={() => addProduct(product)} />
            ))}
          </div>
        </section>
      </form>
    </section>
  );
}

function ProductCard({ product, onPick }: { product: ProductPickerItem; onPick: () => void }) {
  return (
    <article className="product-card">
      {product.pictureUrl ? (
        <img className="product-thumb" src={product.pictureUrl} alt={product.name} />
      ) : (
        <div className="product-thumb product-thumb-placeholder">{product.name.charAt(0).toUpperCase()}</div>
      )}
      <div className="product-info">
        <strong>{product.name}</strong>
        <div className="muted-text">{product.categoryName ?? "Chưa phân loại"}</div>
        <div className={product.hasPurchased ? "product-price" : "muted-text"}>{productPriceText(product)}</div>
        {product.hasPurchased && <div className="muted-text">Giá mua gần nhất</div>}
      </div>
      <button className="button primary" type="button" onClick={onPick}>
        Chọn
      </button>
    </article>
  );
}

function productPriceText(product: ProductPickerItem) {
  return product.hasPurchased && product.unitPrice !== null && product.unitPrice !== undefined
    ? money(product.unitPrice)
    : "Chờ báo giá";
}
