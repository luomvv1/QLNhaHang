# Roadmap - He thong quan ly order mon an theo ban

> Phien ban: v2.0  
> Ngay cap nhat: 2026-10-03  
> Tai lieu doi chieu: `docs/database/21_VoVanLuom.docx`, `docs/database/restaurant_schema.sql`, `docs/database/qlNhaHang.drawio`.

## Muc luc

1. [Pham vi he thong](#1-pham-vi-he-thong)
2. [Actor va quyen han](#2-actor-va-quyen-han)
3. [Danh sach module va tien do](#3-danh-sach-module-va-tien-do)
4. [Nghiep vu theo actor](#4-nghiep-vu-theo-actor)
5. [Quy trinh lien thong quan trong](#5-quy-trinh-lien-thong-quan-trong)
6. [Ke hoach theo giai doan](#6-ke-hoach-theo-giai-doan)
7. [Doi chieu database](#7-doi-chieu-database)
8. [Kiem thu va tieu chi hoan thanh](#8-kiem-thu-va-tieu-chi-hoan-thanh)
9. [Nguyen tac cap nhat roadmap](#9-nguyen-tac-cap-nhat-roadmap)

## Ky hieu trang thai

- `[ ]` Chua lam.
- `[~]` Dang lam hoac moi co giao dien.
- `[x]` Da lam va da kiem tra ca UI, API va database.
- `[!]` Dang bi chan boi credential, dich vu ngoai hoac quyet dinh kien truc.

## 1. Pham vi he thong

He thong quan ly nha hang theo ban gom Web cho quan ly, bep va khach hang;
Mobile cho nhan vien phuc vu; Backend API va SQL Server lam lop du lieu trung tam.

Muc tieu nghiep vu:

- Quan ly menu, danh muc, bien the va hinh anh mon an.
- Quan ly ban, khu vuc, suc chua va trang thai ban.
- Tao order tai ban, them mon, xac nhan mon va theo doi tien do.
- Ket noi order voi man hinh bep.
- Thanh toan, ap dung khuyen mai, diem thanh vien va tao hoa don.
- Cho phep khach dat ban, xem menu, lich su hoa don va danh gia dich vu.
- Quan ly kho nguyen lieu, nha cung cap va canh bao ton kho.
- Thong ke doanh thu, hoa don, mon ban chay va hieu qua van hanh.
- Ghi audit log de phuc vu truy vet va bao cao tot nghiep.

## 2. Actor va quyen han

### 2.1. Admin/Quan ly

Quan ly mon an, bien the, danh muc, ban, nhan vien, thanh vien, kho, khuyen
mai, hoa don, thong ke va cau hinh he thong.

### 2.2. Nhan vien phuc vu

Xem so do ban, tao va cap nhat order, chon mon/bien the, chuyen ban, gop ban,
xac nhan mon, tim thanh vien va thuc hien thanh toan.

### 2.3. Dau bep

Xem order da xac nhan, loc mon theo trang thai, cap nhat dang lam/hoan tat,
bao het mon va theo doi order theo ban.

### 2.4. Thanh vien/Khach hang

Dang ky, dang nhap, xem thuc don, tro chuyen tu van mon, dat ban, xem lich su
hoa don/dat ban, xem va su dung diem tich luy, danh gia dich vu.

## 3. Danh sach module va tien do

Bang nay la noi theo doi tien do cap cao. Chi danh dau `[x]` sau khi da test.

| Ma | Module | Actor chinh | Trang thai | Con lai/ket qua can dat |
|---|---|---|---|---|
| M00 | Nen tang, moi truong va cau truc | Tat ca | `[~]` | Chot Web/Mobile/API, env, logging |
| M01 | Tai khoan va phan quyen | Admin, tat ca | `[~]` | Hash mat khau, JWT, role guard, seed |
| M02 | Danh muc mon an | Admin | `[~]` | CRUD, soft delete, sap xep, validate |
| M03 | Mon an va hinh anh | Admin | `[~]` | CRUD, upload, URL, trang thai mon |
| M04 | Bien the/size mon an | Admin, Staff | `[~]` | CRUD dong bo voi FoodVariants |
| M05 | Ban va so do ban | Admin, Staff | `[~]` | CRUD, loc, trang thai, gan order |
| M06 | Order tai ban | Staff | `[ ]` | Tao order, them mon, gop/chuyen ban |
| M07 | Hang doi bep | Chef | `[ ]` | Confirmed -> cooking -> ready -> served |
| M08 | Thanh vien va tich diem | Admin, Staff, Customer | `[ ]` | Customer, PointTransactions |
| M09 | Khuyen mai | Admin, Staff | `[ ]` | Promotion, usage, tinh discount |
| M10 | Thanh toan va hoa don | Staff, Admin | `[ ]` | Invoice, InvoiceItems, Payments |
| M11 | Dat ban | Customer, Staff, Admin | `[ ]` | Reservation va check-in |
| M12 | Kho va nguyen lieu | Admin, Chef | `[ ]` | Inventory, supplier, stock movement |
| M13 | Thong ke va bao cao | Admin | `[ ]` | Doanh thu, mon ban chay, loc thoi gian |
| M14 | Danh gia va tu van AI | Customer, Admin | `[ ]` | Reviews, AiConversations, AiMessages |
| M15 | Audit, bao mat va van hanh | Admin | `[ ]` | AuditLog, backup, monitoring |

### 3.1. Viec con lai uu tien cao

- `R01`: Tao API order va order item truoc khi lam man hinh bep.
- `R02`: Chuan hoa trang thai giua database, API va giao dien.
- `R03`: Hash mat khau; khong tiep tuc luu plaintext trong `PasswordHash`.
- `R04`: Luu snapshot ten mon, bien the va gia tai thoi diem thanh toan.
- `R05`: Dung transaction khi thanh toan de tranh tao hoa don nua chung.
- `R06`: Tao seed tai khoan theo tung role de test end-to-end.

## 4. Nghiep vu theo actor

### 4.1. Cum A - Quan ly menu

| Ma use case | Chuc nang | Bang chinh | Phu thuoc |
|---|---|---|---|
| UC-A01 | Them/sua/xoa danh muc | `FoodCategories` | M01 |
| UC-A02 | Them/sua/an mon an | `Foods` | UC-A01 |
| UC-A03 | Quan ly bien the/size | `FoodVariants` | UC-A02 |
| UC-A04 | Upload va thay hinh anh | `Foods.ImageUrl` | Cloudinary/storage |
| UC-A05 | Tim kiem mon an | `Foods`, `FoodCategories` | UC-A02 |
| UC-A06 | Loc mon theo danh muc/trang thai | `Foods.FoodStatus` | UC-A02 |

Quy tac:

- Mon phai thuoc mot danh muc.
- Gia mon va gia bien the khong am.
- Ten bien the khong trung trong cung mon.
- Khong xoa cung mon da co trong order/hoa don; chi an hoac ngung ban.

### 4.2. Cum B - Quan ly ban

| Ma use case | Chuc nang | Bang chinh | Phu thuoc |
|---|---|---|---|
| UC-B01 | Them/sua ban | `DiningTables` | M01 |
| UC-B02 | Phan khu va suc chua | `DiningTables.AreaName`, `Capacity` | UC-B01 |
| UC-B03 | Cap nhat trang thai ban | `DiningTables.TableStatus` | M06, M11 |
| UC-B04 | Xem so do va loc ban | `DiningTables`, `OrderTables` | UC-B03 |
| UC-B05 | Gan ban vao order | `OrderTables` | M06 |

Trang thai chuan theo schema:

```text
empty -> serving -> cleaning -> empty
empty -> reserved -> serving
empty -> disabled
```

Khong cho tao order moi tren ban `reserved`, `disabled` hoac dang gan order mo.

### 4.3. Cum C - Order theo ban

| Ma use case | Chuc nang | Bang chinh | Phu thuoc |
|---|---|---|---|
| UC-C01 | Chon ban va tao order | `Orders`, `OrderTables` | M05 |
| UC-C02 | Them mon/bien the | `OrderItems` | M03, M04 |
| UC-C03 | Tang/giam so luong | `OrderItems.Quantity` | UC-C02 |
| UC-C04 | Ghi chu mon/order | `OrderItems.Note`, `Orders.Note` | UC-C02 |
| UC-C05 | Xac nhan mon | `OrderItems.ItemStatus` | M07 |
| UC-C06 | Chuyen ban/gop ban | `OrderTables`, `Orders.MergedIntoOrderId` | UC-C01 |
| UC-C07 | Xem tam tinh | `Orders`, `OrderItems` | UC-C02 |
| UC-C08 | Huy order | `Orders.OrderStatus` | Kiem tra item da lam |

Trang thai order:

```text
open -> preparing -> serving -> completed -> paid
open -> cancelled
open -> merged
```

Trang thai item:

```text
pending -> confirmed -> cooking -> ready -> served
pending/confirmed -> cancelled
```

### 4.4. Cum D - Bep

| Ma use case | Chuc nang | Bang chinh |
|---|---|---|
| UC-D01 | Xem mon chua xac nhan | `OrderItems` |
| UC-D02 | Tiep nhan va bat dau lam | `ItemStatus`, `ChefId` |
| UC-D03 | Cap nhat hoan tat | `ReadyAt`, `ItemStatus` |
| UC-D04 | Bao het mon | `Foods.FoodStatus` |
| UC-D05 | Xem order theo ban | `Orders`, `OrderTables` |

Dau bep chi duoc sua trang thai mon; khong duoc sua gia, xoa hoa don hoac
thay doi tai khoan.

### 4.5. Cum E - Thanh vien, khach hang va dat ban

| Ma use case | Chuc nang | Bang chinh |
|---|---|---|
| UC-E01 | Dang ky/dang nhap | `Users`, `Roles` |
| UC-E02 | Tao ho so thanh vien | `Customers` |
| UC-E03 | Tim thanh vien theo dien thoai | `Customers.Phone` |
| UC-E04 | Dat ban | `Reservations` |
| UC-E05 | Xac nhan/tiep nhan dat ban | `Reservations`, `DiningTables` |
| UC-E06 | Xem lich su hoa don | `Invoices`, `InvoiceItems` |
| UC-E07 | Tich diem/su dung diem | `PointTransactions`, `Customers` |
| UC-E08 | Danh gia dich vu | `Reviews` |
| UC-E09 | Tu van mon bang AI | `AiConversations`, `AiMessages` |

### 4.6. Cum F - Thanh toan va hoa don

1. Nhan vien chon order dang mo.
2. He thong tinh `Subtotal` tu cac `OrderItems`.
3. Tim `Customer` neu khach la thanh vien.
4. Kiem tra va ap dung `PromotionUsages`.
5. Kiem tra diem co the su dung.
6. Tao `Invoice` va sao chep cac dong sang `InvoiceItems`.
7. Luu `FoodNameSnapshot`, `VariantSnapshot`, `UnitPrice`, `Quantity`.
8. Tao `Payment` voi mot trong `cash`, `transfer`, `card`, `e_wallet`.
9. Cong/tru diem bang `PointTransactions`.
10. Chuyen order sang `paid`, gan `PaidAt`, giai phong ban.

## 5. Quy trinh lien thong quan trong

### 5.1. Luong tao order tai ban

```text
Staff dang nhap
  -> chon DiningTable co TableStatus = empty
  -> tao Orders(OrderStatus = open)
  -> tao OrderTables
  -> chuyen ban sang serving
  -> them OrderItems(ItemStatus = pending)
  -> staff xac nhan mon
  -> ItemStatus = confirmed
  -> bep tiep nhan
```

### 5.2. Luong bep

```text
confirmed -> cooking -> ready -> served
                         |
                         +-> staff nhan thong bao va phuc vu
```

### 5.3. Luong thanh toan

```text
Order open/serving
  -> kiem tra item khong con pending/cooking neu quy dinh bat buoc
  -> tinh tam tinh/giam gia/diem
  -> tao Invoice + InvoiceItems
  -> tao Payment
  -> cap nhat diem va PromotionUsages
  -> Order paid
  -> DiningTable empty
```

### 5.4. Luong dat ban

```text
Customer tao Reservation pending
  -> Admin/Staff xem va xac nhan
  -> gan TableId phu hop
  -> confirmed
  -> khach den: checked_in
  -> gan vao order
  -> completed hoac cancelled/no_show
```

## 6. Ke hoach theo giai doan

### Giai doan 0 - Tai lieu va chot kien truc

**Muc tieu:** moi thanh vien lam cung mot mo hinh.

- [x] Co use case tong quat trong `qlNhaHang.drawio`.
- [x] Co schema SQL Server.
- [x] Co tai lieu de cuong tot nghiep.
- [ ] Chot app nao danh cho Staff va web nao danh cho Admin/Chef/Customer.
- [ ] Chot ten endpoint, response va ma trang thai.
- [ ] Chot quy tac branch, commit va moi truong.

**Dau ra:** API contract, data dictionary, role matrix va roadmap nay.

### Giai doan 1 - Database va Backend foundation

- [ ] Tao database `RestaurantManagement` tu schema.
- [ ] Tao migration/seed roles.
- [ ] Tao seed tai khoan test cho 4 actor.
- [ ] Chuyen connection string vao environment.
- [ ] Tao Health API.
- [ ] Tao middleware exception, validation va logging.
- [ ] Tao auth API va hash mat khau.

**Phu thuoc:** khong co.  
**Hoan thanh khi:** database ket noi, login tra token, role sai bi tu choi.

### Giai doan 2 - Admin menu va ban

- [ ] M02: Category CRUD.
- [ ] M03: Food CRUD va soft delete.
- [ ] M04: Variant CRUD.
- [ ] M05: Dining table CRUD va filter.
- [ ] Upload anh va luu `Foods.ImageUrl`.
- [ ] View Admin tach theo `admin/catalog`.

**Phu thuoc:** Giai doan 1.  
**Hoan thanh khi:** Admin tao duoc danh muc, mon co nhieu bien the va ban.

### Giai doan 3 - Staff order theo ban

- [ ] So do ban theo `TableStatus`.
- [ ] Tao order va gan ban.
- [ ] Them mon, bien the, so luong, ghi chu.
- [ ] Sua/xoa item khi chua xac nhan.
- [ ] Xac nhan order.
- [ ] Chuyen ban/gop ban.
- [ ] Hien thi tam tinh.

**Phu thuoc:** M02-M05.  
**Hoan thanh khi:** Staff tao va xac nhan duoc mot order day du.

### Giai doan 4 - Chef workspace

- [ ] Hang doi mon `confirmed`.
- [ ] Cap nhat `cooking`, `ready`, `served`.
- [ ] Gan `ChefId`, `ConfirmedAt`, `ReadyAt`.
- [ ] Bao het mon va cap nhat menu.
- [ ] Loc theo ban/order/trang thai.

**Phu thuoc:** Giai doan 3.  
**Hoan thanh khi:** order tu Staff xuat hien va duoc bep xu ly.

### Giai doan 5 - Thanh toan, thanh vien va hoa don

- [ ] Tim va them Customer.
- [ ] Ap dung Promotion.
- [ ] Tinh diem earn/redeem.
- [ ] Tao Invoice/InvoiceItems snapshot.
- [ ] Tao Payment.
- [ ] Dong order va giai phong ban.
- [ ] Xem danh sach va chi tiet hoa don.

**Phu thuoc:** Giai doan 3 va M08-M09.  
**Hoan thanh khi:** tong tien dung va khong mat lich su sau khi thanh toan.

### Giai doan 6 - Dat ban va customer web

- [ ] Customer xem menu.
- [ ] Customer tao Reservation.
- [ ] Staff/Admin xac nhan dat ban.
- [ ] Gan ban khi check-in.
- [ ] Xem lich su hoa don/dat ban.
- [ ] Danh gia dich vu.

**Phu thuoc:** M05, M08, M10.  
**Hoan thanh khi:** khach dat ban va quan ly xu ly duoc toan bo trang thai.

### Giai doan 7 - Kho, khuyen mai va AI

- [ ] Quan ly Supplier va InventoryItem.
- [ ] Gan FoodIngredients cho mon.
- [ ] Nhap/xuat/huy kho qua StockMovements.
- [ ] Canh bao duoi muc ton toi thieu.
- [ ] Quan ly Promotion.
- [ ] Luu hoi thoai va tin nhan AI.

**Phu thuoc:** menu, order, thanh toan.  
**Hoan thanh khi:** du lieu kho va khuyen mai co the truy vet.

### Giai doan 8 - Thong ke va van hanh

- [ ] Doanh thu ngay/tuan/thang/tat ca.
- [ ] Hoa don theo khoang ngay.
- [ ] Mon ban chay theo so luong va doanh thu.
- [ ] So ban dang phuc vu.
- [ ] Diem thanh vien va hieu qua khuyen mai.
- [ ] Audit log.
- [ ] Export CSV/PDF neu can.

**Phu thuoc:** hoa don va payment da on dinh.

### Giai doan 9 - Kiem thu, bao mat va bao cao tot nghiep

- [ ] Unit test cac ham tinh tien, diem, trang thai.
- [ ] Integration test order -> bep -> thanh toan.
- [ ] Test phan quyen tung actor.
- [ ] Test duplicate request va thao tac dong thoi.
- [ ] Test loi database/API/mat mang.
- [ ] Test responsive Web/Mobile.
- [ ] Viet tai lieu cai dat va demo.
- [ ] Cap nhat use case, ERD, sequence diagram va screenshots.

## 7. Doi chieu database

### 7.1. Nhom tai khoan

| Bang | Vai tro trong nghiep vu | Module |
|---|---|---|
| `Roles` | Danh sach vai tro manager/staff/chef/customer | M01 |
| `Users` | Tai khoan dang nhap va nguoi thao tac | M01 |
| `AuditLogs` | Lich su thay doi quan trong | M15 |

### 7.2. Nhom menu

| Bang | Vai tro trong nghiep vu | Module |
|---|---|---|
| `FoodCategories` | Nhom mon an | M02 |
| `Foods` | Mon an, URL anh, trang thai, gia thap nhat | M03 |
| `FoodVariants` | Size/bien the/gia/don vi | M04 |

### 7.3. Nhom ban, khach va order

| Bang | Vai tro trong nghiep vu | Module |
|---|---|---|
| `Customers` | Ho so thanh vien | M08 |
| `DiningTables` | Ban, khu vuc, suc chua, trang thai | M05 |
| `Reservations` | Phieu dat ban va trang thai | M11 |
| `Orders` | Phieu order va tong tien | M06 |
| `OrderTables` | Quan he order - nhieu ban | M06 |
| `OrderItems` | Cac mon trong order va xu ly bep | M06/M07 |

### 7.4. Nhom thanh toan

| Bang | Vai tro trong nghiep vu | Module |
|---|---|---|
| `Invoices` | Hoa don da chot | M10 |
| `InvoiceItems` | Snapshot dong mon tai luc thanh toan | M10 |
| `Payments` | Giao dich thanh toan | M10 |
| `Promotions` | Chuong trinh khuyen mai | M09 |
| `PromotionUsages` | Lan khuyen mai da su dung | M09/M10 |
| `PointTransactions` | Cong, tru va dieu chinh diem | M08/M10 |

### 7.5. Nhom kho va mo rong

| Bang | Vai tro trong nghiep vu | Module |
|---|---|---|
| `Suppliers` | Nha cung cap | M12 |
| `InventoryItems` | Nguyen lieu ton kho | M12 |
| `FoodIngredients` | Dinh luong nguyen lieu cho mon | M12 |
| `StockMovements` | Nhap, xuat, huy, dieu chinh kho | M12 |
| `Reviews` | Danh gia va phan hoi | M14 |
| `AiConversations` | Phien tu van | M14 |
| `AiMessages` | Tin nhan trong phien tu van | M14 |

### 7.6. Rang buoc can giu khi lap trinh

- `FoodVariants` khong trung `(FoodId, VariantName)`.
- `OrderItems.Quantity > 0`, `UnitPrice >= 0`.
- `Invoice.OrderId` la duy nhat, moi order toi da mot hoa don.
- `Payments` phai thuoc mot invoice.
- `Reservation.GuestCount > 0`.
- `Promotion.EndAt > StartAt`.
- `Reviews.Rating` tu 1 den 5.
- Khong xoa cung `Foods`, `DiningTables`, `Orders`, `Invoices` da co lich su.

## 8. Kiem thu va tieu chi hoan thanh

### 8.1. Checklist chung cho moi module

- [ ] Database/table va foreign key da co.
- [ ] Request/response DTO da co.
- [ ] API co validate va ma loi ro rang.
- [ ] Service/repository khong dat logic tinh tien trong view.
- [ ] View co loading, empty, error va success state.
- [ ] Route co kiem tra role.
- [ ] Da test happy path va invalid path.
- [ ] Da cap nhat bang tien do o muc 3.

### 8.2. Kich ban E2E bat buoc

1. Admin tao category -> food -> variant -> ban.
2. Staff chon ban -> tao order -> them mon -> xac nhan.
3. Chef nhan mon -> cooking -> ready.
4. Staff phuc vu -> mo thanh toan -> chon member -> dung diem.
5. He thong tao invoice, payment, point transaction va giai phong ban.
6. Admin mo thong ke va thay doanh thu, hoa don, mon ban chay.
7. Customer dang nhap -> dat ban -> xem lich su -> danh gia.

### 8.3. Khong danh dau hoan thanh neu

- Chi co giao dien mau ma chua co API.
- API chay duoc nhung chua ket noi database that.
- Thanh toan thanh cong nhung khong tao invoice/payment.
- Xoa du lieu lam mat lich su.
- Role sai van truy cap duoc endpoint.
- Tinh tong tien khac nhau giua order va invoice.

## 9. Nguyen tac cap nhat roadmap

- File cu `roadmap/roadmap.md` duoc giu nguyen lam ban tong quan ban dau.
- File nay la ban chi tiet theo schema va use case, dung de theo doi phat trien.
- Moi chuc nang moi them ma `Nxx` neu khong thuoc module hien co.
- Moi module da code phai cap nhat dong trang thai va muc `Con lai`.
- Chi doi `[x]` sau khi co test va kiem tra luong lien thong.
- Neu bi chan boi Cloudinary, payment gateway, AI hoac credential, dung `[!]`.
- Sau moi giai doan, cap nhat ngay, commit lien quan va ket qua test.

## Nhat ky cap nhat

- 2026-10-03: Tao ban roadmap v2 theo schema SQL Server, use case Draw.io va de cuong tot nghiep.
