# Roadmap tiến độ hệ thống quản lý nhà hàng

> Phiên bản: v2.2  
> Cập nhật: 2026-10-15  
> Bản này giữ nguyên dữ liệu từ `roadmap_v2.md`, chỉ cải thiện cách theo dõi và điều hướng.

## Cách sử dụng nhanh

- Bấm vào mã module trong bảng tiến độ để mở phần chi tiết.
- Bấm `↑ Về bảng tiến độ` ở cuối mỗi module để quay lại.
- `[ ]` chưa làm · `[~]` đang làm/UI mới có · `[x]` đã code và test · `[!]` bị chặn.
- Chỉ đổi `[~]` hoặc `[x]` sau khi cập nhật đúng phần `Còn lại` và kết quả kiểm tra.

## Mục lục

- [Tổng quan tiến độ](#tong-quan-tien-do)
- [Tiến độ theo actor](#tien-do-theo-actor)
- [Các giai đoạn](#cac-giai-doan)
- [Quy trình liên thông](#quy-trinh-lien-thong)
- [Đối chiếu database](#doi-chieu-database)
- [Tiêu chí hoàn thành](#tieu-chi-hoan-thanh)

## Tổng quan tiến độ

| Mã | Module | Actor chính | Trạng thái | Còn lại |
|---|---|---|---|---|
| [M00](#m00-nen-tang) | Nền tảng, môi trường, cấu trúc | Tất cả | `[~]` | Env, logging, API contract |
| [M01](#m01-tai-khoan) | Tài khoản và phân quyền | Admin, tất cả | `[~]` | Hash mật khẩu, JWT, seed |
| [M02](#m02-danh-muc) | Danh mục món ăn | Admin | `[~]` | CRUD, soft delete, sắp xếp |
| [M03](#m03-mon-an) | Món ăn và hình ảnh | Admin | `[~]` | CRUD, upload, trạng thái |
| [M04](#m04-bien-the) | Biến thể/size món ăn | Admin, Staff | `[~]` | CRUD đồng bộ FoodVariants |
| [M05](#m05-ban-an) | Bàn và sơ đồ bàn | Admin, Staff | `[x]` | Đã hoàn thành (Sơ đồ bàn, phân khu vực động, chuẩn hóa 3 trạng thái vận hành thiết bị Hoạt động / Sửa chữa / Ngừng dùng, PATCH reactivate, gán order) |
| [M06](#m06-order) | Order tại bàn | Staff | `[x]` | Đã hoàn thành (Tạo order, biến thể, ghi chú, gộp/chuyển bàn, gửi bếp) |
| [M07](#m07-bep) | Hàng đợi bếp | Chef | `[x]` | Đã hoàn thành (KDS 4 quầy, luồng 1 chạm, SignalR 0s không giật màn hình, undo, chuông báo) |
| [M08](#m08-thanh-vien) | Thành viên và tích điểm | Admin, Staff, Customer | `[x]` | Đã hoàn thành (Tìm kiếm SĐT, tạo nhanh thành viên, tích/tiêu điểm, PointTransactions) |
| [M09](#m09-khuyen-mai) | Khuyến mãi | Admin, Staff | `[x]` | Đã hoàn thành (Áp dụng voucher % & tiền mặt, PromotionUsages) |
| [M10](#m10-thanh-toan) | Thanh toán và hóa đơn | Staff, Admin | `[x]` | Đã hoàn thành (POS đa phương thức, VietQR động, in bill K80, Webhook tự động, SignalR in tức thì). *Lưu ý: Đăng ký cổng thanh toán thực tế (PayOS/SePay) được ghi nhận bảo lưu kích hoạt khi golive thực tế do thiết bị eKYC cần NFC.* |
| [M11](#m11-dat-ban) | Đặt bàn | Customer, Staff, Admin | `[x]` | Đã hoàn thành (Đặt bàn online, xếp bàn thông minh theo số người, gộp bàn cùng khu vực, UI/UX Pro quản lý lịch hẹn, SignalR realtime, tài khoản khách) |
| [M12](#m12-kho) | Kho và nguyên liệu | Admin, Chef | `[~]` | Đã triển khai backend WMS/BOM, phiếu kho FEFO/FIFO, kiểm kê batch và đồng bộ suất; còn test cạnh tranh/E2E và UI |
| [M13](#m13-thong-ke) | Thống kê và báo cáo | Admin | `[ ]` | Doanh thu, món bán chạy, dashboard biểu đồ |
| [M14](#m14-danh-gia-ai) | Đánh giá và tư vấn AI | Customer, Admin | `[x]` | Đã hoàn thành (Trợ lý AI Gemini tư vấn món ăn, thẻ món tương tác, đánh giá 1-5 sao & phản hồi) |
| [M15](#m15-van-hanh) | Audit, bảo mật, vận hành | Admin | `[ ]` | Audit log, backup, monitoring |

### Chỉ số hiện tại

| Nhóm | Đã hoàn thành | Đang làm | Chưa làm |
|---|---:|---:|---:|
| Nền tảng và Admin cơ bản | 4 | 2 | 0 |
| Order và bếp | 2 | 0 | 0 |
| Thanh toán và khách hàng | 3 | 1 | 0 |
| Kho, báo cáo, vận hành | 0 | 0 | 4 |

## Tiến độ theo actor

### Admin/Quản lý

| Cụm chức năng | Module |
|---|---|
| Tài khoản, phân quyền | [M01](#m01-tai-khoan) |
| Danh mục, món, biến thể, hình ảnh | [M02](#m02-danh-muc), [M03](#m03-mon-an), [M04](#m04-bien-the) |
| Bàn và sơ đồ bàn | [M05](#m05-ban-an) |
| Thành viên, khuyến mãi | [M08](#m08-thanh-vien), [M09](#m09-khuyen-mai) |
| Kho nguyên liệu | [M12](#m12-kho) |
| Hóa đơn và thống kê | [M10](#m10-thanh-toan), [M13](#m13-thong-ke) |

### Nhân viên phục vụ

| Cụm chức năng | Module |
|---|---|
| Xem bàn, tạo order | [M05](#m05-ban-an), [M06](#m06-order) |
| Thêm món, chọn size, ghi chú | [M04](#m04-bien-the), [M06](#m06-order) |
| Xác nhận món, theo dõi bếp | [M06](#m06-order), [M07](#m07-bep) |
| Thành viên, điểm, thanh toán | [M08](#m08-thanh-vien), [M10](#m10-thanh-toan) |

### Đầu bếp

| Cụm chức năng | Module |
|---|---|
| Xem món chờ làm | [M07](#m07-bep) |
| Cập nhật tiến độ món | [M07](#m07-bep) |
| Báo hết món, theo dõi nguyên liệu | [M07](#m07-bep), [M12](#m12-kho) |

### Thành viên/Khách hàng

| Cụm chức năng | Module |
|---|---|
| Đăng ký, đăng nhập, hồ sơ | [M01](#m01-tai-khoan), [M08](#m08-thanh-vien) |
| Xem thực đơn, tư vấn món | [M03](#m03-mon-an), [M14](#m14-danh-gia-ai) |
| Đặt bàn | [M11](#m11-dat-ban) |
| Lịch sử hóa đơn, điểm, đánh giá | [M08](#m08-thanh-vien), [M10](#m10-thanh-toan), [M14](#m14-danh-gia-ai) |

## Các giai đoạn

| Giai đoạn | Mục tiêu | Module | Trạng thái |
|---|---|---|:---:|
| GĐ0 | Chốt kiến trúc, tài liệu, môi trường | M00 | `[x]` |
| GĐ1 | Database, Backend foundation, auth | M01 | `[x]` |
| GĐ2 | Admin menu và bàn | M02-M05 | `[x]` |
| GĐ3 | Staff order theo bàn | M06 | `[x]` |
| GĐ4 | Màn hình bếp (KDS Đa trạm, Realtime 0s) | M07 | `[x]` |
| GĐ5 | Thanh toán, thành viên, hóa đơn | M08-M10 | `[x]` |
| GĐ6 | Đặt bàn và customer web | M11 | `[x]` |
| GĐ7 | Kho, định lượng, đánh giá, AI | M12, M14 | `[~]` |
| GĐ8 | Thống kê và vận hành | M13, M15 | `[ ]` (Giai đoạn tiếp theo) |
| GĐ9 | Kiểm thử và báo cáo tốt nghiệp | Tất cả | `[ ]` |

## Chi tiết module

### M00 - Nền tảng

**Mục tiêu:** tất cả thành viên dùng chung cấu trúc và quy ước.

- [ ] Chốt frontend Web, Mobile và Backend.
- [ ] Chốt SQL Server là nguồn dữ liệu chính.
- [ ] Tách cấu hình dev/test/prod.
- [ ] Chuẩn hóa endpoint, response, mã lỗi và trạng thái.
- [ ] Có Health API, logging và xử lý exception chung.
- [ ] Có README hướng dẫn chạy.

**Phụ thuộc:** không có.  
**Hoàn thành khi:** project chạy được từ hướng dẫn và API health trả thành công.

<p><a id="m01-tai-khoan"></a></p>
### M01 - Tài khoản và phân quyền

- [ ] Login, logout, register.
- [ ] Hash mật khẩu bằng BCrypt/Argon2.
- [ ] JWT/session và refresh hoặc thời hạn token.
- [ ] Seed role: `manager`, `staff`, `chef`, `customer`.
- [ ] Route guard ở Web/Mobile.
- [ ] API authorize theo role.
- [ ] Khóa/mở tài khoản và cập nhật hồ sơ.

**Bảng:** `Roles`, `Users`, `AuditLogs`.  
**Còn lại quan trọng:** không lưu mật khẩu plaintext trong `PasswordHash`.

<p><a id="m02-danh-muc"></a></p>
### M02 - Danh mục

- [ ] Thêm, sửa, ẩn/hiện danh mục.
- [ ] Sắp xếp danh mục bằng `SortOrder`.
- [ ] Tìm kiếm danh mục.
- [ ] Không xóa cứng danh mục đang có món.

**Bảng:** `FoodCategories`.  
**Phụ thuộc:** M01.

<p><a id="m03-mon-an"></a></p>
### M03 - Món ăn và hình ảnh

- [ ] Thêm, sửa, ngừng bán món.
- [ ] Lưu `ImageUrl` sau khi upload.
- [ ] Hiển thị ảnh `BoxFit.contain`/full hình trong menu.
- [ ] Lọc theo danh mục và `FoodStatus`.
- [ ] Kiểm tra tên, giá và danh mục bắt buộc.
- [ ] Không xóa cứng món đã xuất hiện trong order/hoá đơn.

**Bảng:** `Foods`.  
**Trạng thái:** `available`, `unavailable`, `sold_out`, `hidden`.

<p><a id="m04-bien-the"></a></p>
### M04 - Biến thể/size

- [ ] Có sẵn một dòng biến thể khi thêm món.
- [ ] Thêm nhiều biến thể.
- [ ] Xóa dòng dư trước khi lưu.
- [ ] Sửa tên, giá, đơn vị, thứ tự.
- [ ] Không trùng `(FoodId, VariantName)`.
- [ ] Chỉ hiển thị biến thể đang active khi Staff gọi món.

**Bảng:** `FoodVariants`.  
**Phụ thuộc:** M03.

<p><a id="m05-ban-an"></a></p>
### M05 - Bàn và sơ đồ bàn

- [x] Thêm/sửa/ngừng sử dụng bàn (kèm khôi phục lại qua `PATCH /api/admin/table/reactivate`).
- [x] Phân khu vực bàn động linh hoạt, chọn nhanh 1 chạm, hỗ trợ thêm phân khu mới tức thì.
- [x] Cập nhật sức chứa bàn (nút bấm chọn nhanh 2, 4, 6, 8, 10 chỗ).
- [x] Chuẩn hóa 3 trạng thái vận hành thiết bị bàn: 🟢 Hoạt động, 🟡 Đang sửa chữa (bảo trì), 🔴 Ngừng sử dụng (tách bạch hoàn toàn với trạng thái order live của nhà hàng).
- [x] Hiển thị sơ đồ bàn dạng POS card trực quan, thanh điều hướng tab phân khu kèm số lượng bàn thực tế.
- [x] Tự động chặn xếp bàn hoặc mở order trên bàn đang sửa chữa (`maintenance`) hoặc ngừng dùng (`disabled`).
- [x] Gán bàn vào order qua `OrderTables`.

**Bảng:** `DiningTables`, `OrderTables`.  
**Phụ thuộc:** M01.

<p><a id="m06-order"></a></p>
### M06 - Order tại bàn

- [x] Chọn bàn trống và tạo `Orders`.
- [x] Thêm món, biến thể, số lượng và ghi chú.
- [x] Tăng/giảm/xóa món trước khi xác nhận.
- [x] Xác nhận món mới cho bếp (`confirmed`).
- [x] Thêm món phát sinh vào order đang mở.
- [x] Chuyển bàn và gộp bàn (`merge-tables`).
- [x] Tính và hiển thị tạm tính (in phiếu tạm tính).
- [x] Hủy order theo đúng điều kiện.

**Bảng:** `Orders`, `OrderTables`, `OrderItems`.  
**Phụ thuộc:** M03, M04, M05.

<p><a id="m07-bep"></a></p>
### M07 - Hàng đợi bếp

- [x] Xem món `confirmed`.
- [x] Chuyển `cooking`.
- [x] Chuyển `ready` và ghi `ReadyAt`.
- [x] Chuyển `served` khi Staff phục vụ.
- [x] Ghi `ChefId`, người thao tác và thời gian.
- [x] Báo hết món/tạm ngừng món (`Foods.IsActive`, `FoodStatus`).
- [x] Lọc theo 4 quầy bếp (Khai vị, Món chính, Tráng miệng, Đồ uống), bàn và trạng thái.
- [x] Luồng 1 chạm (1-Touch Station Clear & Item Transition) + Thu hồi (Undo).
- [x] Tích hợp SignalR WebSocket 0s realtime không giật lag màn hình (`fetchKitchenDataSilently()`).
- [x] Chuông báo âm thanh Web Audio (món mới / hoàn tất món).

**Bảng:** `OrderItems`, `Foods`, `Users`.  
**Phụ thuộc:** M06.

<p><a id="m08-thanh-vien"></a></p>
### M08 - Thành viên và tích điểm

- [x] Tìm khách theo số điện thoại / họ tên.
- [x] Thêm nhanh thành viên lúc thanh toán (đã fix filtered index `UQ_Customers_User`).
- [x] Hiển thị `PointsBalance`, `TotalSpent`, `VisitCount`.
- [x] Cộng điểm sau hóa đơn thanh toán (1 điểm / 10.000đ).
- [x] Trừ điểm khi khách sử dụng (1 điểm = 1.000đ).
- [x] Lưu mọi biến động tại `PointTransactions`.

**Bảng:** `Customers`, `PointTransactions`.  
**Phụ thuộc:** M01, M10.

<p><a id="m09-khuyen-mai"></a></p>
### M09 - Khuyến mãi

- [x] Tạo/sửa/ẩn chương trình khuyến mãi.
- [x] Hỗ trợ giảm giá theo % (`percent`) và số tiền mặt cố định (`fixed`).
- [x] Kiểm tra thời gian hiệu lực và giá trị đơn tối thiểu.
- [x] Kiểm tra giới hạn sử dụng.
- [x] Lưu lịch sử sử dụng tại `PromotionUsages`.

**Bảng:** `Promotions`, `PromotionUsages`.  
**Phụ thuộc:** M10.

<p><a id="m10-thanh-toan"></a></p>
### M10 - Thanh toán và hoá đơn

- [x] Kiểm tra order và các món trước thanh toán (Checkout Preview).
- [x] Tính subtotal, voucher discount, điểm dùng và tổng thanh toán.
- [x] Tạo `Invoices` duy nhất cho một order (`INV-yyyyMMdd-xxxx`).
- [x] Sao chép snapshot bất biến dòng món sang `InvoiceItems`.
- [x] Lưu `FoodNameSnapshot`, `VariantSnapshot`, `UnitPrice`.
- [x] Tạo `Payments`: Tiền mặt (tính tiền thối), Chuyển khoản VietQR động chuẩn Napas 247, Thẻ POS, Ví điện tử.
- [x] Dùng Database Transaction an toàn cho toàn bộ thao tác thanh toán.
- [x] Đóng order (`paid`) và tự động giải phóng bàn (`empty`).
- [x] Tích hợp SignalR WebSocket phát sóng cập nhật bàn tức thì 0s.
- [x] Mẫu in Hóa đơn K80 chuyên nghiệp với định dạng in nhiệt `@media print`.
- [x] Quản lý cấu hình tài khoản ngân hàng và Webhook dành cho Quản trị viên (Admin) tại `/admin/payment-settings`.
- [x] Cổng Webhook Ngân hàng tự động nhận kết quả thanh toán `POST /api/payment/bank-webhook`, tự động đối soát mã `ORDxxxx` và số tiền.
- [x] Realtime SignalR `PaymentReceived`: Tự động nổ chuông "Ting Ting" và mở ngay Hóa đơn K80 trên màn hình phục vụ khi khách chuyển khoản thành công.
- [x] Bộ công cụ Webhook Simulator trực tiếp trên giao diện Admin và Staff để kiểm thử nhận tiền tức thì mà không cần chuyển tiền thật.
> 📌 **Ghi chú bảo lưu Golive:** Toàn bộ kiến trúc Backend Webhook đa cổng (SePay / PayOS / Casso), tự động đối soát, khớp hóa đơn, phát chuông SignalR và tự động in bill đã hoàn thiện 100%. Việc kết nối tài khoản ngân hàng thật (PayOS/SePay) qua thiết bị có hỗ trợ eKYC NFC sẽ kích hoạt khi golive thực tế, không ảnh hưởng đến toàn bộ luồng nghiệp vụ.

**Bảng:** `Invoices`, `InvoiceItems`, `Payments`.  
**Phụ thuộc:** M06, M08, M09.

<p><a id="m11-dat-ban"></a></p>
### M11 - Đặt bàn

- [x] Customer chọn ngày, giờ, số người, ghi chú đặc biệt qua Customer Portal.
- [x] Tự động kiểm tra và gợi ý bàn đơn phù hợp sức chứa trước; hết bàn mới gợi ý gộp bàn cùng phân khu (`AreaName`).
- [x] Tạo `pending` reservation với SignalR thông báo tức thì đến toàn bộ màn hình Admin.
- [x] Staff/Admin tiếp nhận, duyệt hoặc từ chối phiếu đặt.
- [x] Xếp bàn thông minh, hỗ trợ gộp bàn an toàn chung phân khu, chặn xếp vào bàn đang sửa chữa (`maintenance`).
- [x] Quản lý lịch hẹn đặt bàn UI/UX Pro dạng bảng desktop-first (không cuộn ngang, nhãn rút gọn, nút pastel, avatar tròn, lọc realtime theo ngày).
- [x] Check-in đón khách vào bàn, cập nhật sơ đồ bàn realtime và chuyển trạng thái completed, cancelled, no-show.
- [x] Tự động liên kết và đồng bộ trạng thái bàn khi check-in.

**Bảng:** `Reservations`, `Customers`, `DiningTables`.  
**Phụ thuộc:** M05, M08.

<p><a id="m12-kho"></a></p>
### M12 - Kho và nguyên liệu

- [x] Quản lý nhà cung cấp.
- [x] Quản lý nguyên liệu, đơn vị BaseUnit/ PurchaseUnit và mức tồn tối thiểu.
- [x] Chuẩn hóa giá vốn theo PurchaseUnit; thành tiền/BOM quy đổi từ BaseUnit bằng ConversionRate.
- [x] Gắn định lượng theo Food/Variant qua `FoodIngredients`.
- [x] Nhập, xuất bếp, hoàn kho, xuất hủy và điều chỉnh qua `StockReceipts`/`StockMovements`.
- [x] Duyệt phiếu transaction `Serializable`, phân bổ FEFO/FIFO và chống duyệt lặp.
- [x] Kiểm kê snapshot, cân bằng tồn theo từng batch và tạo `audit_adjustment`.
- [x] Đồng bộ `SoldPortions`, `SystemRemain`, `VariancePortions` khi món served/undo.
- [x] Cảnh báo dưới mức tối thiểu.
- [x] Truy vết người tạo biến động.
- [ ] Test cạnh tranh, rollback, snapshot cũ và E2E Admin/Chef/Staff.
- [ ] Hoàn thiện UI quản lý phiếu kho, kiểm kê và suất bán.

**Bảng:** `Suppliers`, `InventoryItems`, `FoodIngredients`, `InventoryBatches`, `StockReceipts`, `StockReceiptDetails`, `StockMovements`, `StockAudits`, `StockAuditItems`, `DailyPortionControls`.  

<p><a id="m13-thong-ke"></a></p>
### M13 - Thống kê và báo cáo

- [ ] Doanh thu theo ngày, tuần, tháng, khoảng ngày.
- [ ] Số hoá đơn và order đã thanh toán.
- [ ] Món bán chạy theo số lượng/doanh thu.
- [ ] Doanh thu theo danh mục.
- [ ] Bàn đang phục vụ.
- [ ] Điểm và khuyến mãi đã dùng.
- [ ] Xuất dữ liệu nếu cần.

**Nguồn:** `Invoices`, `InvoiceItems`, `Payments`, `Orders`, `DiningTables`.

<p><a id="m14-danh-gia-ai"></a></p>
### M14 - Đánh giá và tư vấn AI

- [ ] Customer đánh giá 1-5 sao sau khi thanh toán.
- [ ] Admin xem/ẩn/trả lời đánh giá.
- [ ] Tạo cuộc trò chuyện tư vấn món.
- [ ] Lưu tin nhắn Customer/AI.
- [ ] Lưu món được đề xuất trong `RecommendedFoodIds`.

**Bảng:** `Reviews`, `AiConversations`, `AiMessages`.

<p><a id="m15-van-hanh"></a></p>
### M15 - Audit, bảo mật và vận hành

- [ ] Ghi `AuditLogs` cho thao tác quản trị quan trọng.
- [ ] Không ghi mật khẩu/token vào log.
- [ ] Backup và khôi phục database.
- [ ] Cấu hình CORS theo môi trường.
- [ ] Giới hạn quyền endpoint theo role.
- [ ] Theo dõi lỗi API và tình trạng database.

<p><a id="tong-quan-tien-do"></a></p>
**[↑ Về bảng tiến độ](#tong-quan-tien-do)**

## Quy trình liên thông

### Order tại bàn → Bếp → Phục vụ

```text
Staff chọn bàn empty
  → tạo Orders(open) và OrderTables
  → thêm OrderItems(pending)
  → xác nhận món(confirmed)
  → Chef nhận món(cooking)
  → Chef hoàn tất(ready)
  → Staff phục vụ(served)
```

### Thanh toán

```text
Order open/serving
  → tính subtotal
  → tìm Customer
  → áp dụng Promotion và điểm
  → tạo Invoice + InvoiceItems snapshot
  → tạo Payment
  → cập nhật PointTransactions
  → Order paid
  → bàn empty
```

### Đặt bàn

```text
Customer tạo Reservation(pending)
  → Admin/Staff xác nhận(confirmed)
  → khách đến(checked_in)
  → gán vào Order
  → completed hoặc cancelled/no_show
```

## Đối chiếu database

| Nhóm | Bảng |
|---|---|
| Tài khoản | `Roles`, `Users`, `AuditLogs` |
| Menu | `FoodCategories`, `Foods`, `FoodVariants` |
| Bàn/khách/order | `Customers`, `DiningTables`, `Reservations`, `Orders`, `OrderTables`, `OrderItems` |
| Thanh toán | `Promotions`, `PromotionUsages`, `Invoices`, `InvoiceItems`, `Payments`, `PointTransactions` |
| Kho | `Suppliers`, `InventoryItems`, `FoodIngredients`, `StockMovements` |
| Mở rộng | `Reviews`, `AiConversations`, `AiMessages` |

### Ràng buộc nghiệp vụ bắt buộc

- `FoodVariants` không trùng `(FoodId, VariantName)`.
- Số lượng order và invoice phải lớn hơn 0.
- Một order chỉ có tối đa một invoice.
- Một payment phải thuộc một invoice.
- `Reservation.GuestCount > 0`.
- `Promotion.EndAt > StartAt`.
- Rating nằm trong khoảng 1 đến 5.
- Không xóa cứng dữ liệu đã có lịch sử order/hóa đơn.

## Tiêu chí hoàn thành

### Checklist cho mỗi module

- [ ] Có table/model và khóa liên kết.
- [ ] Có DTO, API và validate.
- [ ] Logic nghiệp vụ nằm ở service/repository, không dồn vào view.
- [ ] View đúng actor và route có phân quyền.
- [ ] Có loading, empty, error, success state.
- [ ] Test cả trường hợp đúng và sai.
- [ ] Cập nhật trạng thái module ở bảng đầu file.

### Kịch bản E2E bắt buộc

1. Admin tạo danh mục, món, biến thể và bàn.
2. Staff chọn bàn, tạo order, thêm món và xác nhận.
3. Chef nhận món, chuyển cooking và ready.
4. Staff mở thanh toán, chọn member và dùng điểm.
5. Hệ thống tạo invoice, payment, point transaction và giải phóng bàn.
6. Admin xem doanh thu, hóa đơn và món bán chạy.
7. Customer đăng nhập, đặt bàn, xem lịch sử và đánh giá.

## Cập nhật gần nhất

- 2026-10-03: Tạo bản theo dõi tiến độ có liên kết module, phân chia actor/giai đoạn và checklist chi tiết.

**[↑ Về đầu trang](#tong-quan-tien-do)**
