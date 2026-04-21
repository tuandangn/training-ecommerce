import { useState } from "react";

const LAYERS = [
  {
    id: "presentation",
    label: "Presentation Layer",
    color: "#6366f1",
    bg: "#eef2ff",
    border: "#a5b4fc",
    projects: [
      { name: "NamEcommerce.Web", desc: "Controllers · Views · ModelFactories · Models + Validators" },
      { name: "NamEcommerce.Web.Contracts", desc: "Commands · Queries · Result Models · View Models" },
      { name: "NamEcommerce.Web.Framework", desc: "MediatR Command Handlers · Query Handlers" },
    ],
  },
  {
    id: "application",
    label: "Application Layer",
    color: "#0891b2",
    bg: "#ecfeff",
    border: "#67e8f9",
    projects: [
      { name: "NamEcommerce.Application.Contracts", desc: "IXxxAppService · AppDtos · Validate()" },
      { name: "NamEcommerce.Application.Services", desc: "XxxAppService · Extensions (domainDto.ToDto())" },
    ],
  },
  {
    id: "domain",
    label: "Domain Layer",
    color: "#059669",
    bg: "#ecfdf5",
    border: "#6ee7b7",
    projects: [
      { name: "NamEcommerce.Domain", desc: "Entities (sealed record) · Accessibility" },
      { name: "NamEcommerce.Domain.Shared", desc: "Base Classes · Enums · Domain DTOs · Interfaces" },
      { name: "NamEcommerce.Domain.Services", desc: "XxxManager · Extensions (entity.ToDto())" },
    ],
  },
  {
    id: "infrastructure",
    label: "Infrastructure Layer",
    color: "#b45309",
    bg: "#fffbeb",
    border: "#fcd34d",
    projects: [
      { name: "NamEcommerce.Data.SqlServer", desc: "EF Core Configs · DbContext · Repositories · Migrations" },
    ],
  },
];

const FLOW_STEPS = [
  { label: "HTTP Request", icon: "🌐", color: "#6366f1" },
  { label: "Controller", icon: "🎮", color: "#6366f1" },
  { label: "MediatR", icon: "📨", color: "#8b5cf6" },
  { label: "Handler", icon: "⚙️", color: "#0891b2" },
  { label: "AppService", icon: "🔧", color: "#0891b2" },
  { label: "Manager", icon: "🏗️", color: "#059669" },
  { label: "Domain Entity", icon: "📦", color: "#059669" },
  { label: "Repository", icon: "🗄️", color: "#b45309" },
  { label: "SQL Server", icon: "💾", color: "#b45309" },
];

const MODULES = [
  { name: "Catalog", icon: "📋", entities: ["Category", "Product", "UnitMeasurement", "Vendor"], desc: "Quản lý sản phẩm, danh mục, đơn vị" },
  { name: "Orders", icon: "🛒", entities: ["Order", "OrderItem"], desc: "Đơn bán hàng, thanh toán" },
  { name: "Inventory", icon: "🏭", entities: ["InventoryStock", "Warehouse", "StockAuditLog", "StockMovementLog"], desc: "Tồn kho, kho hàng" },
  { name: "PurchaseOrders", icon: "📥", entities: ["PurchaseOrder", "PurchaseOrderItem"], desc: "Nhập hàng từ nhà cung cấp" },
  { name: "Customers", icon: "👥", entities: ["Customer"], desc: "Quản lý khách hàng" },
  { name: "Debts", icon: "💳", entities: ["CustomerDebt", "CustomerPayment"], desc: "Công nợ, thanh toán" },
  { name: "DeliveryNotes", icon: "🚚", entities: ["DeliveryNote", "DeliveryNoteItem"], desc: "Phiếu giao hàng" },
  { name: "Finance", icon: "💰", entities: ["Expense"], desc: "Thu chi, báo cáo tài chính" },
  { name: "Users", icon: "🔑", entities: ["User", "Role", "UserRole"], desc: "Người dùng, phân quyền" },
  { name: "Security", icon: "🛡️", entities: ["Permission", "RolePermission"], desc: "Bảo mật, phân quyền" },
  { name: "Media", icon: "🖼️", entities: ["Picture"], desc: "Ảnh sản phẩm" },
];

const RULES = [
  { icon: "🔒", title: "Entity Constructor = internal", desc: "Chỉ Manager mới được khởi tạo Entity" },
  { icon: "🔒", title: "Entity Methods = internal", desc: "Chỉ Manager mới được gọi method thay đổi state" },
  { icon: "✅", title: "DTO.Verify() ở Domain", desc: "Throw exception nếu data invalid" },
  { icon: "✅", title: "DTO.Validate() ở App", desc: "Return (bool, string?) — không throw" },
  { icon: "🚫", title: "Không chạy Migration", desc: "Developer tự chạy Add-Migration" },
  { icon: "🚫", title: "Không chạy Update-Database", desc: "Developer tự chạy Update-Database" },
  { icon: "🧪", title: "TDD cho mọi Manager method", desc: "Mọi method public phải có unit test" },
  { icon: "📨", title: "Controller chỉ dùng MediatR", desc: "Không inject AppService vào Controller" },
];

export default function SystemDiagram() {
  const [activeTab, setActiveTab] = useState("architecture");
  const [activeModule, setActiveModule] = useState(null);

  return (
    <div style={{ fontFamily: "'Segoe UI', sans-serif", background: "#f8fafc", minHeight: "100vh", padding: "24px" }}>
      {/* Header */}
      <div style={{ textAlign: "center", marginBottom: 32 }}>
        <h1 style={{ fontSize: 28, fontWeight: 700, color: "#1e293b", margin: 0 }}>
          🏗️ NamEcommerce — VLXD Tuấn Khôi
        </h1>
        <p style={{ color: "#64748b", marginTop: 6, fontSize: 14 }}>
          Clean Architecture · DDD · .NET 10 · SQL Server
        </p>
      </div>

      {/* Tabs */}
      <div style={{ display: "flex", gap: 8, justifyContent: "center", marginBottom: 28 }}>
        {[
          { id: "architecture", label: "📐 Kiến trúc" },
          { id: "flow", label: "🔄 Luồng dữ liệu" },
          { id: "modules", label: "📦 Modules" },
          { id: "rules", label: "📏 Quy tắc" },
        ].map(tab => (
          <button
            key={tab.id}
            onClick={() => setActiveTab(tab.id)}
            style={{
              padding: "8px 20px",
              borderRadius: 8,
              border: "none",
              cursor: "pointer",
              fontWeight: 600,
              fontSize: 14,
              background: activeTab === tab.id ? "#6366f1" : "#e2e8f0",
              color: activeTab === tab.id ? "#fff" : "#475569",
              transition: "all 0.2s",
            }}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* ── Tab: Architecture ── */}
      {activeTab === "architecture" && (
        <div style={{ maxWidth: 900, margin: "0 auto" }}>
          <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
            {LAYERS.map((layer, li) => (
              <div
                key={layer.id}
                style={{
                  background: layer.bg,
                  border: `2px solid ${layer.border}`,
                  borderRadius: 12,
                  padding: "16px 20px",
                }}
              >
                <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 12 }}>
                  <div style={{
                    width: 12, height: 12, borderRadius: "50%",
                    background: layer.color, flexShrink: 0,
                  }} />
                  <span style={{ fontWeight: 700, color: layer.color, fontSize: 15 }}>
                    {layer.label}
                  </span>
                  <span style={{ fontSize: 12, color: "#94a3b8", marginLeft: "auto" }}>
                    Layer {LAYERS.length - li}
                  </span>
                </div>
                <div style={{ display: "flex", gap: 10, flexWrap: "wrap" }}>
                  {layer.projects.map(p => (
                    <div
                      key={p.name}
                      style={{
                        background: "#fff",
                        border: `1px solid ${layer.border}`,
                        borderRadius: 8,
                        padding: "10px 14px",
                        flex: "1 1 240px",
                        minWidth: 200,
                      }}
                    >
                      <div style={{ fontWeight: 600, fontSize: 13, color: layer.color, marginBottom: 4 }}>
                        {p.name}
                      </div>
                      <div style={{ fontSize: 12, color: "#64748b" }}>{p.desc}</div>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>

          {/* Legend */}
          <div style={{
            marginTop: 20,
            background: "#fff",
            border: "1px solid #e2e8f0",
            borderRadius: 10,
            padding: "14px 18px",
          }}>
            <div style={{ fontSize: 13, fontWeight: 600, color: "#374151", marginBottom: 10 }}>
              📌 Nguyên tắc phụ thuộc (Dependency Rule)
            </div>
            <div style={{ fontSize: 13, color: "#4b5563" }}>
              <strong>Domain</strong> không phụ thuộc ai →{" "}
              <strong>Application</strong> chỉ phụ thuộc Domain →{" "}
              <strong>Infrastructure</strong> phụ thuộc Domain + Application →{" "}
              <strong>Presentation</strong> phụ thuộc Application (qua MediatR)
            </div>
          </div>
        </div>
      )}

      {/* ── Tab: Data Flow ── */}
      {activeTab === "flow" && (
        <div style={{ maxWidth: 860, margin: "0 auto" }}>
          <div style={{
            background: "#fff",
            borderRadius: 14,
            border: "1px solid #e2e8f0",
            padding: 24,
          }}>
            <h3 style={{ textAlign: "center", color: "#1e293b", marginTop: 0 }}>
              Luồng xử lý Request → Response
            </h3>

            {/* Flow steps horizontal */}
            <div style={{
              display: "flex",
              alignItems: "center",
              gap: 4,
              overflowX: "auto",
              padding: "8px 0 16px",
              justifyContent: "center",
              flexWrap: "wrap",
            }}>
              {FLOW_STEPS.map((step, i) => (
                <div key={step.label} style={{ display: "flex", alignItems: "center", gap: 4 }}>
                  <div style={{
                    display: "flex",
                    flexDirection: "column",
                    alignItems: "center",
                    gap: 4,
                  }}>
                    <div style={{
                      background: step.color + "15",
                      border: `2px solid ${step.color}`,
                      borderRadius: 10,
                      padding: "8px 14px",
                      textAlign: "center",
                      minWidth: 70,
                    }}>
                      <div style={{ fontSize: 20 }}>{step.icon}</div>
                      <div style={{ fontSize: 11, fontWeight: 600, color: step.color, marginTop: 2 }}>
                        {step.label}
                      </div>
                    </div>
                  </div>
                  {i < FLOW_STEPS.length - 1 && (
                    <div style={{ color: "#94a3b8", fontSize: 18, flexShrink: 0 }}>→</div>
                  )}
                </div>
              ))}
            </div>

            {/* Detailed flow breakdown */}
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12, marginTop: 8 }}>
              {[
                {
                  layer: "Presentation",
                  color: "#6366f1",
                  bg: "#eef2ff",
                  items: [
                    "Controller nhận request, inject IMediator",
                    "Gọi _mediator.Send(command | query)",
                    "ModelFactory chuẩn bị ViewModel cho View",
                    "Fluent Validation trên Model",
                  ],
                },
                {
                  layer: "MediatR / Handlers",
                  color: "#8b5cf6",
                  bg: "#f5f3ff",
                  items: [
                    "Command Handler → gọi AppService (write)",
                    "Query Handler → gọi AppService (read)",
                    "Handler map kết quả sang Result Model",
                    "Handler nằm trong Web.Framework",
                  ],
                },
                {
                  layer: "Application",
                  color: "#0891b2",
                  bg: "#ecfeff",
                  items: [
                    "AppService nhận AppDto, gọi Validate()",
                    "Gọi Manager qua interface",
                    "Không throw exception ra ngoài",
                    "Trả về Result DTO (Success/ErrorMessage)",
                  ],
                },
                {
                  layer: "Domain",
                  color: "#059669",
                  bg: "#ecfdf5",
                  items: [
                    "Manager nhận DomainDto, gọi Verify()",
                    "Tạo/sửa Entity qua internal constructor/method",
                    "IRepository<T> để write, IEntityDataReader<T> để read",
                    "Publish events qua IEventPublisher",
                  ],
                },
              ].map(section => (
                <div
                  key={section.layer}
                  style={{
                    background: section.bg,
                    border: `1px solid ${section.color}40`,
                    borderRadius: 10,
                    padding: "12px 16px",
                  }}
                >
                  <div style={{ fontWeight: 700, color: section.color, fontSize: 13, marginBottom: 8 }}>
                    {section.layer}
                  </div>
                  {section.items.map(item => (
                    <div key={item} style={{ fontSize: 12, color: "#374151", marginBottom: 4, display: "flex", gap: 6 }}>
                      <span style={{ color: section.color }}>▸</span>
                      {item}
                    </div>
                  ))}
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* ── Tab: Modules ── */}
      {activeTab === "modules" && (
        <div style={{ maxWidth: 960, margin: "0 auto" }}>
          <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(260px, 1fr))", gap: 14 }}>
            {MODULES.map(mod => (
              <div
                key={mod.name}
                onClick={() => setActiveModule(activeModule === mod.name ? null : mod.name)}
                style={{
                  background: "#fff",
                  border: `2px solid ${activeModule === mod.name ? "#6366f1" : "#e2e8f0"}`,
                  borderRadius: 12,
                  padding: "16px 18px",
                  cursor: "pointer",
                  transition: "all 0.2s",
                  boxShadow: activeModule === mod.name ? "0 4px 14px #6366f130" : "none",
                }}
              >
                <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 6 }}>
                  <span style={{ fontSize: 24 }}>{mod.icon}</span>
                  <span style={{ fontWeight: 700, fontSize: 15, color: "#1e293b" }}>{mod.name}</span>
                </div>
                <p style={{ fontSize: 12, color: "#64748b", margin: "0 0 10px" }}>{mod.desc}</p>

                {activeModule === mod.name && (
                  <div style={{ borderTop: "1px solid #e2e8f0", paddingTop: 10 }}>
                    <div style={{ fontSize: 11, fontWeight: 600, color: "#94a3b8", marginBottom: 6, textTransform: "uppercase", letterSpacing: 1 }}>
                      Entities
                    </div>
                    <div style={{ display: "flex", flexWrap: "wrap", gap: 6 }}>
                      {mod.entities.map(e => (
                        <span
                          key={e}
                          style={{
                            background: "#eef2ff",
                            color: "#6366f1",
                            borderRadius: 6,
                            padding: "2px 8px",
                            fontSize: 12,
                            fontWeight: 600,
                            border: "1px solid #c7d2fe",
                          }}
                        >
                          {e}
                        </span>
                      ))}
                    </div>
                    <div style={{ marginTop: 10 }}>
                      <div style={{ fontSize: 11, fontWeight: 600, color: "#94a3b8", marginBottom: 4, textTransform: "uppercase", letterSpacing: 1 }}>
                        Liên quan
                      </div>
                      {mod.entities.map(e => (
                        <div key={e} style={{ fontSize: 11, color: "#64748b", marginBottom: 2 }}>
                          • I{e}Manager · {e}AppService
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {activeModule !== mod.name && (
                  <div style={{ display: "flex", flexWrap: "wrap", gap: 5 }}>
                    {mod.entities.slice(0, 3).map(e => (
                      <span
                        key={e}
                        style={{
                          background: "#f1f5f9",
                          color: "#64748b",
                          borderRadius: 5,
                          padding: "2px 7px",
                          fontSize: 11,
                        }}
                      >
                        {e}
                      </span>
                    ))}
                    {mod.entities.length > 3 && (
                      <span style={{ fontSize: 11, color: "#94a3b8" }}>+{mod.entities.length - 3}</span>
                    )}
                  </div>
                )}
              </div>
            ))}
          </div>
          <p style={{ textAlign: "center", color: "#94a3b8", fontSize: 12, marginTop: 14 }}>
            Nhấn vào module để xem chi tiết Entities
          </p>
        </div>
      )}

      {/* ── Tab: Rules ── */}
      {activeTab === "rules" && (
        <div style={{ maxWidth: 860, margin: "0 auto" }}>
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
            {RULES.map(rule => (
              <div
                key={rule.title}
                style={{
                  background: "#fff",
                  border: "1px solid #e2e8f0",
                  borderRadius: 10,
                  padding: "14px 16px",
                  display: "flex",
                  gap: 12,
                  alignItems: "flex-start",
                }}
              >
                <span style={{ fontSize: 24, flexShrink: 0 }}>{rule.icon}</span>
                <div>
                  <div style={{ fontWeight: 700, fontSize: 14, color: "#1e293b", marginBottom: 4 }}>{rule.title}</div>
                  <div style={{ fontSize: 13, color: "#64748b" }}>{rule.desc}</div>
                </div>
              </div>
            ))}
          </div>

          {/* Naming convention table */}
          <div style={{
            marginTop: 20,
            background: "#fff",
            border: "1px solid #e2e8f0",
            borderRadius: 12,
            overflow: "hidden",
          }}>
            <div style={{ background: "#1e293b", padding: "12px 18px" }}>
              <span style={{ color: "#f8fafc", fontWeight: 700, fontSize: 14 }}>📌 Naming Conventions</span>
            </div>
            <table style={{ width: "100%", borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ background: "#f8fafc" }}>
                  {["Thành phần", "Pattern", "Ví dụ (Category)"].map(h => (
                    <th key={h} style={{ padding: "10px 14px", textAlign: "left", fontSize: 12, fontWeight: 700, color: "#374151", borderBottom: "1px solid #e2e8f0" }}>
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {[
                  ["Domain Manager Interface", "I{Entity}Manager", "ICategoryManager"],
                  ["Domain Manager", "{Entity}Manager", "CategoryManager"],
                  ["Domain DTO", "{Action}{Entity}Dto", "CreateCategoryDto"],
                  ["App Service Interface", "I{Entity}AppService", "ICategoryAppService"],
                  ["App Service", "{Entity}AppService", "CategoryAppService"],
                  ["App DTO", "{Action}{Entity}AppDto", "CreateCategoryAppDto"],
                  ["Command", "{Action}{Entity}Command", "CreateCategoryCommand"],
                  ["Query", "Get{Entity}Query", "GetCategoryListQuery"],
                  ["Handler", "{Action}{Entity}Handler", "CreateCategoryHandler"],
                  ["Model Factory", "I{Entity}ModelFactory", "ICategoryModelFactory"],
                  ["View Model", "{Action}{Entity}Model", "CreateCategoryModel"],
                  ["Validator", "{Action}{Entity}Validator", "CreateCategoryValidator"],
                ].map(([comp, pattern, example], i) => (
                  <tr key={comp} style={{ background: i % 2 === 0 ? "#fff" : "#f8fafc" }}>
                    <td style={{ padding: "9px 14px", fontSize: 13, color: "#374151", borderBottom: "1px solid #f1f5f9" }}>{comp}</td>
                    <td style={{ padding: "9px 14px", fontSize: 12, fontFamily: "monospace", color: "#6366f1", borderBottom: "1px solid #f1f5f9" }}>{pattern}</td>
                    <td style={{ padding: "9px 14px", fontSize: 12, fontFamily: "monospace", color: "#059669", borderBottom: "1px solid #f1f5f9" }}>{example}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Footer */}
      <div style={{ textAlign: "center", marginTop: 32, color: "#94a3b8", fontSize: 12 }}>
        VLXD Tuấn Khôi · NamEcommerce · .NET 10 · DDD + Clean Architecture
      </div>
    </div>
  );
}
