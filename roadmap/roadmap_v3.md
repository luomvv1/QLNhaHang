# Roadmap - Hệ thống Quản Lý Order Món Ăn Theo Bàn

> **Cập nhật:** 2026-10-20 · **Phiên bản:** v2.8.0 · **Lịch sử:** `changelog.md`  
> **Ký hiệu trạng thái:** ✅ Đã hoàn thành & test · 🟡 Đang làm / Một phần · ⬜ Chưa làm · 🔑 Cần credential/hạ tầng ngoài · 🏗️ Subsystem lớn  
> **Tài liệu đối chiếu:** `docs/database/21_VoVanLuom.docx` · `docs/database/restaurant_schema.sql` · `docs/database/qlNhaHang.drawio`

---

### Cập nhật nghiệp vụ Sơ đồ bàn & Đặt bàn thông minh (v2.8.0 - 2026-10-20)

| Cụm nghiệp vụ | Đã cập nhật & hoàn tất trong hệ thống | Trạng thái thực tế | Việc còn lại |
|---|---|:---:|---|
| **Sơ đồ bàn & Phân khu (M05)** | Chuẩn hóa 3 trạng thái vận hành thiết bị của bàn (`🟢 Đang hoạt động`, `🟡 Đang sửa chữa`, `🔴 Ngừng sử dụng`), tách biệt hoàn toàn với trạng thái order live của POS. Tinh gọn bộ chọn khu vực 1 chạm, thêm phân khu mới linh hoạt. Bổ sung endpoint `PATCH /api/admin/table/reactivate` khôi phục bàn ngừng dùng. | ✅ Hoàn tất | Mở rộng vẽ sơ đồ 2D canvas (nếu có yêu cầu) |
| **Xếp bàn & Gộp bàn thông minh (M11)** | Xếp bàn tự động theo số khách: ưu tiên bàn đơn đúng sức chứa, chỉ gợi ý gộp bàn khi hết bàn đơn. Ràng buộc an toàn: bắt buộc các bàn gộp phải thuộc **cùng một phân khu vực** (`AreaName`). Chặn xếp bàn/đặt bàn vào bàn đang sửa chữa (`maintenance`). | ✅ Hoàn tất | Đặt cọc trực tuyến qua cổng thanh toán |
| **Giao diện Quản lý Đặt bàn (M11)** | UI/UX Pro desktop-first: bảng vừa vặn màn hình desktop, gỡ bỏ `position: sticky` triệt tiêu lỗi che khuất cột Trạng thái, nhãn trạng thái rút gọn (`Chờ xếp bàn`), nút Xếp bàn pastel dịu nhẹ (`#eef2ff`), avatar tròn, KPI metrics và bộ lọc realtime theo ngày (Hôm nay, Ngày mai, Tất cả ngày). | ✅ Hoàn tất | Báo cáo tỷ lệ lấp đầy bàn theo khung giờ |

---

### Mục lục điều hướng nhanh
- [1. Tổng quan tiến độ](#1-tổng-quan-tiến-độ)
  - [1.1. Core Backend & Quản trị Hệ thống (Admin Web)](#11-core-backend--quản-trị-hệ-thống-admin-web)
  - [1.2. Nghiệp vụ Order & Phục vụ (Staff Mobile / Web)](#12-nghiệp-vụ-order--phục-vụ-staff-mobile--web)
  - [1.3. Khu vực Bếp & Bar (Kitchen Display System - KDS)](#13-khu-vực-bếp--bar-kitchen-display-system---kds)
  - [1.4. Khách hàng & Đặt bàn (Customer Portal)](#14-khách-hàng--đặt-bàn-customer-portal)
  - [1.5. Phân hệ mở rộng: Kho hàng, Báo cáo & AI](#15-phân-hệ-mở-rộng-kho-hàng-báo-cáo--ai)
- [2. Danh sách việc còn lại theo mức ưu tiên](#2-danh-sách-việc-còn-lại-theo-mức-ưu-tiên)
  - [Ưu tiên cao nhất (P0 - Critical Core)](#ưu-tiên-cao-nhất-p0---critical-core)
  - [Ưu tiên kế tiếp (P1 - Operations)](#ưu-tiên-kế-tiếp-p1---operations)
  - [Ưu tiên nâng cao (P2 - Advanced & Extension)](#ưu-tiên-nâng-cao-p2---advanced--extension)
- [3. Chi tiết nghiệp vụ & Use Case theo Actor](#3-chi-tiết-nghiệp-vụ--use-case-theo-actor)
  - [Cụm A: Quản lý Thực đơn & Danh mục (Admin)](#cụm-a-quản-lý-thực-đơn--danh-mục-admin)
  - [Cụm B: Sơ đồ bàn & Khu vực (Admin / Staff)](#cụm-b-sơ-đồ-bàn--khu-vực-admin--staff)
  - [Cụm C: Quy trình Order tại bàn (Staff)](#cụm-c-quy-trình-order-tại-bàn-staff)
  - [Cụm D: Xử lý món tại Bếp (Chef KDS)](#cụm-d-xử-lý-món-tại-bếp-chef-kds)
  - [Cụm E: Khách hàng, Thành viên & Đặt bàn (Customer)](#cụm-e-khách-hàng-thành-viên--đặt-bàn-customer)
  - [Cụm F: Thanh toán & Xuất hóa đơn (Staff / Admin)](#cụm-f-thanh-toán--xuất-hóa-đơn-staff--admin)
  - [Cụm G: Kho nguyên liệu & Định lượng (Admin / Chef)](#cụm-g-kho-nguyên-liệu--định-lượng-admin--chef)
  - [Cụm H: Thống kê & Báo cáo Doanh thu (Admin)](#cụm-h-thống-kê--báo-cáo-doanh-thu-admin)
  - [Cụm I: Audit Log, Bảo mật & Giám sát](#cụm-i-audit-log-bảo-mật--giám-sát)
- [4. Quy trình liên thông cốt lõi](#4-quy-trình-liên-thông-cốt-lõi)
- [5. Đối chiếu Database & Ràng buộc toàn vẹn](#5-đối-chiếu-database--ràng-buộc-toàn-vẹn)
- [6. Kế hoạch triển khai theo giai đoạn (Phase 0 → Phase 9)](#6-kế-hoạch-triển-khai-theo-giai-đoạn-phase-0--phase-9)
- [7. Tiêu chuẩn kiểm thử & Nghiệm thu E2E](#7-tiêu-chuẩn-kiểm-thử--nghiệm-thu-e2e)

> **💡 Hướng dẫn click điều hướng:**
> - Bấm vào mã module (cột **`#`**) để nhảy xuống xem chi tiết chức năng & database liên quan.
> - Bấm vào mã task ở cột **`Còn lại`** (`Mxx-Rx`) để nhảy thẳng tới việc cần thực hiện tiếp theo.

---

### Cập nhật nghiệp vụ gần nhất (v2.3.0)

| Cụm | Đã cập nhật trong code | Trạng thái thực tế | Việc còn lại |
|---|---|:---:|---|
| Tài khoản khách hàng | Đăng ký tạo/liên kết `Users` - `Customers` qua `UserId`; khách xem và sửa hồ sơ cá nhân | 🟡 | Băm mật khẩu, đổi/quên mật khẩu, refresh token |
| Đặt bàn trực tuyến | Khách chọn ngày, giờ, số người và ghi chú; không chọn khu vực/bàn; thông tin người đặt lấy từ tài khoản khi đã đăng nhập | ✅ Core | Đặt cọc, nhắc lịch và kiểm tra slot theo khung giờ |
| Tiếp nhận đặt bàn | Quy trình tuần tự `pending` → xác nhận → phân bàn `reserved` → check-in; chặn bàn bận và bàn thiếu sức chứa | ✅ Core | Kiểm thử cạnh tranh khi nhiều nhân viên cùng phân một bàn |
| Lịch sử khách hàng | Xem lịch đặt bàn, trạng thái, điểm và bill đầy đủ món/đơn giá/giảm trừ/bàn/thu ngân/phương thức thanh toán | ✅ Core | Liên kết trực tiếp Reservation - Order - Invoice |
| Đồng bộ thời gian thực | SignalR phát `ReservationUpdated`; tài khoản khách tự làm mới; admin có toast/chuông ở mọi trang quản trị | ✅ Core | Lưu lịch sử thông báo vào database |
| Bếp & phục vụ | Chef: `confirmed` → `cooking` → `ready`; Staff: `ready` → `served` | ✅ Core | Kiểm thử E2E liên vai trò |

### Cập nhật WMS/BOM - Hạng mục phiếu kho, kiểm kê và suất bán (2026-10-15)

Đã triển khai backend cho `M12`:

- `AdminStockReceiptController`: tra cứu/phân trang phiếu kho, tạo/sửa/hủy draft, approve theo `Serializable`, FEFO/FIFO, rollback khi thiếu tồn, chống approve lặp, hoàn kho và xuất hủy nhanh.
- `AdminAuditController`: tạo snapshot kiểm kê, nhập số đếm, hoàn tất điều chỉnh `audit_adjustment`, tạo batch điều chỉnh khi thừa và movement/detail theo từng batch khi thiếu.
- `ChefController` và `StaffOrderController`: đồng bộ `SoldPortions` khi chuyển `served`, hoàn lại khi undo, khóa transaction để tránh cộng trùng.
- `StockReceiptDetails`: lưu thêm thông tin lô dự kiến (`NewBatchCode`, `ManufacturingDate`, `ExpiryDate`) để approve không làm mất dữ liệu phiếu nháp.
- Chuẩn hóa giá vốn: tồn và BOM vẫn tính theo `BaseUnit`, còn `CostPerUnit`/`UnitCost` lưu theo `PurchaseUnit`; mọi thành tiền dùng `BaseQuantity / ConversionRate * PurchaseUnitCost`.
- Bổ sung migration một lần `docs/database/migration_wms_cost_purchase_unit.sql` để chuyển dữ liệu cũ sang quy ước giá mới.

Trạng thái M12 vẫn là 🟡 cho đến khi chạy đủ nghiệm thu E2E qua giao diện và test cạnh tranh cho approve/hoàn/hủy. API smoke, rollback nghiệp vụ, lô hết hạn và snapshot BOM đã có kết quả kiểm tra; chi tiết quy ước nằm trong `docs/database/wms_bom_antigravity_handoff.md`.

> **Quy tắc cập nhật:** Chỉ đánh dấu ✅ khi API, giao diện và dữ liệu SQL cùng chạy được. Những nội dung chưa có kiểm thử E2E hoặc còn thiếu ràng buộc dữ liệu phải giữ 🟡.

### Đối soát quy trình kho 6 bước và xác nhận vận hành (2026-10-19)

Đối chiếu với tài liệu quy trình kho được gửi ngày 2026-10-19 và xác nhận vận hành bổ sung của chủ dự án. Mô hình hiện tại là **một kho nguyên liệu trung tâm; vẫn lập kế hoạch suất ăn để tổng hợp BOM và tạo phiếu xuất bếp nháp, thủ kho duyệt FEFO trước khi trừ tồn**. Đây là phiếu cấp phát theo kế hoạch, không phải kho thứ hai có tồn độc lập.

| Bước | Đã có trong hệ thống | Sai khác / việc còn thiếu |
|---|---|---|
| 1. Nguyên liệu và BOM | `InventoryItems`, `FoodIngredients`, BOM theo `FoodId`/`VariantId`; `QuantityPerUnit` là lượng nguyên liệu sạch dùng cho món, phiếu xuất không trừ hao hụt lần nữa. | `FoodIngredients.WastagePercent` chỉ giữ để tương thích dữ liệu cũ, không còn chỉnh trên view BOM và không tham gia tính lượng xuất. Hao hụt thực tế được ghi theo dòng nhập/lô. |
| 2. Nhập lô | Phiếu `import_supplier` ghi lượng mua thô, tỷ lệ và lượng hao hụt; khi duyệt mới tạo/cập nhật batch, cộng tồn theo lượng sạch, giữ nguyên tổng tiền mua và phân bổ giá vốn theo lượng sạch. | Đã cập nhật API và view. Bắt buộc chạy `docs/database/migration_wms_receipt_prep_wastage.sql` trước khi khởi động backend phiên bản mới. |
| 3. Kế hoạch | `DailyPortionControls` lưu kế hoạch theo món/variant; API nhân BOM, gom nguyên liệu và tạo phiếu `export_kitchen` nháp. Tồn khả dụng được tính từ batch `active` chưa hết hạn theo ngày kế hoạch, không dùng toàn bộ `CurrentQuantity`. | ✅ Đã có UI hiển thị tồn còn hạn, tồn hết hạn, cảnh báo thiếu và gợi ý mua bù theo mức tồn tối thiểu. Còn nghiệm thu trực tiếp trên trình duyệt. |
| 4. Xuất cho bếp | Approve phiếu xuất trừ batch theo FEFO, trừ tồn tổng và ghi movement trong transaction. Phiếu giữ snapshot BOM để lịch sử không đổi khi sửa công thức. | ✅ Luồng một kho trung tâm đã đúng. Còn mở rộng kiểm thử đồng thời đầy đủ cho approve/hoàn/hủy và cân nhắc tách bước bàn giao - duyệt nếu vận hành nhiều ca. |
| 5. Nấu và phục vụ | KDS chuyển món qua trạng thái; `SoldPortions` chỉ cộng khi món thành `served` và hoàn lại khi undo. Đối soát nguyên liệu dùng số suất đã phục vụ nhân BOM snapshot, không trừ BOM lần hai ở order/KDS. | ✅ Đã tách rõ số suất phục vụ và lượng nguyên liệu. Món nấu nhưng chưa phục vụ hoặc hỏng cần tiếp tục theo dõi riêng nếu nhà hàng yêu cầu. |
| 6. Đối soát cuối ca | Đối soát nguyên liệu đã xuất bếp theo từng dòng/lô FEFO, tách đã dùng/hoàn/hủy, khóa chốt một lần và có báo cáo theo ngày chốt. | ✅ API/UI đã kiểm thử các trường hợp cân bằng, thiếu lý do và chốt lặp. Còn E2E trình duyệt và quyết định duyệt hai bước. |

**Quyết định thiết kế đã xác nhận:** duy trì một kho nguyên liệu trung tâm và luồng **lập kế hoạch theo ngày → tính/gom BOM → tạo phiếu xuất bếp nháp → thủ kho duyệt FEFO → ghi giảm tồn và movement**. Kế hoạch không tạo kho thứ hai. Định mức xuất là lượng nguyên liệu sạch/thực dùng; hao hụt được khấu trừ ở khâu nhập/sơ chế, không cộng hao hụt vào phiếu xuất lần nữa. Form/API nhập lô đã nhận lượng mua thô, tỷ lệ hao hụt mặc định, lượng thực nhận sau sơ chế và lý do vượt hao hụt; chỉ lượng sạch thực nhận được cộng vào tồn/batch, giá vốn được tính trên lượng sạch nhưng giữ nguyên tổng tiền mua. Tồn món chín chuyển ngày là trường hợp tùy chọn, chỉ nhập khi thực tế có. Đối soát thực cấp, trả, hủy cuối ca nối với phiếu cấp phát đã duyệt; tuyệt đối không tạo thêm lượt trừ kho ở order/KDS cho cùng lượng nguyên liệu.

**Cảnh báo script:** không chạy DDL trong tài liệu quy trình đính kèm. Script đó `DROP TABLE dbo.FoodIngredients`, tạo lại các bảng WMS đã có với tên cột/trạng thái không tương thích và có thể xóa BOM. Schema/migration hiện hành là `restaurant_schema.sql`, `migration_wms_bom.sql` và các migration WMS tiếp theo.

**Các việc M12 cần làm tiếp:**

- P0: chạy nghiệm thu E2E qua trình duyệt cho Admin/Chef/Staff theo luồng kế hoạch → lập phiếu → duyệt FEFO → đối soát cuối ca.
- P1: bổ sung test đồng thời cho approve/hoàn/hủy và kiểm tra rollback khi nhiều người thao tác cùng một lô.
- P1: cân nhắc bước bàn giao/duyệt riêng khi vận hành nhiều ca hoặc tách quyền bếp và thủ kho; hiện quản lý chốt đối soát và phiếu được ghi nhận ngay.
- P1: bổ sung báo cáo mua bù theo tồn khả dụng, mức tồn tối thiểu và các lô hết hạn cần hủy.
- P2: tiếp tục theo dõi món đã nấu nhưng chưa phục vụ/chế biến hỏng nếu nhà hàng cần đối soát riêng ngoài số suất `served`.

### Cập nhật đối soát cuối ca (2026-10-19)

- Hàng hoàn bếp được tạo thành **lô riêng** (`TH-BEP-...`), liên kết phiếu hoàn và `StockMovement(import)` để không lẫn với tồn gốc. Hạn cấp phát của lô hoàn là ngày sớm hơn giữa HSD lô gốc và **cuối ngày kế tiếp theo giờ Việt Nam**. Lô gốc không còn hạn đến ngày mai thì không được hoàn; xử lý phần dư tại bếp. Khi duyệt xuất bếp, ưu tiên lô hoàn trước các lô thường; từ ngày thứ hai sau hoàn, API không cho cấp phát nữa kể cả khi lô vẫn còn tồn sổ sách. Không cho sửa HSD lô hoàn. Hàng hoàn không dùng hết cần lập phiếu hủy tại kho để giảm tồn và ghi nhận giá trị hủy; chưa có tự động hủy cuối ngày.
- Lô có HSD đã qua nhưng còn tồn được trả về với trạng thái hiệu lực `expired` theo ngày Việt Nam, không phụ thuộc trạng thái cũ trong CSDL. Không xuất bếp/hoàn kho; màn kho hiển thị `Quá hạn · chờ hủy`, có bộ lọc và nút chuyển thẳng sang `Hủy tại kho`. Phiếu `waste_disposal` bắt buộc chọn đúng lô, lý do, trừ tồn và ghi `StockMovement(waste)` trong transaction.
- Màn riêng `/admin/kitchen-reconciliation` đọc phiếu xuất bếp đã duyệt theo ngày và lô FEFO. **Đã dùng theo phục vụ** được tính từ `DailyPortionControls.SoldPortions` của từng món/biến thể nhân `FoodIngredients.QuantityPerUnit`, phân bổ một lần theo thứ tự phiếu xuất/lô (phiếu ban đầu trước phiếu bổ sung). Người vận hành nhập **hoàn kho** và **hủy tại bếp**; chỉ chốt khi **đã xuất = đã dùng theo phục vụ + hoàn + hủy**. Không suy lượng đã dùng từ phần dư của phiếu. Muốn giữ nguyên liệu tại bếp sang ca sau thì chưa chốt. Đây là tiêu hao theo BOM của món **đã phục vụ**, không phải số cân thực tế khi nấu; món nấu nhưng chưa phục vụ/chế biến hỏng cần theo dõi riêng.
- Chốt phiếu tạo bản ghi `KitchenReconciliationLines` gắn `SourceDetailId` duy nhất. Phần hoàn sinh phiếu `return_from_kitchen`, cộng lại đúng lô và tạo `StockMovement(import)`. Lô quá hạn không được hoàn. Phần hủy sinh phiếu `waste_disposal` và lưu lý do, giá vốn, liên kết phiếu xuất; **không trừ kho lần hai** vì kho đã giảm lúc duyệt phiếu xuất bếp.
- Hủy hàng còn nằm trong kho là tab riêng, gọi `quick-waste`, bắt buộc chọn lô và lý do; backend trừ tồn kho/lô và ghi `StockMovement(waste)` trong transaction.
- Màn thao tác lọc phiếu theo **ngày kế hoạch trên mã phiếu hoặc ngày duyệt xuất** để tìm lượng bếp cần đối soát. Số suất phục vụ ghi theo ngày Việt Nam; báo cáo hoàn/hủy và CSV tính theo **ngày chốt đối soát**, kể cả khi phiếu xuất thuộc ngày trước. Hủy tại kho tính theo ngày duyệt phiếu hủy. Xem được từng phiếu/lô, lượng xuất/dùng/hoàn/hủy, lý do và giá trị hủy. Không cộng số lượng khác đơn vị vào một tổng giả.
- API hoàn nhanh cũ đã bỏ khỏi luồng; phiếu hoàn thủ công không còn được lập/duyệt để tránh hoàn không có nguồn xuất. Quản lý là người chốt và các phiếu được duyệt ngay; nếu cần hai chữ ký bếp/thủ kho thì phát triển bước duyệt riêng sau.
- Migration `docs/database/migration_kitchen_reconciliation.sql` đã chạy trên SQL Server nội bộ. GET API thực tế trả 6 phiếu/19 dòng ngày 19/10/2026. POST có lượng hoàn/hủy chưa cân bằng với số suất đã phục vụ trả 409; hủy kho thiếu lý do trả 400. E2E trình duyệt vẫn cần chạy thủ công vì repo chưa có Playwright/Cypress.
- Kiểm thử lại theo số suất phục vụ thật: ngày 19/10 có 3 suất Ba Chỉ Heo 250g, API phân bổ đúng 750g đã dùng trên các phiếu (không lặp trên phiếu bổ sung). Phiếu QA xuất 1.000g chốt 750g dùng + 200g hoàn + 50g hủy, báo cáo ghi 8đ hủy; chốt lặp trả 409. Đã xóa bộ QA và khôi phục tồn 21.500g. Từ 2026-10-20, phiếu xuất bếp lưu `StockReceiptDetails.BomSnapshotJson`; khi FEFO tách dòng, snapshot được chia theo tỷ lệ từng lô, nên sửa BOM sau đó không đổi lịch sử đối soát. Migration mới: `docs/database/migration_stock_receipt_bom_snapshot.sql`.

### Cập nhật kiểm tra tồn lô và concurrency (2026-10-20)

- Lập kế hoạch và lập phiếu bổ sung dùng tổng `InventoryBatches.QuantityRemaining` của các lô `active` có HSD >= ngày kế hoạch; tồn lô hết hạn được tách riêng thành `expiredQuantity`, không được coi là tồn khả dụng. UI hiển thị rõ “Lô còn hạn”, lượng hết hạn bị loại và cảnh báo tồn sau kế hoạch thấp hơn mức tối thiểu.
- Snapshot BOM được lưu trên từng dòng phiếu xuất bếp, bao gồm món/biến thể, số suất, định lượng và hao hụt tham chiếu. Đối soát cuối ca ưu tiên snapshot; dữ liệu phiếu cũ không có snapshot vẫn dùng fallback BOM hiện tại để tương thích.
- Kiểm thử API liên vai trò: Admin/Chef/Staff đăng nhập và gọi đúng endpoint; đối soát ngày 19/10 trả 6 phiếu, không có lỗi sử dụng. Kiểm thử đồng thời 8 request lập phiếu đạt `1 x 200` và `7 x 409`, phiếu test được hủy sau kiểm thử. Kịch bản lặp lại nằm tại `docs/testing/wms_e2e_smoke.ps1`.
- Đã xử lý deadlock cạnh tranh khi lập phiếu thành phản hồi `409` có mã `REQUISITION_CONCURRENT`; E2E trực tiếp qua trình duyệt Admin/Chef/Staff vẫn là bước nghiệm thu riêng.

**Tệp hiện thực để đối chiếu nhanh:**

| Nội dung | Tệp chính |
|---|---|
| API đặt/hủy bàn, tài khoản khách, lịch sử hóa đơn | `backend/Restaurant.Api/Controllers/Customer/CustomerReservationController.cs` |
| Form đặt bàn và tự điền thông tin tài khoản | `fontend/web/src/views/customer/ReservationView.vue` |
| Hồ sơ khách, lịch sử đặt bàn, điểm và xem bill | `fontend/web/src/views/customer/CustomerAccountView.vue` |
| Kênh kết nối SignalR dùng chung | `fontend/web/src/services/signalr.js` |
| Chuông/toast đặt bàn hiển thị trên mọi trang admin | `fontend/web/src/components/AdminReservationNotifier.vue` |
| Layout quản trị giữ thông báo toàn cục | `fontend/web/src/views/admin/layout/AdminLayoutView.vue` |

---

## 1. Tổng quan tiến độ

### 1.1. Core Backend & Quản trị Hệ thống (Admin Web)

| # | Module | Trạng thái | Còn lại / Kết quả cần đạt |
|:---:|---|:---:|---|
| [M00](#m00-detail) | Nền tảng, Môi trường & Kiến trúc | 🟡 | [M00-R1: Hoàn thiện Global Exception Middleware & Logging Central](#m00-r1) |
| [M01](#m01-detail) | Tài khoản, Phân quyền & Nhân sự | 🟡 | [M01-R1: Refresh Token & Quên mật khẩu qua Email 🔑](#m01-r1) |
| [M02](#m02-detail) | Danh mục món ăn (Category) | ✅ | [M02-R1: Đã hoàn thành CRUD, validation, lọc theo tên](#m02-detail) |
| [M03](#m03-detail) | Món ăn & Hình ảnh (Food) | ✅ | [M03-R1: Tích hợp Upload ảnh trực tiếp lên Cloudinary / Storage 🔑](#m03-r1) |
| [M04](#m04-detail) | Biến thể & Size món ăn (FoodVariant) | ✅ | [M04-R1: Đã xong CRUD biến thể, ngăn xóa kẹt khi mở bán lại](#m04-detail) |
| [M05](#m05-detail) | Sơ đồ bàn & Phân khu (DiningTable) | ✅ | Đã xong: Sơ đồ bàn, phân khu vực động, chọn nhanh 1 chạm, chuẩn hóa 3 trạng thái vận hành thiết bị (Hoạt động / Sửa chữa / Ngừng dùng) tách biệt với trạng thái Order live, khôi phục qua PATCH reactivate |

### 1.2. Nghiệp vụ Order & Phục vụ (Staff Mobile / Web)

| # | Module | Trạng thái | Còn lại / Kết quả cần đạt |
|:---:|---|:---:|---|
| [M06](#m06-detail) | Order tại bàn & Gán bàn | ✅ | Đã xong: Mở bàn, thêm món, chọn size biến thể, ghi chú nhanh |
| [M06](#m06-detail) | Chuyển bàn & Gộp bàn | ✅ | Đã xong: Chuyển bàn, ghép thêm bàn trống (đoàn đông), gộp 2 đơn |
| [M06](#m06-detail) | Ghi chú & Xác nhận món gửi Bếp | ✅ | Đã xong: Gửi món xuống bếp (`confirmed`), xem & in hóa đơn tạm tính |

### 1.3. Khu vực Bếp & Bar (Kitchen Display System - KDS)

| # | Module | Trạng thái | Còn lại / Kết quả cần đạt |
|:---:|---|:---:|---|
| [M07](#m07-detail) | Hàng đợi chế biến (KDS Queue) | ✅ | Đã xong: Màn hình Bếp xem thẻ bàn & hàng đợi món FIFO thời gian thực |
| [M07](#m07-detail) | Cập nhật tiến độ món ăn | ✅ | Chef xử lý `confirmed` → `cooking` → `ready`; Staff xác nhận `ready` → `served` |
| [M07](#m07-detail) | Báo hết món (Sold out) | ✅ | Đã xong: Đầu bếp đánh dấu hết món, đồng bộ tức thì sang POS phục vụ |

### 1.4. Khách hàng & Đặt bàn (Customer Portal)

| # | Module | Trạng thái | Còn lại / Kết quả cần đạt |
|:---:|---|:---:|---|
| [M08](#m08-detail) | Hồ sơ khách hàng & Tích điểm | 🟡 | Đã xong portal tài khoản, lịch sử điểm/hóa đơn; còn nâng cấp bảo mật & hạng thành viên |
| [M11](#m11-detail) | Đặt bàn trước (Reservation) | 🟡 | Đã xong đặt/hủy, xác nhận, xếp bàn thông minh theo số khách & gộp bàn cùng phân khu, UI/UX Pro desktop-first (không cuộn ngang, nhãn rút gọn, nút pastel), lọc realtime theo ngày, check-in và SignalR; còn liên kết Reservation - Order & chính sách đặt cọc |
| [M14](#m14-detail) | Đánh giá món ăn & Phản hồi | ⬜ | [M14-R1: Khách gửi review 1-5 sao sau khi thanh toán hóa đơn](#m14-r1) |

### 1.5. Phân hệ mở rộng: Kho hàng, Báo cáo & AI

| # | Module | Trạng thái | Còn lại / Kết quả cần đạt |
|:---:|---|:---:|---|
| [M09](#m09-detail) | Khuyến mãi & Voucher (Promotions) | ✅ | Đã xong: Quản lý voucher % & tiền mặt, PromotionUsages |
| [M10](#m10-detail) | Thanh toán & Xuất hóa đơn | ✅ | Đã xong: POS đa phương thức, VietQR động, Webhook tự động, in bill K80 🏗️ *(Bảo lưu kết nối cổng ngân hàng thật khi golive)* |
| [M12](#m12-detail) | Kho nguyên liệu & Nhà cung cấp | 🟡 | Đã có CRUD/BOM, kế hoạch → phiếu xuất bếp nháp → duyệt FEFO và kiểm kê; còn kiểm tra batch còn hạn, UX kế hoạch và đối soát cấp phát cuối ca. Một kho trung tâm |
| [M13](#m13-detail) | Thống kê & Báo cáo doanh thu | ⬜ | [M13-R1: Biểu đồ doanh thu ngày/tháng, món bán chạy nhất](#m13-r1) |
| [M14](#m14-detail) | Trợ lý tư vấn món bằng AI & Đánh giá | ✅ | Đã xong: Trợ lý AI Gemini tư vấn món ăn, trích xuất thẻ món, đánh giá 1-5 sao |
| [M15](#m15-detail) | Nhật ký hệ thống & Audit Log | ⬜ | [M15-R1: Ghi vết thay đổi dữ liệu quan trọng phục vụ bảo vệ đồ án](#m15-r1) |

---

## 2. Danh sách việc còn lại theo mức ưu tiên

<a id="p0-tasks"></a>
### Ưu tiên cao nhất (P0 - Critical Core)
*Các tính năng cốt lõi bắt buộc phải hoàn thành trước để hệ thống vận hành liên thông:*

- <a id="m06-r1"></a>**`M06-R1` [Order tại bàn - Backend & UI]**:
  - Tạo bảng/Entity `Orders`, `OrderTables`, `OrderItems` đầy đủ quan hệ.
  - Viết API `POST /api/staff/order/create`: Tạo Order trạng thái `open`, gán `TableId` và đổi trạng thái bàn sang `serving`.
  - Viết API `POST /api/staff/order/add-item`: Thêm món hoặc tăng số lượng, ghi chú (`Note`).
  - Giao diện chọn món trên POS/Mobile.

- <a id="m07-r1"></a>**`M07-R1` [Màn hình Bếp KDS - Tiếp nhận món]**:
  - Viết API `GET /api/chef/queue`: Lấy danh sách các `OrderItems` có trạng thái `confirmed`.
  - Giao diện màn hình Bếp hiển thị theo thẻ món, sắp xếp theo thời gian order tăng dần (món gọi trước làm trước).

- <a id="m07-r2"></a>**`M07-R2` [Cập nhật tiến độ chế biến]**:
  - ✅ API Chef chỉ chuyển `confirmed` → `cooking` → `ready`; ghi nhận `ChefId` và `ReadyAt`.
  - ✅ API Staff chuyển `ready` → `served` khi nhân viên thực tế đã mang món ra bàn.
  - 🔎 Cần kiểm thử đồng thời nhiều bếp/nhân viên trên cùng một order.

- <a id="m10-r1"></a>**`M10-R1` [Thanh toán & Đóng Order (Transaction)]**:
  - Viết API `POST /api/staff/payment/checkout`: Dùng DB Transaction:
    1. Tính `Subtotal` từ các `OrderItems`.
    2. Trừ giảm giá nếu có voucher/điểm.
    3. Tạo bản ghi `Invoices` + sao chép các dòng sang `InvoiceItems` (lưu snapshot `FoodNameSnapshot`, `VariantSnapshot`, `UnitPrice`).
    4. Tạo `Payments` (`cash`, `transfer`, `card`, `e_wallet`).
    5. Chuyển `Orders.OrderStatus = 'paid'`, giải phóng bàn về `empty` hoặc `cleaning`.

---

<a id="p1-tasks"></a>
### Ưu tiên kế tiếp (P1 - Operations)
*Các tính năng phục vụ hoàn thiện quy trình phục vụ và trải nghiệm khách hàng:*

- <a id="m06-r2"></a>**`M06-R2` [Chuyển bàn & Gộp bàn]**:
  - Chuyển toàn bộ order từ Bàn A sang Bàn B (nếu B trống).
  - Gộp Bàn A vào Bàn B: Cập nhật `Orders.MergedIntoOrderId`, giải phóng bàn A.
- <a id="m06-r3"></a>**`M06-R3` [Tạm tính & Hủy món]**:
  - Xem trước hóa đơn tạm tính tại bàn trước khi thanh toán.
  - ✅ Gộp/ghép bàn chỉ được thực hiện trong cùng `AreaName`; frontend tự khóa bàn khác khu vực và backend từ chối request trái quy tắc.
  - Cho phép hủy món khi món vẫn ở trạng thái `pending`.
- <a id="m08-r1"></a>**`M08-R1` [Thành viên & Tích điểm]**:
  - ✅ Tìm kiếm khách hàng theo số điện thoại khi order/thanh toán.
  - ✅ Tự động cộng/trừ điểm và lưu `PointTransactions`.
  - ✅ Cổng khách hàng xem/sửa hồ sơ, số điểm, lịch sử đặt bàn và hóa đơn của chính mình.
  - 🟡 Còn lại: đổi mật khẩu, quên mật khẩu, phân hạng thành viên và bảo mật mật khẩu.
- <a id="m11-r1"></a>**`M11-R1` [Đặt bàn trước (Reservations)]**:
  - ✅ Khách hàng chọn thời gian, số lượng khách và ghi chú; không chọn khu vực/bàn để tránh hứa chỗ khi nhà hàng chưa kiểm tra vận hành.
  - ✅ Khi đã đăng nhập, API lấy tên/SĐT/email từ JWT + hồ sơ `Customer`; payload không được phép đổi sang khách khác.
  - ✅ Admin/Staff xử lý tuần tự: xác nhận phiếu → phân bàn (`TableStatus = 'reserved'`) → check-in khi khách đến; không được bỏ qua bước.
  - ✅ Phân bàn kiểm tra bàn đang trống và sức chứa phải đủ số khách.
  - ✅ Khi phân nhiều bàn cho một phiếu đặt, các bàn bắt buộc cùng khu vực; không cho gộp bàn giữa các khu vực.
  - ✅ Khách chỉ hủy được phiếu `pending`/`confirmed` trước giờ đến; quyền sở hữu phiếu được kiểm tra theo tài khoản.
  - ✅ SignalR phát `ReservationUpdated`: admin nhận thông báo toàn cục, khách tự cập nhật lịch sử.
  - 🟡 Còn lại: liên kết Reservation - Order, đặt cọc, nhắc lịch và ràng buộc slot/sức chứa.
- <a id="m11-r2"></a>**`M11-R2` [Liên kết đặt bàn với order và đặt món trước]**:
  - Thêm quan hệ `Orders.ReservationId` hoặc bảng trung gian `ReservationOrders`.
  - Chỉ cho phép đặt món trước sau khi phiếu được xác nhận; dùng bảng `ReservationPreOrderItems`, không lưu danh sách món trong `Note`.
  - Ghi nhận trạng thái nhận món trước khi check-in để Staff/Bếp xử lý minh bạch.
- <a id="m11-r3"></a>**`M11-R3` [Lịch sử thông báo đặt bàn]**:
  - Tạo bảng `Notifications` để lưu thông báo theo người nhận, trạng thái đã đọc và thời điểm đọc.
  - Hiện tại toast/chuông admin chỉ tồn tại trong phiên trình duyệt đang mở.
- <a id="m03-r1"></a>**`M03-R1` [Upload hình ảnh món ăn]**:
  - Tích hợp Cloudinary API hoặc lưu trữ cục bộ để tải ảnh món ăn trực tiếp từ giao diện Admin.
- <a id="m01-r1"></a>**`M01-R1` [Nâng cấp bảo mật tài khoản]**:
  - Chuyển mật khẩu đang lưu thô sang BCrypt/PBKDF2 hoặc ASP.NET Identity; không được so sánh mật khẩu trực tiếp.
  - Bổ sung cơ chế Refresh Token JWT, đổi mật khẩu và quên mật khẩu qua Email 🔑.

---

<a id="p2-tasks"></a>
### Ưu tiên nâng cao (P2 - Advanced & Extension)
*Phân hệ nâng cao phục vụ báo cáo tốt nghiệp và quản lý toàn diện:*

- <a id="m13-r1"></a>**`M13-R1` [Dashboard & Báo cáo doanh thu]**:
  - Biểu đồ doanh thu theo ngày, tuần, tháng.
  - Thống kê top món ăn bán chạy nhất, khung giờ cao điểm.
  - Xuất báo cáo Excel / PDF.
- <a id="m12-r1"></a>**`M12-R1` [Quản lý Kho & Định lượng nguyên liệu]**:
  - Danh mục nguyên liệu tồn kho (`InventoryItems`), đơn vị tính.
  - Liên kết nguyên liệu vào món ăn (`FoodIngredients`).
  - Ghi nhận phiếu nhập/xuất/hoàn/hủy/điều chỉnh (`StockReceipts`, `StockReceiptDetails`, `StockMovements`).
  - Duyệt phiếu trong transaction `Serializable`, phân bổ lô theo FEFO/FIFO và rollback khi thiếu tồn.
  - Kiểm kê snapshot, đối chiếu `SystemQuantity`/`ActualQuantity`, tạo `audit_adjustment` theo từng batch.
  - Theo dõi `DailyPortionControls`, đồng bộ số suất `SoldPortions`, `SystemRemain`, `VariancePortions`; đây không phải sổ tiêu thụ nguyên liệu.
  - Cảnh báo khi nguyên liệu chạm ngưỡng tối thiểu (`MinLimit`).
  - Trạng thái: CRUD/BOM, nhập lô có duyệt, phiếu FEFO, kế hoạch xuất bếp và kiểm kê tồn kho tổng đã có; nhập lô tự tính hao hụt từ lượng thô và đối soát cấp phát cuối ca còn thiếu.
- <a id="m14-r2"></a>**`M14-R2` [Chatbot AI tư vấn món ăn]**:
  - Tích hợp Gemini API vào giao diện khách hàng.
  - Trợ lý hỏi đáp thực đơn, gợi ý món ăn theo khẩu vị (cay, ngọt, ăn chay, số lượng người).
- <a id="m15-r1"></a>**`M15-R1` [Audit Log hệ thống]**:
  - Ghi log tự động mỗi khi có thao tác nhạy cảm: xóa món, hủy order, sửa giá, khóa tài khoản.

---

## 3. Chi tiết nghiệp vụ & Use Case theo Actor

<a id="cụm-a"></a>
### Cụm A: Quản lý Thực đơn & Danh mục (Admin)
<a id="m02-detail"></a><a id="m03-detail"></a><a id="m04-detail"></a>

| Mã UC | Chức năng chi tiết | Bảng CSDL chính | Trạng thái |
|:---:|---|---|:---:|
| `UC-A01` | Thêm, sửa, xóa danh mục món ăn | `FoodCategories` | ✅ Xong |
| `UC-A02` | Thêm món ăn mới, phân loại danh mục | `Foods`, `FoodCategories` | ✅ Xong |
| `UC-A03` | Cấu hình biến thể món ăn (Size nhỏ, lớn, giá riêng) | `FoodVariants` | ✅ Xong |
| `UC-A04` | Tạm ngừng bán / Mở bán lại món ăn (Không mất biến thể) | `Foods.IsActive` | ✅ Xong |
| `UC-A05` | Tìm kiếm & lọc thực đơn theo danh mục, trạng thái | `Foods`, `FoodCategories` | ✅ Xong |
| `UC-A06` | Upload và cập nhật hình ảnh món ăn | `Foods.ImageUrl` | 🟡 Chờ Storage |

> **Quy tắc nghiệp vụ menu:**
> - Mỗi món ăn phải thuộc về ít nhất 1 danh mục hợp lệ.
> - Giá khởi điểm và giá từng biến thể không được là số âm (`>= 0`).
> - Không xóa cứng món ăn đã từng có trong lịch sử hóa đơn (chỉ dùng cờ `IsActive = false` để ẩn).

---

<a id="cụm-b"></a>
### Cụm B: Sơ đồ bàn & Khu vực (Admin / Staff)
<a id="m05-detail"></a>

| Mã UC | Chức năng chi tiết | Bảng CSDL chính | Trạng thái |
|:---:|---|---|:---:|
| `UC-B01` | Thêm bàn ăn mới (Mã bàn tự động nhận diện tiền tố `TB-` hoặc `B`, sinh số kế tiếp) | `DiningTables` | ✅ Xong |
| `UC-B02` | Phân khu vực bàn động linh hoạt (chọn nhanh 1 chạm, thêm phân khu mới tức thì) | `DiningTables.AreaName` | ✅ Xong |
| `UC-B03` | Cấu hình sức chứa bàn (2, 4, 6, 8, 10 người) với nút chọn nhanh 1 chạm | `DiningTables.Capacity` | ✅ Xong |
| `UC-B04` | Quản lý trạng thái vận hành thiết bị bàn: `🟢 Đang hoạt động`, `🟡 Đang sửa chữa`, `🔴 Ngừng sử dụng` (tách biệt hoàn toàn với trạng thái order live của nhà hàng) | `DiningTables.TableStatus`, `DiningTables.IsActive` | ✅ Xong |
| `UC-B05` | Xem sơ đồ bàn trực quan dạng thẻ POS & Thanh tab điều hướng phân khu kèm số lượng bàn thực tế | `DiningTables` | ✅ Xong |
| `UC-B06` | Khôi phục hoạt động bàn ngừng dùng qua `PATCH /api/admin/table/reactivate` | `DiningTables.IsActive` | ✅ Xong |
| `UC-B07` | Ràng buộc an toàn: Chặn xếp bàn/đặt bàn vào bàn đang sửa chữa (`maintenance`) | `DiningTables.TableStatus` | ✅ Xong |

> **Phân định rõ 2 tầng trạng thái của bàn ăn:**
> 1. **Tầng cấu hình & vận hành thiết bị (Admin Table Management):**
>    - `🟢 active (Đang hoạt động)`: Bàn sẵn sàng phục vụ khách.
>    - `🟡 maintenance (Đang sửa chữa / Bảo trì)`: Bàn hỏng hóc, sửa chữa; tự động chặn không cho đặt bàn hay mở order.
>    - `🔴 disabled (Ngừng sử dụng)`: Tạm ngừng sử dụng, ẩn khỏi sơ đồ tiếp nhận.
> 2. **Tầng nghiệp vụ gọi món theo thời gian thực (Staff POS / Order Live):**
>    ```text
>    🟢 empty (Trống) ──[Khách vào / Mở order]──> 🟡 serving (Đang phục vụ)
>           │                                                │
>    [Khách đặt trước]                                  [Thanh toán xong]
>           ▼                                                ▼
>    🔵 reserved (Đã đặt) ──────────> ⚪ cleaning (Đang dọn dẹp) ──> 🟢 empty
>    ```

---

<a id="cụm-c"></a>
### Cụm C: Quy trình Order tại bàn (Staff)
<a id="m06-detail"></a>

| Mã UC | Chức năng chi tiết | Bảng CSDL chính | Trạng thái |
|:---:|---|---|:---:|
| `UC-C01` | Chọn bàn trống và mở Order mới | `Orders`, `OrderTables` | ✅ Xong |
| `UC-C02` | Chọn món ăn, chọn biến thể size và số lượng | `OrderItems`, `FoodVariants` | ✅ Xong |
| `UC-C03` | Nhập ghi chú đặc biệt cho từng món (VD: ít cay, không hành) | `OrderItems.Note` | ✅ Xong |
| `UC-C04` | Xác nhận gửi món xuống Bếp (`pending` → `confirmed`) | `OrderItems.ItemStatus` | ✅ Xong |
| `UC-C05` | Xem bảng tạm tính tiền hiện tại của bàn | `Orders`, `OrderItems` | ✅ Xong |
| `UC-C06` | Đổi bàn / Chuyển bàn phục vụ cho khách | `OrderTables` | ✅ Xong |
| `UC-C07` | Gộp 2 bàn thành một hóa đơn thanh toán chung | `Orders.MergedIntoOrderId` | ✅ Xong |
| `UC-C08` | Hủy món ăn khi chưa chế biến | `OrderItems.ItemStatus` | ✅ Xong |

---

<a id="cụm-d"></a>
### Cụm D: Xử lý món tại Bếp (Chef KDS)
<a id="m07-detail"></a>

| Mã UC | Chức năng chi tiết | Bảng CSDL chính | Trạng thái |
|:---:|---|---|:---:|
| `UC-D01` | Màn hình hiển thị danh sách món cần nấu theo thứ tự order | `OrderItems` | ✅ Xong |
| `UC-D02` | Tiếp nhận món: Chuyển sang Đang chế biến (`cooking`) | `OrderItems.ItemStatus` | ✅ Xong |
| `UC-D03` | Hoàn tất món: Chuyển sang Đã nấu xong (`ready`) để phục vụ mang ra | `ItemStatus`, `ReadyAt` | ✅ Xong |
| `UC-D04` | Staff xác nhận đã phục vụ lên bàn (`ready` → `served`) | `OrderItems.ItemStatus` | ✅ Xong |
| `UC-D05` | Báo hết món tạm thời trên hệ thống | `Foods.FoodStatus` | ✅ Xong |

---

<a id="cụm-e"></a>
### Cụm E: Khách hàng, Thành viên & Đặt bàn (Customer)
<a id="m08-detail"></a><a id="m11-detail"></a>

| Mã UC | Chức năng chi tiết | Bảng CSDL chính | Trạng thái |
|:---:|---|---|:---:|
| `UC-E01` | Đăng ký / Đăng nhập tài khoản khách hàng, tạo/liên kết hồ sơ Customer | `Users`, `Roles`, `Customers` | 🟡 Core xong, còn bảo mật mật khẩu |
| `UC-E02` | Xem/sửa hồ sơ thành viên, số điểm tích lũy và lịch sử điểm | `Customers`, `PointTransactions` | ✅ Xong Core |
| `UC-E03` | Đặt/hủy bàn trực tuyến theo tài khoản, chọn ngày, giờ, số khách và ghi chú; nhà hàng chủ động phân khu vực/bàn | `Reservations` | ✅ Xong Core |
| `UC-E04` | Xem lịch sử hóa đơn và bill chi tiết của chính tài khoản | `Invoices`, `InvoiceItems` | ✅ Xong Core |
| `UC-E05` | Đánh giá sao (1-5★) và bình luận trải nghiệm món ăn | `Reviews` | ✅ Xong |
| `UC-E06` | Trò chuyện với trợ lý AI để nhận tư vấn thực đơn phù hợp | `AiConversations`, `AiMessages`| ✅ Xong |
| `UC-E07` | Đồng bộ realtime lịch đặt bàn/hóa đơn cho khách và thông báo đặt bàn toàn cục cho admin | `Reservations`, `Invoices`, SignalR | ✅ Xong Core |
| `UC-E08` | Điều phối & Xếp bàn thông minh: ưu tiên bàn đơn đúng sức chứa, chỉ gợi ý gộp bàn khi hết bàn đơn; ràng buộc an toàn bắt buộc các bàn gộp phải thuộc **cùng một phân khu** (`AreaName`); chặn bàn đang sửa chữa (`maintenance`) | `Reservations`, `DiningTables` | ✅ Xong Core |
| `UC-E09` | Giao diện Quản lý Đặt bàn UI/UX Pro: Bảng hiển thị desktop-first vừa vặn màn hình laptop/PC, không cuộn ngang, gỡ bỏ `sticky` triệt tiêu lỗi che khuất cột Trạng thái, nhãn rút gọn (`Chờ xếp bàn`), nút Xếp bàn pastel dịu nhẹ (`#eef2ff`), avatar tròn | Frontend Admin | ✅ Xong UI/UX |
| `UC-E10` | Bộ lọc đa chiều theo ngày hẹn (Hôm nay, Ngày mai, Tất cả ngày), tự động đồng bộ theo ngày chọn và hệ thống KPI metric cards realtime | Frontend Admin, API | ✅ Xong Core |

---

<a id="cụm-f"></a>
### Cụm F: Thanh toán & Xuất hóa đơn (Staff / Admin)
<a id="m09-detail"></a><a id="m10-detail"></a>

| Mã UC | Chức năng chi tiết | Bảng CSDL chính | Trạng thái |
|:---:|---|---|:---:|
| `UC-F01` | Kiểm tra tổng tiền order, tính tiền chiết khấu voucher | `Orders`, `Promotions` | ✅ Xong |
| `UC-F02` | Trừ điểm tích lũy thành viên vào số tiền thanh toán | `PointTransactions` | ✅ Xong |
| `UC-F03` | Tạo hóa đơn thanh toán chính thức (`Invoices`) | `Invoices` | ✅ Xong |
| `UC-F04` | Lưu snapshot tên món, size và giá tiền tại thời điểm xuất bill | `InvoiceItems` | ✅ Xong |
| `UC-F05` | Ghi nhận phương thức trả: Tiền mặt, Chuyển khoản, Thẻ, Ví điện tử | `Payments` | ✅ Xong |
| `UC-F06` | Tự động đóng order, cộng điểm mới và giải phóng bàn | `Orders`, `DiningTables` | ✅ Xong |

---

<a id="cụm-g"></a>
### Cụm G: Kho nguyên liệu & Định lượng (Admin / Chef)
<a id="m12-detail"></a>

| Mã UC | Chức năng chi tiết | Bảng CSDL chính | Trạng thái |
|:---:|---|---|:---:|
| `UC-G01` | Quản lý danh mục nhà cung cấp thực phẩm | `Suppliers` | ✅ Xong |
| `UC-G02` | Quản lý danh mục nguyên liệu tồn kho (Tên, đơn vị tính, giá nhập) | `InventoryItems` | ✅ Xong |
| `UC-G03` | Cấu hình công thức định lượng nguyên liệu cho món ăn | `FoodIngredients` | ✅ Xong |
| `UC-G04` | Tạo phiếu nhập kho / xuất kho / kiểm kê hủy kho | `StockReceipts`, `StockReceiptDetails`, `StockMovements` | 🟡 Backend đã code, cần test cạnh tranh/E2E |
| `UC-G05` | Báo động cảnh báo nguyên liệu sắp hết theo ngưỡng tồn tối thiểu | `InventoryItems.MinimumQuantity` | ✅ Xong |
| `UC-G06` | Duyệt phiếu và phân bổ tồn theo FEFO/FIFO, chống duyệt lặp | `InventoryBatches`, `StockMovements` | 🟡 Đã code backend, cần nghiệm thu concurrency |
| `UC-G07` | Kiểm kê snapshot và cân bằng tồn theo từng batch | `StockAudits`, `StockAuditItems`, `StockReceipts` | 🟡 Đã code backend, cần test snapshot cũ/movement phát sinh |
| `UC-G08` | Đồng bộ số suất món khi served/undo; không đại diện cho tiêu hao nguyên liệu | `DailyPortionControls`, `OrderItems` | 🟡 Backend đã có, cần kiểm thử transition đồng thời |
| `UC-G09` | Lập kế hoạch ngày, tổng hợp BOM theo món/biến thể và tạo phiếu xuất bếp nháp; duyệt FEFO mới ghi giảm kho | `DailyPortionControls`, `FoodIngredients`, `StockReceipts`, `InventoryBatches`, `StockMovements` | 🟡 Đã có luồng API/UI, snapshot BOM và kiểm tra lô còn hạn; còn E2E trình duyệt |
| `UC-G10` | Đối soát lượng cấp phát cuối ca: thực dùng, hoàn nguyên liệu còn tốt và xuất hủy; không trừ lại lượng đã xuất | `KitchenReconciliationLines`, `StockReceipts`, `StockReceiptDetails`, `StockMovements` | 🟡 API/UI/báo cáo đã test thành công/400/409; còn E2E trình duyệt và quyết định duyệt hai bước |
| `UC-G11` | Cảnh báo thiếu nguyên liệu theo kế hoạch từ tồn batch còn hạn và mức tồn tối thiểu | `DailyPortionControls`, `FoodIngredients`, `InventoryItems`, `InventoryBatches` | ✅ Đã kiểm tra batch còn hạn, tách tồn hết hạn và cảnh báo tồn sau kế hoạch; gợi ý mua bù là phần tối ưu tiếp theo |

---

<a id="cụm-h"></a>
### Cụm H: Thống kê & Báo cáo Doanh thu (Admin)
<a id="m13-detail"></a>

| Mã UC | Chức năng chi tiết | Bảng CSDL chính | Trạng thái |
|:---:|---|---|:---:|
| `UC-H01` | Biểu đồ doanh thu theo ngày / tuần / tháng / quý | `Invoices`, `Payments` | ⬜ Chưa làm |
| `UC-H02` | Thống kê số lượng hóa đơn và giá trị trung bình mỗi bàn | `Invoices` | ⬜ Chưa làm |
| `UC-H03` | Bảng xếp hạng top món ăn bán chạy nhất | `InvoiceItems` | ⬜ Chưa làm |
| `UC-H04` | Báo cáo hiệu quả các chương trình khuyến mãi | `PromotionUsages` | ⬜ Chưa làm |
| `UC-H05` | Xuất dữ liệu thống kê ra file Excel / PDF | `Reports` | ⬜ Chưa làm |

---

<a id="cụm-i"></a>
### Cụm I: Audit Log, Bảo mật & Giám sát
<a id="m00-detail"></a><a id="m01-detail"></a><a id="m15-detail"></a>

| Mã UC | Chức năng chi tiết | Bảng CSDL chính | Trạng thái |
|:---:|---|---|:---:|
| `UC-I01` | Phân quyền truy cập theo vai trò (Role-based Authorization) | `Roles`, `Users` | ✅ Xong Core |
| `UC-I02` | Khóa / Mở khóa tài khoản nhân viên vi phạm | `Users.IsActive` | ✅ Xong |
| `UC-I03` | Băm mật khẩu an toàn (Bcrypt/Argon2/PBKDF2) | `Users.PasswordHash` | ⬜ Chưa làm - ưu tiên trước golive |
| `UC-I04` | Ghi vết nhật ký thay đổi dữ liệu nhạy cảm (Ai làm, lúc nào, cái gì) | `AuditLogs` | ⬜ Chưa làm |
| `UC-I05` | Sao lưu định kỳ và khôi phục cơ sở dữ liệu | `System` | ⬜ Chưa làm |

---

## 4. Quy trình liên thông cốt lõi

### 4.1. Quy trình Order tại bàn & Chế biến tại Bếp
```text
[Nhân viên phục vụ]                                [Nhà Bếp (Chef KDS)]
        │                                                   │
        ├─ 1. Chọn Bàn trống (TableStatus = empty)          │
        ├─ 2. Tạo Order (OrderStatus = open)                │
        ├─ 3. Chuyển Bàn sang serving (Đang phục vụ)        │
        ├─ 4. Thêm món & biến thể (ItemStatus = pending)    │
        ├─ 5. Nhấn 'Xác nhận món' ─────────────────────────>│
        │     (ItemStatus chuyển sang confirmed)            ├─ 6. Bếp thấy món trên màn hình
        │                                                   ├─ 7. Bếp bấm 'Nấu' (cooking)
        │                                                   ├─ 8. Bếp bấm 'Xong' (ready)
        │<── 9. Nhân viên nhận thông báo món đã sẵn sàng ───┤
        ├─ 10. Bưng món ra bàn & cập nhật served            │
```

### 4.2. Quy trình Thanh toán, Tích điểm & Giải phóng bàn
```text
1. Khách yêu cầu tính tiền -> Nhân viên mở Order của bàn.
2. Hệ thống tổng hợp Subtotal từ tất cả các OrderItems.
3. Nhân viên nhập SĐT khách hàng -> Tìm thấy thành viên (nếu có).
4. Áp dụng mã Voucher khuyến mãi (nếu có) -> Kiểm tra hạn dùng & điều kiện.
5. Khách chọn đổi điểm thưởng tích lũy -> Giảm trừ trực tiếp vào tổng bill.
6. Mở DB Transaction:
   ├── Tạo bản ghi Invoice chính thức.
   ├── Sao chép toàn bộ món sang InvoiceItems (Lưu snapshot tên, size, đơn giá).
   ├── Tạo bản ghi Payments (Tiền mặt / Chuyển khoản QR / Thẻ).
   ├── Cộng điểm thưởng mới cho khách (PointTransactions).
   ├── Đánh dấu OrderStatus = 'paid'.
   └── Chuyển TableStatus = 'cleaning' (Đang dọn) hoặc 'empty' (Trống).
7. In hóa đơn gửi khách và hoàn tất.
```

---

### 4.3. Quy trình đặt bàn trực tuyến, realtime và lịch sử khách hàng
```text
[Khách chưa đăng nhập]
  Nhập tên + SĐT + email -> Tạo/tìm Customer theo SĐT -> Reservation = pending

[Khách đã đăng nhập]
  JWT UserId -> Customer.UserId -> Lấy tên/SĐT/email từ hồ sơ
  -> Tạo Reservation = pending (bỏ qua tên/SĐT/email do frontend gửi lên)

[SignalR ReservationUpdated]
  ├─ AdminLayout: hiện chuông + toast ở mọi màn hình quản trị
  ├─ ReservationManagement: tải lại danh sách/xếp bàn
  └─ CustomerAccount: tải lại lịch đặt bàn và trạng thái mới nhất

[Admin/Staff]
  pending
    -> xác nhận lịch (confirmed, chưa có bàn)
    -> phân bàn phù hợp (TableId, bàn = reserved)
    -> check-in khi khách đến (checked_in, bàn = serving)
    -> tự động mở order tại bàn

[Khách hàng]
  Có thể hủy pending/confirmed trước giờ đến
  -> cancelled; bàn reserved được giải phóng nếu không còn phiếu confirmed khác

[Thanh toán]
  Order paid -> Invoice + InvoiceItems snapshot + Payments + PointTransactions
  -> CustomerAccount làm mới lịch sử và hiển thị bill gồm món, đơn giá, giảm trừ,
     điểm, bàn, khu vực, thu ngân và phương thức thanh toán
```

> Chưa triển khai đặt món trước. Chỉ mở chức năng này sau khi bổ sung liên kết Reservation - Order và bảng `ReservationPreOrderItems`.

---

## 5. Đối chiếu Database & Ràng buộc toàn vẹn

| Tên bảng trong DB | Nhóm chức năng | Module tương ứng | Ràng buộc chính cần lưu ý |
|---|---|:---:|---|
| `Roles` | Phân quyền vai trò | `M01` | Mã vai trò: `manager`, `staff`, `chef`, `customer` |
| `Users` | Tài khoản đăng nhập | `M01` | Email là duy nhất; cần chuyển `PasswordHash` sang BCrypt/Identity trước golive |
| `FoodCategories`| Danh mục món ăn | `M02` | Tên danh mục không trùng lặp |
| `Foods` | Món ăn | `M03` | Giá tối thiểu `>= 0`, thuộc danh mục hợp lệ |
| `FoodVariants` | Biến thể / Size món | `M04` | Cặp `(FoodId, VariantName)` không được trùng lặp |
| `DiningTables` | Bàn ăn & Sơ đồ | `M05` | Trạng thái: `empty`, `serving`, `reserved`, `cleaning`, `disabled` |
| `Orders` | Phiếu order bàn | `M06` | Trạng thái: `open`, `preparing`, `serving`, `completed`, `paid`, `cancelled`, `merged` |
| `OrderTables` | Bàn gắn với order | `M06` | Hỗ trợ 1 order gán nhiều bàn khi khách ngồi đông |
| `OrderItems` | Món trong order | `M06/M07`| Số lượng `> 0`, trạng thái: `pending`, `confirmed`, `cooking`, `ready`, `served`, `cancelled` |
| `Invoices` | Hóa đơn thanh toán | `M10` | 1 Order chỉ có tối đa 1 Invoice duy nhất |
| `InvoiceItems` | Chi tiết hóa đơn | `M10` | Snapshot bất biến: Không thay đổi kể cả khi xóa món gốc |
| `Payments` | Giao dịch thanh toán | `M10` | Bắt buộc liên kết với 1 `InvoiceId` hợp lệ |
| `Customers` | Khách hàng thành viên | `M08` | Số điện thoại là duy nhất để tra cứu |
| `PointTransactions`| Lịch sử điểm | `M08` | Số điểm cộng/trừ minh bạch theo từng hóa đơn |
| `Promotions` | Mã giảm giá | `M09` | `EndAt > StartAt`, giá trị giảm `>= 0` |
| `PromotionUsages`| Lượt dùng voucher | `M09` | Giới hạn số lần dùng tối đa theo khách/chương trình |
| `Reservations` | Phiếu đặt bàn trước | `M11` | Số lượng khách `> 0`, thời gian đặt không ở quá khứ, thuộc đúng `CustomerId` khi khách đã đăng nhập |
| `InventoryItems`| Kho nguyên liệu | `M12` | Số lượng tồn kho `>= 0` |
| `FoodIngredients`| Định lượng món | `M12` | Liên kết công thức chế biến theo món |
| `StockMovements`| Xuất nhập kho | `M12` | Ghi vết loại: `import`, `export`, `waste`, `adjustment` |
| `Reviews` | Đánh giá món ăn | `M14` | Điểm đánh giá thang 1 đến 5 sao |
| `AiConversations`| Phiên chat AI | `M14` | Lưu vết phiên hỗ trợ khách hàng |
| `AuditLogs` | Nhật ký hệ thống | `M15` | Ghi vết bất biến để phục vụ báo cáo bảo vệ đồ án |

---

## 6. Kế hoạch triển khai theo giai đoạn (Phase 0 → Phase 9)

### 📌 Giai đoạn 0: Khảo sát & Chốt Kiến trúc
- [x] Sơ đồ Use Case tổng quát trong `qlNhaHang.drawio`.
- [x] Thiết kế Schema CSDL chuẩn hóa trên SQL Server (`restaurant_schema.sql`).
- [x] Bản đặc tả đề cương tốt nghiệp.
- [x] Thống nhất kiến trúc: Backend RESTful API (C# .NET) + Frontend Quản trị SPA (Vue 3).

### 📌 Giai đoạn 1: Nền tảng Backend & Cơ sở dữ liệu
- [x] Tạo cơ sở dữ liệu `RestaurantManagement` trên SQL Server.
- [x] Cấu hình DbContext, Entity Framework Core và các Check Constraint.
- [x] Seed dữ liệu Roles chuẩn (`manager`, `staff`, `chef`, `customer`).
- [x] API Đăng nhập (`POST /api/Auth/login`) trả JWT Token & phân vai trò.
- [x] Băm mật khẩu nhân viên an toàn khi tạo mới.

### 📌 Giai đoạn 2: Quản trị Thực đơn, Bàn & Nhân sự (Admin Web)
- [x] Module Quản lý Danh mục món ăn (`CategoryManagementView.vue`).
- [x] Module Quản lý Món ăn & Biến thể giá (`FoodManagementView.vue`).
- [x] Xử lý logic ngừng kinh doanh / mở bán lại không làm mất biến thể.
- [x] Module Sơ đồ bàn POS theo phân khu & trạng thái (`TableManagementView.vue`).
- [x] Module Cấp tài khoản nhân viên & Khóa/Mở khóa (`UserManagementView.vue`).
- [x] Dashboard thống kê thời gian thực & phím tắt nghiệp vụ (`AdminDashboardView.vue`).
- [ ] Tích hợp Upload ảnh món ăn trực tiếp lên cloud.

### 📌 Giai đoạn 3: Nghiệp vụ Order tại bàn (Staff POS / Mobile) ✅ [HOÀN THÀNH 100%]
- [x] API Tạo Order mở mới cho bàn trống (`POST /api/staff/orders/create`).
- [x] Giao diện chọn thực đơn, tùy chọn size và thêm ghi chú đặc biệt (`StaffOrderPosView.vue`).
- [x] Chức năng xác nhận gửi món xuống Bếp (`confirmed`) (`POST /api/staff/orders/{id}/confirm`).
- [x] Nghiệp vụ đổi bàn, chuyển bàn, gộp bàn cho nhóm khách lớn (`POST /api/staff/orders/merge-tables`).
- [x] Màn hình tạm tính hóa đơn tại bàn (xem trước bill đa bàn ghép, in phiếu tạm tính).

### 📌 Giai đoạn 4: Màn hình Bếp (Kitchen Display System - KDS) 🟡 [CORE ĐÃ HOÀN THÀNH]
- [x] Giao diện màn hình Bếp hiển thị theo luồng thẻ bàn Kanban và hàng đợi phẳng FIFO (`ChefWorkspaceView.vue`).
- [x] KDS Đa trạm 4 quầy bếp chuyên biệt (Khai vị, Món chính, Tráng miệng, Đồ uống) với bộ lọc danh mục và lưu bộ nhớ trạm làm việc.
- [x] Chef xử lý `confirmed` → `cooking` → `ready` kèm `ChefId` và `ReadyAt`; Staff xác nhận `ready` → `served`.
- [x] Tính năng Hoàn tất quầy (Ready All Station) và Thu hồi thao tác nhầm (Undo) danh sách hoàn thành gần nhất.
- [x] Tích hợp SignalR WebSocket cập nhật hàng đợi khi có món mới hoặc trạng thái món thay đổi.
- [x] Thông báo âm thanh chuông báo (Web Audio chime) và cảnh báo trễ thời gian chờ (Xanh, Vàng, Đỏ).
- [x] Giao diện sáng trực quan (Light Modern UI) tương phản cao, sạch đẹp, dễ nhìn, bỏ nền đen cũ.
- [x] Chức năng Bếp báo hết món nhanh (Sold Out Manager) đồng bộ thời gian thực sang POS phục vụ.
- [x] Tính năng gom mẻ nấu nhanh (Line Cook Summary) cho các món cùng loại.

### 📌 Giai đoạn 5: Thanh toán, Thành viên & Hóa đơn 🟡 [CORE ĐÃ HOÀN THÀNH]
- [x] Backend Controller `StaffPaymentController.cs`: Preview hóa đơn, tìm kiếm khách hàng, tạo nhanh khách thành viên, kiểm tra voucher.
- [x] Backend Transaction toàn vẹn: Tạo `Invoices`, snapshot bất biến `InvoiceItems`, thanh toán `Payments`, lưu `PromotionUsages`, lưu `PointTransactions`, tự động giải phóng bàn ăn (`empty`).
- [x] Tích hợp SignalR WebSocket phát sóng cập nhật khi thanh toán hoàn tất (giải phóng bàn ngay tức thì cho toàn bộ Staff và KDS).
- [x] Giao diện Modal Thanh toán POS chuyên nghiệp trên màn hình Phục vụ (`StaffWorkspaceView.vue`).
- [x] Tìm kiếm khách hàng bằng SĐT, tra cứu số điểm và tạo nhanh tài khoản thành viên ngay tại quầy.
- [x] Áp dụng mã Voucher giảm giá (% hoặc số tiền mặt) và trừ điểm thưởng tích lũy (1 điểm = 1.000 VNĐ).
- [x] Thanh toán đa phương thức (Tiền mặt có tính tiền thối, Mã QR chuyển khoản VietQR tự động, Quẹt thẻ, Ví điện tử).
- [x] Xem trước và In hóa đơn thanh toán K80 chính thức có thông tin thành viên và điểm tích lũy mới.
- [x] Quản trị cấu hình ngân hàng & Cổng Webhook tự động nhận tiền (`/api/payment/bank-webhook`), tích hợp SignalR tự động chuông Ting Ting và tự động in hóa đơn K80 không cần nhân viên bấm.
> 📌 **Ghi chú bảo lưu Golive:** Toàn bộ kiến trúc Backend Webhook đa cổng (SePay / PayOS / Casso), tự động đối soát, khớp hóa đơn, phát chuông SignalR và tự động in bill đã hoàn thiện 100%. Việc kết nối tài khoản ngân hàng thật (PayOS/SePay) qua thiết bị có hỗ trợ eKYC NFC sẽ kích hoạt khi golive thực tế, không ảnh hưởng đến toàn bộ luồng nghiệp vụ.

### 📌 Giai đoạn 6: Cổng khách hàng & Đặt bàn trực tuyến 🟡 [CORE ĐÃ HOÀN THÀNH]
- [x] Giao diện xem thực đơn, không gian, liên hệ và đặt bàn trực tuyến cho khách hàng.
- [x] Khách đăng nhập đặt bàn bằng thông tin tài khoản; khách vãng lai được phép nhập thông tin liên hệ.
- [x] Admin/Staff xử lý tuần tự: xác nhận phiếu → phân bàn đặt trước (`reserved`) → check-in; quản lý hủy/no-show.
- [x] Chặn phân bàn đang bận, bàn thiếu sức chứa và chặn check-in khi chưa phân bàn.
- [x] Khách xem/sửa hồ sơ, lịch đặt bàn, điểm thưởng và bill đầy đủ dữ liệu thanh toán.
- [x] SignalR đồng bộ thay đổi phiếu đặt bàn cho tài khoản khách; admin nhận chuông/toast ở mọi trang trong `AdminLayout`.
- [ ] Liên kết Reservation - Order - Invoice, đặt món trước sau khi xác nhận, đặt cọc và nhắc lịch.
- [x] Khách đánh giá dịch vụ/món ăn (1-5★) sau hóa đơn hợp lệ.
- [ ] Lưu thông báo đặt bàn lâu dài trong `Notifications` thay vì chỉ tồn tại trong phiên trình duyệt.

### 📌 Giai đoạn 7: Phân hệ Kho hàng, Định lượng & Trợ lý AI 🟡 [WMS CORE MỘT PHẦN]
- [x] Quản lý nhà cung cấp và tồn kho nguyên liệu (`InventoryItems`, `Suppliers`).
- [x] Cấu hình định lượng thành phần cho từng món ăn (`FoodIngredients` / Recipes).
- [x] Cảnh báo tồn kho dưới mức tối thiểu (`alerts`, visual badges).
- [x] Ghi nhận phiếu nhập/xuất/hoàn/hủy/điều chỉnh (`StockReceipts`, `StockReceiptDetails`, `StockMovements`).
- [x] Duyệt phiếu `Serializable`, phân bổ FEFO/FIFO, chống thiếu tồn và chống duyệt lặp.
- [x] Kiểm kê snapshot, cân bằng tồn và ghi movement theo từng batch.
- [x] Đồng bộ số suất `SoldPortions` khi served/undo; đây chỉ là theo dõi suất món, chưa ghi nhận lượng nguyên liệu BOM đã tiêu hao.
- [x] Màn hình Web quản lý nguyên liệu/BOM, batch, phiếu kho, kiểm kê và điều phối suất đã được xây dựng; cần nghiệm thu nghiệp vụ và luồng đầy đủ.
- [x] Kiểm tra đủ batch chưa hết hạn khi dự báo/lập phiếu; tính số lượng cần mua bù gồm mức tồn an toàn.
- [x] Hoàn thiện vận hành kế hoạch suất → tổng hợp BOM → phiếu xuất bếp nháp → duyệt FEFO; kế hoạch là căn cứ cấp phát, không tạo kho thứ hai. Phiếu xuất lưu snapshot BOM.
- [x] Kiểm tra đủ tồn theo batch còn hạn trước khi duyệt phiếu; UI thể hiện rõ thiếu hụt và không coi tồn lô hết hạn là khả dụng.
- [x] Ghi hao hụt sơ chế theo từng dòng phiếu nhập/lô (không gắn cố định vào BOM); khi duyệt phiếu, trừ hao hụt khỏi lượng nhập kho, điều chỉnh giá vốn trên lượng sạch và giữ nguyên tổng tiền mua. BOM chỉ định lượng phần nguyên liệu sạch; xuất kho không trừ hao hụt lần hai. Cần chạy `migration_wms_receipt_prep_wastage.sql`.
- [x] Giá trị tồn và giá bình quân trên màn kho tính từ lượng còn lại và giá vốn thực tế từng lô; cảnh báo khi tổng lượng theo lô khác tồn sổ. Seed cũ có giá sai đơn vị mua; đã sửa seed mới và có `repair_demo_seed_purchase_costs.sql` chỉ áp dụng cho các mã lô demo được liệt kê trong script.
- [x] Bổ sung đối soát thực cấp theo phiếu xuất/lô, hoàn nguyên liệu và xuất hủy cuối ca; không trừ kho lặp ở order/KDS. POST thành công/400/409 đã thử trên phiếu QA; còn E2E trình duyệt, thử đồng thời và xem xét duyệt hai bước.
- [ ] Chạy E2E qua trình duyệt Admin/Chef/Staff và kiểm thử đồng thời đầy đủ cho approve/hoàn/hủy; API smoke/concurrency đã có tại `docs/testing/wms_e2e_smoke.ps1`.
- [x] Trợ lý AI Gemini tư vấn món ăn thông minh cho khách hàng kèm fallback heuristic và thẻ món tương tác (`AiConversations`, `AiMessages`).
- [x] Hệ thống đánh giá 1-5 sao và trả lời phản hồi khách hàng (`Reviews`).

### 📌 Giai đoạn 8: Thống kê báo cáo & Tối ưu vận hành
- [ ] Báo cáo doanh thu theo mốc thời gian (ngày, tháng, năm).
- [ ] Biểu đồ trực quan hóa top món bán chạy và doanh thu theo khu vực.
- [ ] Ghi nhận nhật ký kiểm toán hệ thống (`AuditLogs`).
- [ ] Xuất file báo cáo tài chính Excel / PDF.

### 📌 Giai đoạn 9: Kiểm thử toàn diện & Hoàn thiện báo cáo
- [ ] Kiểm thử luồng liên thông End-to-End: Order → Bếp → Thanh toán → Báo cáo.
- [ ] Kiểm thử bảo mật phân quyền theo Role.
- [ ] Đóng gói hướng dẫn cài đặt, video demo và slide bảo vệ đồ án tốt nghiệp.

---

## 7. Tiêu chuẩn kiểm thử & Nghiệm thu E2E

### 7.1. Kịch bản E2E bắt buộc (Happy Path)
1. **Admin** đăng nhập → tạo danh mục 'Lẩu' → tạo món 'Lẩu Thái' với 2 size (Nhỏ: 150k, Lớn: 250k) → tạo Bàn A01 (Khu Tầng 1).
2. **Staff** đăng nhập → thấy Bàn A01 màu xanh (Trống) → mở Order bàn A01 → chọn 1 Lẩu Thái Size Lớn (ghi chú: 'Ít cay') → bấm Xác nhận món.
3. Bàn A01 chuyển sang màu vàng (Đang phục vụ).
4. **Chef** đăng nhập giao diện Bếp → thấy món Lẩu Thái của bàn A01 xuất hiện → bấm 'Bắt đầu nấu' → sau khi nấu xong bấm 'Đã xong'.
5. **Staff** nhận thông báo → mang món ra bàn → bấm 'Đã phục vụ'.
6. Khách yêu cầu tính tiền → **Staff** mở thanh toán → nhập SĐT thành viên → tích lũy điểm → chọn thanh toán 'Tiền mặt' → bấm Hoàn tất.
7. Hệ thống xuất hóa đơn thành công → Bàn A01 tự động giải phóng về trạng thái trống.
8. **Admin** vào trang Thống kê → thấy hóa đơn và doanh thu 250.000đ được ghi nhận tức thì.

### 7.2. Điều kiện không nghiệm thu (Non-Acceptance Criteria)
- ❌ Chỉ có giao diện tĩnh (Mock data) mà không có API Backend kết nối Database thật.
- ❌ Bị mất dữ liệu biến thể hoặc lỗi kẹt dữ liệu khi tắt mở kinh doanh món ăn.
- ❌ Cho phép tạo order mới trên bàn đang phục vụ hoặc bàn đang hỏng (`disabled`).
- ❌ Xóa món ăn làm biến mất các dòng lịch sử trong hóa đơn cũ đã xuất.
- ❌ Tài khoản vai trò Đầu bếp truy cập được vào trang Sửa giá / Cấp quyền của Admin.

