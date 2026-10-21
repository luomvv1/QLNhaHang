# WMS/BOM - Quy uoc da chot truoc khi code Backend

Tai lieu nay la ban giao cho Antigravity. Pham vi la **mot nha hang/can tin va mot kho duy nhat**. Khong them bang `Warehouses` hay `Branches` trong giai doan nay.

## 0. Cac diem da sua tu ban Antigravity

- Seed khong con phu thuoc `FoodId`, `VariantId`, `InventoryItemId`, `SupplierId` hay `RoleId` tu tang.
- Seed da tao day du Foods va FoodVariants truoc khi tao BOM; khong con BOM tro vao mon khong ton tai.
- Bo cac lenh `DELETE` co the xoa du lieu that khi chay lai seed.
- Du lieu ton dau ky di qua `StockReceipts`, `StockReceiptDetails`, `InventoryBatches` va `StockMovements`; khong chi update `CurrentQuantity`.
- Them `SystemRemain` va `VariancePortions` la cot computed trong database.
- Them unique/index/check constraint cho BOM, batch FEFO, conversion rate, so luong, hao hut, receipt type/status va portions.
- Sua EF mapping bi trung cua `FoodIngredient`; mapping moi dung khoa `FoodIngredientId` va unique key nghiep vu BOM.
- Dong bo precision so luong theo BaseUnit thanh `decimal(18,4)` o InventoryItems, StockMovements, batch, receipt detail va audit.

### Bo sung sau khi audit API hang muc 1

- `AdminInventoryController` chi cho `admin` va `manager`; staff khong duoc xem gia von, margin, supplier va lich su kho cua admin.
- `CreateBatch` dung transaction `Serializable`, doc item trong transaction, xu ly trung batch code bang `409 Conflict` va khong de mat cap nhat ton khi request dong thoi.
- Khong cho tao/cap nhat batch `active` neu da het han, het so luong hoac sai thu tu ngay san xuat/hạn dùng.
- Khong gan movement cho user mac dinh. Token thieu `NameIdentifier` se bi tu choi.
- EF Core mapping cua `CurrentQuantity` va `MinimumQuantity` da dong bo `decimal(18,4)` voi migration.
- BOM tu choi trung `InventoryItemId` trong cung mot request; supplier da tung duoc tham chieu se chi bi vo hieu hoa, khong xoa cung.

### Cap nhat sau audit Giai doan phieu kho/kiem ke/suat ban

- `StockReceiptDetail` da luu `NewBatchCode`, `ManufacturingDate`, `ExpiryDate`; approve import khong con bo qua thong tin lo trong phieu nhap.
- Khi tham chieu `BatchId`, API bat buoc kiem tra batch thuoc dung `InventoryItemId`; khong cho trừ nham lo cua nguyen lieu khac.
- Approve export/waste kiem tra trang thai, so luong va han lo; waste co the xu ly lo da het han.
- Quick return dung transaction `Serializable`, tao lô return rieng neu khong chi dinh lo va luon ghi movement co `ReferenceId` cua receipt.
- Quick waste dung FEFO theo tung batch, tao detail/movement rieng cho tung phan bo va khong cho tong ton lech voi ton lo.
- Kiem ke khong cho hoan tat tren snapshot da cu; neu ton so da thay doi sau luc tao audit thi tra `409` de tao lai snapshot.
- Kiem ke thua tao batch dieu chinh rieng; kiem ke thieu tao movement/detail theo tung batch FEFO.
- Dong bo `SoldPortions` tren served/undo chay trong transaction `Serializable`; undo tu `served` ve `ready` da hoan lai so phan.

### Cac bai test bat buoc truoc khi danh dau hoan tat

- Hai request approve cung mot phieu: mot thanh cong, mot `409`.
- Hai request xuat cung mot batch: khong am `QuantityRemaining` va khong mat cap nhat `CurrentQuantity`.
- BatchId cua nguyen lieu A tham chieu trong detail nguyen lieu B: bi tu choi.
- Quick waste mot nguyen lieu qua nhieu batch: tong movement/detail bang dung so luong yeu cau.
- Quick return khong co BatchId: tao batch return va movement co `ReferenceId` hop le.
- Audit co movement phat sinh sau snapshot: khong cho complete, tra `409`.
- Audit thua/thieu qua nhieu batch: moi batch co movement va receipt detail tuong ung.
- Served goi lap lai, Chef undo va Staff served dong thoi: `SoldPortions` chi thay doi dung mot lan.

## 1. Thu tu chay database

1. Chay `docs/database/restaurant_schema.sql` tren database `RestaurantManagement`.
2. Chay `docs/database/migration_wms_bom.sql` de nang cap schema WMS/BOM.
3. Neu database da co du lieu WMS cu, chay mot lan `docs/database/migration_wms_cost_purchase_unit.sql` de doi gia tu BaseUnit sang PurchaseUnit.
4. Chay `docs/database/seed_wms_bom_sample_data.sql` de tao du lieu mau cho dev/test.

`restaurant_schema.sql` la schema nen cu; cac bang va cot WMS/BOM moi nam trong migration. Khong chay seed cu co hard-code ID.

## 2. Quy uoc don vi va BOM

- `InventoryItems.CurrentQuantity` luu **duy nhat theo `BaseUnit`**.
- `PurchaseUnit` la don vi mua, vi du `kg`, `goi`, `con`.
- `BaseUnit` la don vi nho nhat dung trong tinh ton, vi du `g`, `ml`.
- `ConversionRate` la so `BaseUnit` trong mot `PurchaseUnit`; luon lon hon 0.
- `CostPerUnit` va `InventoryBatches.UnitCost` luu theo `PurchaseUnit` (vi du VND/thung), khong phai theo `BaseUnit`.
- `CurrentQuantity`, `InventoryBatches.QuantityRemaining`, `StockMovements.Quantity`, `StockReceiptDetails.Quantity` va `FoodIngredient.QuantityPerUnit` luu theo `BaseUnit`.
- Khi tinh thanh tien/gia von: `BaseQuantity / ConversionRate * CostPerUnit`; khong duoc nhan truc tiep so luong BaseUnit voi gia mua.
- O giao dien nhap lô, nguoi dung nhap so luong va don gia cung theo `PurchaseUnit`; frontend tu quy doi so luong sang `BaseUnit` truoc khi goi API tao batch.
- Du lieu cu phai chay mot lan `migration_wms_cost_purchase_unit.sql` sau `migration_wms_bom.sql` de doi gia cu tu BaseUnit sang PurchaseUnit; script co marker chong chay lap.
- `FoodIngredients` co khoa nghiep vu: `FoodId + VariantId + InventoryItemId`.
- `FoodIngredient.QuantityPerUnit` la luong **gross can xuat kho cho mot variant**, da tinh luong so che can thiet.
- `WastagePercent` luu thong tin hao hut so che. Seed da dua luong gross vao `QuantityPerUnit`, vi vay backend **khong duoc nhan hao hut them lan hai**. Neu sau nay muon tinh hao hut tu net recipe, phai doi mot quy uoc va migration rieng.
- Khi lap phieu xuat bep tu ke hoach suat, API tra them `WastageSummary`, `HasWastage` va `WastageDetails` de nguoi duyet theo doi hao hut theo tung mon. Cac thong tin nay chi la doi chieu dinh muc; `QuantityRequired` van la luong gross da tinh san hao hut.
- BOM khong hien thi va khong gan cung gia nhap theo nguyen lieu. Gia nhap phuc vu nhap kho/FEFO duoc luu theo tung `InventoryBatch.UnitCost`; `InventoryItem.CostPerUnit` chi la gia chuan tham khao cho phieu nhap moi va bao cao rieng.
- `WastagePercent` chi duoc phep lon hon 0 voi nguyen lieu co `BaseUnit` thuoc nhom khoi luong: `g`, `kg`, `gam`, `gram`, `mg`, `kilogram`. Cac don vi dem/dong goi nhu `con`, `lon`, `chai`, `goi` phai co hao hut bang `0`.
- `VariantId` phai thuoc dung `FoodId`; khong cho phep gan cheo variant cua mon khac.

## 3. Ton kho theo lo va movement

- `InventoryBatches` la nguon chi tiet de FEFO/FIFO: mot nguyen lieu co the co nhieu lo.
- Thu tu xuat mac dinh: `ExpiryDate ASC`, sau do `ReceivedDate ASC`, sau do `BatchId ASC`.
- `StockMovements.Quantity` luon la so duong; chieu bien dong nam trong `MovementType`.
- `MovementType` hien co: `import`, `export`, `adjustment`, `waste`.
- `StockReceipts.ReceiptType` phan biet nghiep vu: `import_supplier`, `export_kitchen`, `return_from_kitchen`, `waste_disposal`, `audit_adjustment`.
- Moi thay doi ton bat buoc co movement va phai cap nhat batch cung voi tong `InventoryItems.CurrentQuantity`.
- Khong cho phep sua `CurrentQuantity` truc tiep trong API neu khong sinh movement tuong ung.
- Khong cho phep xuat qua `QuantityRemaining`; neu thieu ton thi tu choi toan bo giao dich.

## 4. Transaction khi duyet phieu kho

Endpoint approve phieu kho phai nam trong mot transaction. Trong transaction can:

1. Khoa header phieu va kiem tra phieu van o trang thai `draft`.
2. Khoa cac lo lien quan, uu tien theo thu tu FEFO/FIFO.
3. Tao/cap nhat `InventoryBatches`.
4. Tao `StockReceiptDetails` va `StockMovements`.
5. Cap nhat tong `InventoryItems.CurrentQuantity`.
6. Cap nhat `StockReceipts.Status = approved`, `ApprovedBy`, `ApprovedAt`.
7. Commit; loi o bat ky buoc nao thi rollback toan bo.

Nen dung isolation `Serializable` hoac row lock tuong duong (`UPDLOCK`, `HOLDLOCK`) khi phan bo lo de tranh hai request cung xuat mot so ton.

## 5. Theo doi suat an

`DailyPortionControls` duy nhat theo `ControlDate + FoodId + VariantId`.

- `PlannedPortions`: so phan du kien.
- `CarryOverPortions`: so phan chuyen tu ngay truoc.
- `CookedPortions`: so phan bep thuc te nau.
- `SoldPortions`: so phan da phuc vu/ban.
- `SystemRemain`: cot computed trong DB = `CookedPortions - SoldPortions`.
- `ActualRemain`: so phan dem thuc te cuoi ngay.
- `VariancePortions`: cot computed = `ActualRemain - SystemRemain`, null neu chua dem.
- `DisposedPortions`: so phan huy.
- `NextCarryOverPortions`: so phan duoc chuyen sang ngay mai.

Khi order item chuyen sang trang thai nghiep vu da phuc vu (khuyen nghi `served`), backend cong `SoldPortions` dung mot lan. Khi huy/hoan phan da ghi nhan, phai co logic bu tru ro rang, khong cong lap lai khi endpoint duoc goi lai.

Cong thuc lap phieu xuat bep:

```text
NewCookedPlan = max(PlannedPortions - CarryOverPortions, 0)
RequiredQuantity = NewCookedPlan * QuantityPerUnit
```

## 6. Cuoi ngay va kiem ke

- Nguyen lieu tho chua dung: tao phieu `return_from_kitchen`, co detail, batch va movement `import`.
- Mon da nau con du: nhap `ActualRemain`, de DB tinh `VariancePortions`.
- Mon chuyen ngay mai: cap nhat `NextCarryOverPortions` va tao du lieu carry-over cho ngay tiep theo theo mot transaction.
- Mon huy: cap nhat `DisposedPortions` va tao phieu `waste_disposal`, movement `waste`.
- Kiem ke nguyen lieu: luu `SystemQuantity`, `ActualQuantity`, `DifferenceQuantity`; khi hoan tat tao phieu `audit_adjustment` va movement `adjustment` cho phan lech.

## 7. Du lieu seed mau

Seed moi:

- Khong dung ID tu tang cua database.
- Tim supplier, food, variant, inventory item theo ten.
- Khong `DELETE` `FoodIngredients` hay `InventoryBatches`.
- Tao 10 mon, 20 variant, 12 nguyen lieu va BOM theo variant.
- Tao phieu nhap dau ky `import_supplier` trang thai `approved`.
- Tao batch, `StockReceiptDetails` va `StockMovements` cho cac lo dau ky.
- Chi tang `CurrentQuantity` trong cung nhanh voi movement nhap kho.
- Co the chay lai seed ma khong nhan ban du lieu mau.

Seed chi dung cho dev/test. Khong dung cac lo, gia von va ke hoach mau nay lam so lieu kinh doanh that.

## 8. Mapping EF da cap nhat

`RestaurantDbContext` da co mapping cho:

- `FoodIngredient` voi khoa `FoodIngredientId` va unique index BOM.
- `InventoryBatch` voi unique `(InventoryItemId, BatchCode)` va index FEFO.
- `StockReceipt`, `StockReceiptDetail`.
- `DailyPortionControl` voi hai cot computed `SystemRemain`, `VariancePortions`.
- `StockAudit`, `StockAuditItem`.
- `StockMovement.BatchId` va index theo lo/ngay.

Khong them lai mapping cu cua `FoodIngredient` theo khoa `(FoodId, InventoryItemId)`.

## 9. Cac API backend can lam tiep

1. CRUD nguyen lieu, supplier, batch va BOM; validate don vi, variant va so luong.
2. CRUD `DailyPortionControls`; tao phieu xuat bep tu ke hoach suat.
3. CRUD phieu kho; endpoint approve co transaction va FEFO/FIFO.
4. Hoan kho, huy kho va kiem ke dieu chinh; moi thao tac sinh movement.
5. Bao cao ton theo nguyen lieu, theo lo, hang sap het han va lich su movement.
6. Cap nhat `SoldPortions` idempotent theo state transition cua `OrderItem`.
7. Phan quyen endpoint WMS cho role `manager`/`admin`; staff chi duoc xem hoac tao thao tac duoc cap quyen.
8. Viet integration test cho: thieu ton, hai request approve dong thoi, FEFO nhieu lo, approve lap lai, hoan kho, huy kho, audit adjustment va sold portions khong cong lap.

## 10. Kiem tra truoc khi code API

- Build API: `dotnet build backend/Restaurant.Api/Restaurant.Api.csproj --no-restore`
- Khong chay seed neu chua chay migration.
- Kiem tra migration tren database dev truoc; khong chay truc tiep tren production.
- Neu thay doi quy uoc `WastagePercent`, phai sua ca migration/seed/tai lieu nay va test lai cong thuc xuat bep.
