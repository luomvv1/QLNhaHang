# Roadmap he thong quan ly nha hang theo ban

## 1. Muc tieu

Xay dung he thong quan ly nha hang theo ban, cho phep quan ly mon an, ban an,
nhan vien, order, bep, thanh toan, hoa don, thanh vien va bao cao thong ke.
He thong gom cac nhom nguoi dung:

- **Quan ly/Admin:** cau hinh va theo doi toan bo nha hang.
- **Nhan vien phuc vu:** quan ly ban, tao order, them mon va thanh toan.
- **Dau bep:** tiep nhan mon, cap nhat tien do va bao het mon.
- **Thanh vien/Khach hang:** xem thuc don, dat ban, xem lich su va tich diem.

## 2. Nguyen tac trien khai

- Lam theo thu tu tu nen tang du lieu den giao dien.
- Moi chuc nang phai co model, service/repository, API va view rieng.
- Khong xoa du lieu lich su order va hoa don; uu tien ngung su dung (soft delete).
- Trang thai trong database dung ma tieng Anh, giao dien moi hien thi tieng Viet.
- Moi giai doan chi danh dau hoan thanh sau khi da test ca giao dien va API.

## 3. Cau truc module

### 3.1. Nen tang dung chung

- [ ] Cau hinh moi truong dev/test/prod.
- [ ] Cau hinh ket noi Firebase hoac SQL Server.
- [ ] Auth, phan quyen va bao ve route.
- [ ] Xu ly loi chung, loading, empty state va thong bao.
- [ ] Quy uoc ma loi API va dinh dang response.
- [ ] Ghi nhat ky thao tac quan trong.

### 3.2. Quan ly danh muc va thuc don

- [ ] Them, sua, an/hien danh muc.
- [ ] Them, sua, ngung ban mon an.
- [ ] Quan ly hinh anh mon an.
- [ ] Them nhieu bien the: size, don vi, gia.
- [ ] An/hien bien the rieng le.
- [ ] Tim kiem va loc theo danh muc, trang thai, khoang gia.
- [ ] Kiem tra khong cho trung ten mon trong cung danh muc.

### 3.3. Quan ly ban

- [ ] Them, sua, ngung su dung ban.
- [ ] Phan khu vuc va suc chua.
- [ ] Hien thi so do ban.
- [ ] Loc ban trong, dang phuc vu, da dat, dang don dep.
- [ ] Lien ket ban voi order dang mo.
- [ ] Khoa thao tac xoa ban khi con hoa don lich su.

### 3.4. Quan ly tai khoan va nhan vien

- [ ] Dang nhap va dang xuat.
- [ ] Tao tai khoan nhan vien.
- [ ] Gan vai tro: quan ly, phuc vu, dau bep, khach hang.
- [ ] Khoa/mo tai khoan.
- [ ] Doi mat khau va cap nhat thong tin ca nhan.
- [ ] Phan quyen theo route va theo API.
- [ ] Theo doi lich su dang nhap va thao tac quan trong.

### 3.5. Order theo ban

- [ ] Chon ban trong de tao order.
- [ ] Tao ma order duy nhat.
- [ ] Them mon va bien the vao order.
- [ ] Tang/giam so luong.
- [ ] Ghi chu mon va ghi chu order.
- [ ] Sua hoac xoa mon khi mon chua duoc xac nhan.
- [ ] Gop ban va chuyen ban.
- [ ] Them mon phat sinh vao order dang mo.
- [ ] Hien thi tam tinh theo thoi gian thuc.

### 3.6. Quy trinh bep

- [ ] Nhan cac mon da duoc nhan vien xac nhan.
- [ ] Chuyen trang thai: cho che bien -> dang lam -> san sang -> da phuc vu.
- [ ] Hien thi mon theo order va theo ban.
- [ ] Bao het mon hoac tam ngung mon.
- [ ] Thong bao cho nhan vien khi mon da san sang.
- [ ] Luu nguoi thao tac va thoi gian cap nhat.

### 3.7. Thanh toan va hoa don

- [ ] Mo man hinh thanh toan tu order dang mo.
- [ ] Chon thanh vien theo so dien thoai.
- [ ] Ap dung khuyen mai.
- [ ] Su dung va cong diem.
- [ ] Chon tien mat, chuyen khoan hoac phuong thuc khac.
- [ ] Kiem tra tong tien truoc khi thanh toan.
- [ ] Tao hoa don va khoa order sau khi thanh toan.
- [ ] Luu snapshot ten mon, bien the, don gia tai thoi diem thanh toan.
- [ ] In hoac xuat hoa don neu can.

### 3.8. Thanh vien va khach hang

- [ ] Dang ky/dang nhap tai khoan khach hang.
- [ ] Tim thanh vien theo so dien thoai.
- [ ] Them nhanh thanh vien trong luc thanh toan.
- [ ] Xem diem hien co va lich su diem.
- [ ] Dat ban truc tuyen.
- [ ] Xem thuc don va thong tin nha hang.
- [ ] Xem lich su hoa don va dat ban.
- [ ] Danh gia dich vu sau khi thanh toan.

### 3.9. Bao cao va thong ke

- [ ] Doanh thu theo ngay, tuan, thang, khoang ngay.
- [ ] So luong hoa don da thanh toan.
- [ ] So order da tao va da hoan tat.
- [ ] Mon ban chay.
- [ ] Doanh thu theo danh muc.
- [ ] So ban dang phuc vu.
- [ ] Bao cao diem thanh vien va khuyen mai.
- [ ] Loc, xem chi tiet va xuat bao cao.

## 4. Quy trinh nghiep vu tong quat

### 4.1. Khoi tao nha hang

1. Quan ly dang nhap.
2. Tao danh muc mon an.
3. Tao mon an va cac bien the.
4. Tai anh mon an va luu URL anh.
5. Tao khu vuc va ban an.
6. Tao tai khoan nhan vien va gan vai tro.
7. Kiem tra thuc don va so do ban truoc khi mo cua.

### 4.2. Nhan vien phuc vu tiep khach

1. Nhan vien dang nhap va mo so do ban.
2. Chon ban trong de tao order.
3. Them mon, chon bien the, so luong va ghi chu.
4. Xac nhan cac mon moi.
5. He thong chuyen cac mon sang man hinh bep.
6. Nhan vien theo doi trang thai mon va phuc vu khach.
7. Neu khach goi them, nhan vien them mon vao order dang mo.

### 4.3. Dau bep xu ly mon

1. Dau bep dang nhap vao man hinh bep.
2. Xem danh sach mon cho xu ly.
3. Tiep nhan mon va chuyen sang dang lam.
4. Hoan tat mon va chuyen sang san sang.
5. Neu het nguyen lieu, bao het mon.
6. Nhan vien nhan thong tin va phuc vu mon cho khach.

### 4.4. Thanh toan

1. Nhan vien mo man hinh thanh toan cua order.
2. Kiem tra cac mon da goi va tam tinh.
3. Tim thanh vien theo so dien thoai neu co.
4. Ap dung giam gia va diem su dung.
5. Chon phuong thuc thanh toan.
6. He thong tao hoa don, cong diem va ghi lich su.
7. Dong order va tra ban ve trang thai trong.

### 4.5. Khach hang dat ban

1. Khach dang nhap.
2. Chon ngay, gio, so nguoi va khu vuc.
3. He thong kiem tra ban phu hop.
4. Tao phieu dat ban cho khach.
5. Quan ly xac nhan hoac tu choi dat ban.
6. Khi khach den, nhan vien tiep nhan va gan order.
7. Sau khi thanh toan, luu lich su va cho phep danh gia.

## 5. Ke hoach theo giai doan

### Giai doan 0 - Chuan bi va chot kien truc

**Muc tieu:** he thong co cau truc ro rang va chay duoc tren may phat trien.

- [ ] Chot frontend Web, mobile va backend.
- [ ] Chot SQL Server/Firebase la nguon du lieu chinh.
- [ ] Chot quy uoc ten bang, cot, endpoint va trang thai.
- [ ] Tao file cau hinh moi truong.
- [ ] Tao seed role va tai khoan test.
- [ ] Tao README huong dan chay.

**Ket qua can dat:** dang nhap duoc, API health tra ve thanh cong, database ket noi.

### Giai doan 1 - Xac thuc va phan quyen

**Phu trach:** Backend + Auth view.

- [ ] Login, logout, register.
- [ ] JWT/session.
- [ ] Role guard cho admin, staff, chef, customer.
- [ ] Quan ly tai khoan co ban.

**Ket qua can dat:** moi actor vao dung workspace va khong truy cap sai quyen.

### Giai doan 2 - Nen tang quan ly thuc don va ban

**Phu trach:** Admin.

- [ ] CRUD danh muc.
- [ ] CRUD mon an.
- [ ] CRUD bien the.
- [ ] Upload anh.
- [ ] CRUD ban va trang thai ban.
- [ ] Tim kiem, loc va trang thai rong/loading/error.

**Ket qua can dat:** quan ly co the cau hinh mot nha hang moi tu dau.

### Giai doan 3 - Order va phuc vu tai ban

**Phu trach:** Staff + Backend order.

- [ ] So do ban.
- [ ] Tao order.
- [ ] Them/sua/xoa mon.
- [ ] Chuyen ban, gop ban.
- [ ] Xac nhan mon.
- [ ] Order dang mo va tam tinh.

**Ket qua can dat:** nhan vien hoan tat duoc mot quy trinh goi mon tai ban.

### Giai doan 4 - Man hinh bep

**Phu trach:** Chef + Backend kitchen.

- [ ] Hang doi mon.
- [ ] Cap nhat trang thai mon.
- [ ] Bao het mon.
- [ ] Thong bao mon san sang.

**Ket qua can dat:** mot order sau khi xac nhan di qua duoc day xu ly bep.

### Giai doan 5 - Thanh toan, thanh vien va hoa don

**Phu trach:** Staff + Customer + Backend payment.

- [ ] Tim/them thanh vien.
- [ ] Diem tich luy.
- [ ] Khuyen mai.
- [ ] Thanh toan.
- [ ] Tao hoa don.
- [ ] Luu snapshot mon va bien the.

**Ket qua can dat:** order duoc dong dung, ban duoc giai phong va hoa don khong thay doi lich su.

### Giai doan 6 - Dat ban va trai nghiem khach hang

- [ ] Xem thuc don.
- [ ] Dat ban.
- [ ] Quan ly xac nhan dat ban.
- [ ] Xem lich su hoa don.
- [ ] Danh gia dich vu.

**Ket qua can dat:** khach hang co the tu xem thong tin va tao dat ban.

### Giai doan 7 - Bao cao, thong ke va van hanh

- [ ] Bao cao doanh thu.
- [ ] Bao cao mon ban chay.
- [ ] Bao cao hoa don.
- [ ] Loc theo ngay/tuan/thang.
- [ ] Xuat du lieu.
- [ ] Audit log.
- [ ] Backup va phuc hoi database.

**Ket qua can dat:** quan ly co du du lieu de theo doi hoat dong va viet bao cao tot nghiep.

### Giai doan 8 - Kiem thu va hoan thien

- [ ] Unit test cho service va tinh tien.
- [ ] Integration test cho order/thanh toan.
- [ ] Test phan quyen theo actor.
- [ ] Test loi mat mang, API loi, du lieu rong.
- [ ] Test responsive Web va man hinh Mobile.
- [ ] Kiem tra bao mat mat khau va token.
- [ ] Viet tai lieu cai dat, huong dan su dung va demo.

## 6. Tieu chi hoan thanh mot chuc nang

Mot chuc nang chi duoc danh dau hoan thanh khi:

- [ ] Co database/model tuong ung.
- [ ] Co API va validate du lieu.
- [ ] Co service/repository tach khoi view.
- [ ] Co view dung actor va route bao ve.
- [ ] Co loading, empty state, error state.
- [ ] Da test truong hop thanh cong va that bai.
- [ ] Da cap nhat tai lieu va roadmap.

## 7. Thu tu uu tien de lam hang ngay

1. Hoan thien Giai doan 0 va 1.
2. Hoan thien Admin mon an, bien the, danh muc va ban.
3. Hoan thien order theo ban.
4. Ket noi order voi man hinh bep.
5. Hoan thien thanh toan va hoa don.
6. Them thanh vien, dat ban va lich su khach hang.
7. Lam thong ke va bao cao.
8. Kiem thu tong the va viet tai lieu.

## 8. Cach cap nhat roadmap

- Danh dau `[x]` khi chuc nang da code va da test.
- Neu moi lam giao dien, giu `[ ]` va ghi chu `UI only`.
- Neu API da co nhung chua co view, ghi chu `API only`.
- Moi yeu cau moi phai duoc them vao dung module va dung giai doan.
- Cap nhat ngay gan nhat o cuoi file sau moi dot thay doi lon.

## 9. Nhat ky cap nhat

- 2026-10-03: Tao roadmap tong the, phan chia actor, module nghiep vu va 9 giai doan trien khai.
