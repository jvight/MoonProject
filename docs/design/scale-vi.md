# Kinh Thánh Tỷ Lệ & Kích Thước Chuẩn (`docs/design/scale-vi.md`)

> **Trạng thái**: Kiểm toán đo đạc toàn diện & Đặc tả tỷ lệ chuẩn (Không thay đổi kích thước bất kỳ model nào trong tác vụ này; mọi đề xuất điều chỉnh model hiện có đều được gắn nhãn `[PROPOSAL]` để Director quyết định).
> **Quy ước hệ trục tọa độ**:
> - **Blender / Contract JSON (`tools/art/qa/contracts/*.json`)**: `X` = Rộng (Width), `Y` = Dài/Sâu (Length/Depth, `+Y` là hướng tiến của Rover 07), `Z` = Cao (Height, tính từ mặt đất `Z = 0`).
> - **Unity Prefab (`Assets/_Project/Generated/Art/`)**: `X` = Rộng, `Y` = Cao, `Z` = Dài/Sâu. Mọi bảng dưới đây đều liệt kê kích thước theo thứ tự thống nhất **`Rộng (W) × Dài (L) × Cao (H)`** tính bằng mét (m).
> - **Hình ảnh trực quan**: `Logs/scale/lineup.png`, `Logs/scale/07_vs_world.png`, `Logs/scale/07_ortho_dimensions.png` (`07_front.png`, `07_side.png`, `07_top.png`).

---

## 1. Tóm Tắt Điều Hành — Danh Sách Bất Cập Tỷ Lệ & Đề Xuất (`[PROPOSAL]`)

Qua phép đo chính xác toàn bộ **39 model FBX R1** và **116 prefab sinh thủ tục (procedural)** (tổng cộng 155 prefab trong dự án), chúng tôi đã đối chiếu mọi khung cửa, khoang đáp, bệ sạc/bệ xe, đạo cụ tỷ lệ người, công trình do rover xây dựng, thùng hàng (crate) và đá tảng (rock) với hai mốc chuẩn: **Rover 07 (`1.4726 m Rộng × 2.0143 m Dài × 1.5902 m Cao`)** và **Phi hành gia cao `1.750 m`**.

### Danh sách các phát hiện bất cập & đề xuất chỉnh sửa (`[PROPOSAL]`)

1. **`[PROPOSAL]` `ChargingPad.fbx` — 3 Va Chạm Hình Học Trực Tiếp Với Rover 07**:
   - **Phát hiện 1a (Va chạm thanh dẫn hướng bánh xe `GuideRail_L/R`)**: Hai thanh dẫn hướng nhô cao (`X = ±0.680 m`, mặt trong tại `±0.629 m`) chỉ để lại khoảng thông thủy **`1.2580 m`** — **hẹp hơn `0.2146 m` (21.5 cm)** so với bề rộng phủ bì lốp của 07 (**`1.4726 m`**, `X = ±0.7363 m`)! Khi 07 đỗ trên bệ, hai lốp rộng `0.2566 m` đè trực tiếp lên thanh dẫn hướng. Ngoài ra, hai làn cao su (`wheel_lane_x = ±0.48 m`, rộng `0.28 m`) đang thiết kế cho vệt bánh `0.96 m` thay vì vệt bánh `1.220 m` (`±0.610 m`) của 07.
   - **Phát hiện 1b (Dốc lên phía trước quá hẹp)**: Dốc chính (`ramp_width_front = 0.980 m`) và dốc bên (`0.900 m`) đều hẹp hơn vệt bánh xe của 07 (`1.220 m` tâm-tới-tâm, `1.4726 m` phủ bì) khiến bánh xe của 07 trượt ra ngoài dốc!
   - **Phát hiện 1c (Chiều dài mặt sàn ngắn / Va chạm trụ đèn sau)**: Khoảng sàn phẳng từ mặt trước trụ `TotemBar` (`Y = -0.751 m`) đến mép dốc trước (`Y = +0.917 m`) chỉ dài **`1.668 m`**. Khi 07 đỗ khớp tâm tiếp điện `ContactShoes` (`Y = +0.04 m`), lốp sau của 07 (`Y = -0.839 m`) **đâm xuyên `0.088 m` vào trụ bollard phía sau**.
   - **`[PROPOSAL]` (`ChargingPad.fbx` / `tools/art/blender/charging_pad.py`)**:
     - Dịch `wheel_lane_x` từ `±0.48 m` ra **`±0.610 m`** (tăng `1.27×`) và mở rộng làn bánh từ `0.28 m` lên **`0.34 m`**.
     - Dịch `guide_rail_x` từ `±0.680 m` ra **`±0.860 m`** (**thông thủy `1.620 m`**, tạo khoảng hở `+0.074 m` mỗi bên ngoài lốp `1.473 m` của 07).
     - Mở rộng dốc trước `ramp_width_front` thêm **`1.63×`** (`0.980 m -> 1.600 m`).
     - Kéo dài sàn bệ thêm **`+0.54 m`** (`Y tổng: 2.661 m -> 3.200 m`, lùi mặt trụ sau về `Y = -1.100 m`).

2. **`[PROPOSAL]` `KenjiWorkshop.fbx` — Thanh Palăng (`HoistRail`) & Đèn Treo (`HangingLamp`) Va Chạm Với Đầu Của 07**:
   - **Phát hiện**: Khung cửa vòm ngoài của xưởng có bề rộng thông thủy **`3.240 m`** và chiều cao **`1.855 m`** trên mặt sàn bê tông dày `0.08 m` (`Z = 1.935 m`), đủ rộng và cao cho 07 (`1.473 m W × 1.591 m H`). **Tuy nhiên**, ngay bên trong lối vào (`Y = -1.73..-1.39 m`), móc xích của `HoistRail` (`X = -0.10 m`) thõng xuống tận **`Z = 1.2680 m`** (`1.188 m` tính từ mặt sàn bê tông) và `HangingLamp` (`X = +0.88 m`) thõng xuống **`Z = 1.3500 m`** — thấp hơn đầu CRT (`1.525 m`) và ăng-ten (`1.591 m`) của 07 tới **`0.323–0.403 m`**!
   - **`[PROPOSAL]` (`KenjiWorkshop.fbx` / `tools/art/blender/ruins/kenji_workshop.py`)**:
     - Thu ngắn dây xích `HoistRail` thêm **`+0.48 m`** (`Z: 1.268 m -> 1.750 m`) và dây `HangingLamp` thêm **`+0.40 m`** (`Z: 1.350 m -> 1.750 m`) để 07 đi qua an toàn.

3. **`[PROPOSAL]` `UpgradeBay.fbx` — Rãnh Dẫn Hướng (`GuideRail`) Quá Hẹp & Tay Cờ-Lê (`WrenchArm`) Va Chạm Đầu 07**:
   - **Phát hiện 3a (`GuideRail_L/R`)**: Đặt tại `X = ±0.720 m` (lệch với vệt bánh `±0.610 m` của 07) và lòng rãnh chữ U giữa hai gờ thép chỉ rộng **`0.056 m` (5.6 cm)** trong khi lốp của 07 rộng **`0.2566 m` (25.7 cm)**!
   - **Phát hiện 3b (`WrenchArm_L/R` ở tư thế nghỉ)**: Trong FBX xuất ra, `WrenchArm_L` vươn vào tới `X = +0.3256 m` và `WrenchArm_R` tới `X = -0.1850 m` ở độ cao `Z = 0.98..1.59 m`, chỉ chừa **`0.5106 m` khe ngang**, đâm xuyên vào đầu `Head` rộng `0.770 m` của 07 tới **`0.259 m`**!
   - **`[PROPOSAL]` (`UpgradeBay.fbx` / `tools/art/blender/upgrade_bay.py`)**:
     - Đưa `GuideRail_L/R` về **`X = ±0.610 m`** và mở rộng lòng rãnh chữ U gấp **`5.7×`** (`0.056 m -> 0.320 m`).
     - Xoay mở hai tay `WrenchArm_L/R` ở tư thế nghỉ ra **`X = ±0.60 m`** (khe thông thủy `1.20 m` thay vì `0.51 m`).

4. **`[PROPOSAL]` `VehiclePad.fbx` — Mép Dốc Trước (`RampLip`) Hẹp & Khung Móc Cẩu (`HoistHook`) Treo Thấp**:
   - **Phát hiện**: Khoảng thông thủy giữa hai gờ bên của `RampLip` chỉ rộng **`1.4050 m`** (hẹp hơn lốp `1.4726 m` của 07 `6.8 cm`), và khung móc cẩu `HoistHook` treo ở độ cao **`Z = 1.2325 m`** ngay giữa sàn (`1.073 m` trên mặt sàn cao `0.16 m`), thấp hơn đầu của 07 tới **`0.453 m`**.
   - **`[PROPOSAL]` (`VehiclePad.fbx` / `tools/art/blender/vehicle_pad.py`)**: Mở rộng `RampLip` **`1.25×`** (`1.405 m -> 1.760 m` thông thủy) và nâng `HoistHook` ở tư thế nghỉ lên **`+0.55 m`** (`Z: 1.2325 m -> 1.780 m`).

5. **`[PROPOSAL]` `HabitatModules.fbx`, `KenjiWorkshop.fbx`, `GaragePort.prefab`, `RoverBayMeshes.cs` — Bất Cập Tỷ Lệ Người `1.75 m`**:
   - **Cửa Airlock `HabitatModules.fbx` (`ModuleA_Main`)**: Cánh cửa chỉ rộng `0.96 m × cao 1.32 m` (`Z = 0.12..1.44 m`), thấp hơn người `1.75 m` tới **`0.43 m` (25%)**, bảng nút bấm `AirlockPanel` đặt ở `Z = 0.84 m` (ngang hông). **`[PROPOSAL]`**: Phóng cửa **`1.15× Rộng × 1.55× Cao`** (`-> 1.10 × 2.05 m`), nâng bảng nút bấm lên `Z = 1.25 m`.
   - **Tủ đồ `KenjiWorkshop.fbx` (`WorkbenchAndRacks`)**: Tủ đứng bên phải chỉ cao **`1.16 m`** (ngang ngực). **`[PROPOSAL]`**: Tăng chiều cao tủ **`1.55×`** (`1.16 m -> 1.80 m`) và nâng mặt bàn nguội thêm `+0.085 m` (`0.795 m -> 0.880 m`).
   - **Cửa cuốn `GaragePort.prefab` / `GarageSite.cs`**: Cửa rover cao `1.600 m` (chỉ dư **9 mm** so với ăng-ten `1.591 m` của 07!), cửa người `0.74 × 1.80 m`. **`[PROPOSAL]`**: Phóng chiều cao `GaragePort` **`1.25×`** (`1.68 m -> 2.10 m`, cửa rover `2.20 × 2.00 m`) và cửa người lên `0.90 × 2.05 m`.
   - **Cốc nước (`RoverBayMeshes.cs` `MugProfile`)**: Đường kính `0.110 m × cao 0.110 m` (~1 lít). **`[PROPOSAL]`**: Thu nhỏ **`0.78×`** về chuẩn `0.085 m Ø × 0.095 m H`.

6. **`[PROPOSAL]` `CargoLander.fbx` — Khoang Rover Hơi Nông Theo Chiều Dọc**:
   - **Phát hiện**: Cửa khoang (`1.94 m W × 1.96 m H`) và dốc `RampDoor` (`1.92 m W`) vừa vặn cho 07 (`1.473 m W × 1.591 m H`), nhưng chiều sâu lòng khoang từ `Bay_Connector` (`Y = -0.49 m`) đến bản lề dốc (`Y = +1.66 m`) là **`2.150 m`**, chỉ dư **`0.136 m`** so với chiều dài `2.014 m` của 07.
   - **`[PROPOSAL]` (`CargoLander.fbx`)**: Lùi vách trong `Bay_Connector` thêm **`-0.35 m`** (`Y: -0.49 m -> -0.84 m`, tăng chiều sâu khoang lên **`2.50 m`**).

7. **`[PROPOSAL]` `Smelter.fbx`, `StorageRack.fbx`, `PowerPylon.fbx`, `BatteryBank.fbx` — Độ Cao Tương Tác Với Tầm Với Của 07**:
   - **`Smelter.fbx`**: Miệng phễu nạp quặng `Hopper` nằm ở **`Z = 0.85..1.30 m`**, cao hơn gầu xúc `ScoopBucket` (`Z = 0.12..0.34 m`) của 07 tới `+0.51 m`. **`[PROPOSAL]`**: Hạ phễu `Hopper` xuống **`-0.50 m`** (`Z: 0.35..0.80 m`) hoặc thêm dốc đổ quặng.
   - **`StorageRack.fbx`**: Tầng khay trên `Cradle_4..6` đặt bản lề ở **`Z = 0.90 m`** (`Z = 0.90..1.14 m`), cao hơn đầu cắt (`0.73 m`) và nắp thùng hàng (`0.80 m`) của 07. **`[PROPOSAL]`**: Hạ chiều cao giá đỡ **`0.86×`** (`1.62 m -> 1.40 m`, đưa bản lề tầng trên về `Z = 0.68 m`).
   - **`PowerPylon.fbx`**: Cầu dao `KnifeSwitch` đặt ở **`Z = 1.22..1.39 m`** (cao hơn tầm với `0.73 m` của 07). **`[PROPOSAL]`**: Hạ `KnifeSwitch` xuống **`-0.55 m`** (`Z -> 0.75 m`).
   - **`BatteryBank.fbx`**: Ổ cắm `Socket_Power_1/2` đặt ở **`Z = 0.90 / 1.01 m`**, cao hơn hẳn dải `Z = 0.08–0.45 m` của mọi công trình khác. **`[PROPOSAL]`**: Hạ xuống **`Z = 0.35 m`**.

8. **`[PROPOSAL]` Thùng Hàng (Crates) & Đá Tảng (Rocks) — Chênh Lệch Tỷ Lệ Giữa Các Builder**:
   - **Thùng hàng (`ScatterSet.fbx` vs `StorageRack.fbx`)**: Thùng chính trong cụm `SealedCrates` của `ScatterSet.fbx` có kích thước **`0.920 × 0.680 × 0.640 m`** — lớn gấp **2.7× chiều ngang (19.5× thể tích)** so với thùng R1 trong `StorageRack.fbx` (`0.340 × 0.260 × 0.240 m`) và không thể lọt vào khoang `CargoHatch` rộng `0.58 m` của 07! **`[PROPOSAL]`**: Thu nhỏ `SealedCrates` **`0.70×`** (`-> 0.64 × 0.48 × 0.45 m`) và chuẩn hóa 3 cỡ thùng (Tier S `0.36×0.28×0.24 m`, Tier M `0.60×0.44×0.38 m`, Tier L `0.82×0.56×0.50 m`).
   - **Đá tảng (`R1/Models/Rocks` vs `Generated/Art/Rocks`)**: `Rock_00..04.fbx` của R1 có kích thước **`0.94–1.07 m Rộng × 0.46–0.62 m Cao`** (to ngang tảng đá, cao hơn gầm `0.335 m` của 07), còn `Rock_05..09.fbx` chỉ đạt **`1.47–1.55 m Rộng`** (chỉ gấp `1.5×` nhóm nhỏ), trong khi đá procedural `Rock_00..05` trải đều từ `0.35 m` đến `3.40 m`. **`[PROPOSAL]`**: Thu nhỏ `Rock_00..04.fbx` **`0.38×–0.45×`** (`-> 0.35–0.45 m`), điều chỉnh `Rock_05..06.fbx` **`0.75×–0.80×`** (`~1.2 m`), giữ `Rock_07.fbx` (`1.55 m`), và phóng `Rock_08..09.fbx` **`1.65×–2.00×`** (`-> 2.4–3.0 m`).

---

## 2. Kích Thước Mốc Chuẩn: Phi Hành Gia `1.75 m` & Rover 07

| Thông số Rover 07 (`RoverModel.fbx`) | Node | Giá trị đo thực tế | Khoảng tọa độ (Blender `X` W, `Y` L, `Z` H) | Chuẩn thiết kế |
| :--- | :--- | :--- | :--- | :--- |
| **Kích thước phủ bì tổng thể** | Toàn bộ 58 mesh | **1.4726 m W × 2.0143 m L × 1.5902 m H** | `X: [-0.7363..+0.7363]`, `Y: [-0.8793..+1.1350]`, `Z: [0.0007..1.5909]` | `1.47 m × 2.01 m × 1.59 m` |
| **Phủ bì 4 bánh xe (không tính gầu)** | `Wheel_FL..RR` | **1.4726 m W × 1.7586 m L** | `X: [-0.7363..+0.7363]`, `Y: [-0.8793..+0.8793]` | `1.47 m × 1.76 m` |
| **Thân vỏ (`Body`)** | `Body` | **0.9600 m W × 1.6510 m L × 0.6325 m H** | `X: [-0.4800..+0.4800]`, `Y: [-0.8240..+0.8270]`, `Z: [0.3350..0.9675]` | `0.96 m × 1.65 m × 0.63 m` |
| **Độ cao đỉnh đầu (`Head`)** | `Head` | **1.5250 m** | `X: [-0.3850..+0.3850]`, `Y: [-0.2114..+0.4550]`, `Z: [1.0100..1.5250]` | `1.525 m` |
| **Độ cao đỉnh ăng-ten (`AntennaTip`)** | `AntennaTip` | **1.5909 m** | Pivot `(0.36, -0.46, 1.57)`, đỉnh `Z = 1.5909 m` | `1.591 m` |
| **Đường kính & bề rộng lốp xe** | `Wheel_FL..RR` | **Ø 0.6986 m × 0.2566 m W** | `Z: [0.0007..0.6993]`, tâm trục `Z = 0.3500 m` | `Ø 0.70 m × 0.26 m W` |
| **Vệt bánh xe (Wheel Track)** | `Wheel_FL / FR` | **1.2200 m** | Tâm bánh tại `X = ±0.6100 m` (`1.4726 m` phủ bì ngoài, `0.9594 m` lọt lòng) | `1.220 m` |
| **Chiều dài cơ sở (Wheelbase)** | `Wheel_FL / RL` | **1.0600 m** | Tâm bánh tại `Y = ±0.5300 m` | `1.060 m` |
| **Khoảng sáng gầm (Ground Clearance)** | `Body` / `ScoopBucket` | **0.3350 m** (gầm) / **0.1219 m** (mép gầu) | Đáy `Body` tại `Z = 0.3350 m`; đáy `WinchSocket` tại `Z = 0.3125 m` | `0.335 m` |
| **Màn hình CRT (`Screen`)** | `Screen` | **0.5800 m W × 0.3900 m H** | `X: [-0.2900..+0.2900]`, `Z: [1.0650..1.4550]`, tâm `Z = 1.2600 m` | `0.58 m × 0.39 m` |
| **Ống kính quét (`Eye`)** | `Eye` / `EyeOrigin` | **Ø 0.2448 m** tại **Z = 1.2300 m** | Pivot `(-0.365, 0.040, 1.230)`, mặt kính `LensGlass` Ø `0.180 m` | `Z = 1.230 m` |

---

## 3. Bảng Đo Đạc Toàn Bộ 39 Model FBX R1 (`Assets/_Project/Art/R1/Models/`)

| Nhóm | Tên Model | Contract JSON | Rộng X (m) | Dài Y (m) | Cao Z (m) | Khoảng X (m) | Khoảng Y (m) | Khoảng Z (m) | Dung sai | Tam giác | Đánh giá & `[PROPOSAL]` |
| :--- | :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :--- |
| `Lander` | `CargoLander` | `cargo_lander.json` | **13.1200** | **5.2236** | **5.7500** | `[-6.56..6.56]` | `[-2.61..2.61]` | `[-0.00..5.75]` | ±0.250 m | 38,052 | `[PROPOSAL]` Deepen internal rover bay by `+0.35 m` (`Y: -0.84..+1.66 m`, `2.50 m` clear depth vs `2.15 m` today) so 07 (`2.014 m` L) has `0.24 m` fore/aft margin. |
| `Rocks` | `Rock_00` | `rock_00.json` | **0.9680** | **0.8437** | **0.5600** | `[-0.48..0.49]` | `[-0.42..0.42]` | `[-0.10..0.46]` | ±0.048 m | 96 | `[PROPOSAL]` Scale `0.38×` (`0.97×0.84×0.56 m -> 0.37×0.32×0.21 m`) so `Rock_00..04` act as true cobbles/small rocks (`0.30–0.45 m`) below 07's `0.335 m` belly clearance. |
| `Rocks` | `Rock_01` | `rock_01.json` | **1.0140** | **0.7860** | **0.6200** | `[-0.50..0.51]` | `[-0.39..0.40]` | `[-0.11..0.51]` | ±0.051 m | 96 | `[PROPOSAL]` Scale `0.38×` (`1.01×0.79×0.62 m -> 0.39×0.30×0.24 m`). |
| `Rocks` | `Rock_02` | `rock_02.json` | **1.0721** | **0.8749** | **0.4600** | `[-0.53..0.54]` | `[-0.45..0.42]` | `[-0.08..0.38]` | ±0.054 m | 96 | `[PROPOSAL]` Scale `0.38×` (`1.07×0.87×0.46 m -> 0.41×0.33×0.17 m`). |
| `Rocks` | `Rock_03` | `rock_03.json` | **0.9415** | **0.8248** | **0.5800** | `[-0.47..0.47]` | `[-0.42..0.40]` | `[-0.10..0.48]` | ±0.047 m | 96 | `[PROPOSAL]` Scale `0.42×` (`0.94×0.82×0.58 m -> 0.40×0.35×0.24 m`). |
| `Rocks` | `Rock_04` | `rock_04.json` | **0.9682** | **0.7968** | **0.5200** | `[-0.46..0.51]` | `[-0.40..0.40]` | `[-0.09..0.43]` | ±0.048 m | 96 | `[PROPOSAL]` Scale `0.45×` (`0.97×0.80×0.52 m -> 0.44×0.36×0.23 m`). |
| `Rocks` | `Rock_05` | `rock_05.json` | **1.5334** | **1.3233** | **0.8800** | `[-0.78..0.75]` | `[-0.67..0.65]` | `[-0.14..0.74]` | ±0.077 m | 288 | `[PROPOSAL]` Scale `0.75×` (`1.53×1.32×0.88 m -> 1.15×0.99×0.66 m`) for medium boulder tier (`1.0–1.2 m`). |
| `Rocks` | `Rock_06` | `rock_06.json` | **1.5224** | **1.2164** | **1.0200** | `[-0.78..0.74]` | `[-0.55..0.67]` | `[-0.16..0.86]` | ±0.076 m | 284 | `[PROPOSAL]` Scale `0.80×` (`1.52×1.22×1.02 m -> 1.22×0.97×0.82 m`) for medium boulder tier. |
| `Rocks` | `Rock_07` | `rock_07.json` | **1.5465** | **1.3753** | **0.8200** | `[-0.82..0.73]` | `[-0.63..0.74]` | `[-0.13..0.69]` | ±0.077 m | 288 | Pass as medium-large boulder (`1.55 × 1.38 × 0.82 m`). |
| `Rocks` | `Rock_08` | `rock_08.json` | **1.4705** | **1.3091** | **0.8987** | `[-0.78..0.69]` | `[-0.68..0.63]` | `[-0.15..0.75]` | ±0.074 m | 284 | `[PROPOSAL]` Scale `1.65×` (`1.47×1.31×0.90 m -> 2.43×2.16×1.48 m`) to restore large boulder tier (`2.4–3.2 m`). |
| `Rocks` | `Rock_09` | `rock_09.json` | **1.5116** | **1.3876** | **0.9400** | `[-0.76..0.75]` | `[-0.68..0.71]` | `[-0.15..0.79]` | ±0.076 m | 288 | `[PROPOSAL]` Scale `2.00×` (`1.51×1.39×0.94 m -> 3.02×2.78×1.88 m`) to match procedural `Rock_05` (`3.40 m`) large obstacle tier. |
| `Rover` | `FocusLens` | `focus_lens.json` | **0.1160** | **0.1010** | **0.1345** | `[-0.11..0.00]` | `[-0.07..0.04]` | `[-0.06..0.08]` | ±0.020 m | 2,340 | Pass (matches 07 socket & clearance contract). |
| `Rover` | `HoverCoils` | `hover_coils.json` | **0.6420** | **0.6120** | **0.1520** | `[-0.32..0.32]` | `[-0.31..0.31]` | `[-0.15..0.00]` | ±0.032 m | 2,996 | Pass (matches 07 socket & clearance contract). |
| `Rover` | `Kit_BatterySpine` | `kit_battery_spine.json` | **0.1030** | **0.4000** | **0.1060** | `[-0.06..0.05]` | `[-0.20..0.20]` | `[-0.00..0.10]` | ±0.020 m | 2,976 | Pass (matches 07 socket & clearance contract). |
| `Rover` | `Kit_CapacitorDrum` | `kit_capacitor_drum.json` | **0.1523** | **0.3840** | **0.1568** | `[-0.01..0.15]` | `[-0.19..0.19]` | `[-0.10..0.06]` | ±0.020 m | 2,992 | Pass (matches 07 socket & clearance contract). |
| `Rover` | `Kit_CargoRack` | `kit_cargo_rack.json` | **0.7320** | **0.3210** | **0.4250** | `[-0.37..0.37]` | `[-0.47..-0.15]` | `[-0.01..0.41]` | ±0.037 m | 1,904 | Pass (matches 07 socket & clearance contract). |
| `Rover` | `Kit_LampBar` | `kit_lamp_bar.json` | **0.7670** | **0.2694** | **0.1902** | `[-0.38..0.38]` | `[0.04..0.31]` | `[-0.17..0.02]` | ±0.038 m | 2,768 | Pass (matches 07 socket & clearance contract). |
| `Rover` | `Kit_SideBin` | `kit_side_bin.json` | **0.1465** | **0.3192** | **0.1600** | `[-0.00..0.14]` | `[-0.16..0.16]` | `[-0.08..0.08]` | ±0.020 m | 1,612 | Pass (matches 07 socket & clearance contract). |
| `Rover` | `Kit_TapeDeck` | `tape_deck.json` | **0.0960** | **0.2184** | **0.2184** | `[-0.04..0.06]` | `[-0.11..0.11]` | `[-0.11..0.11]` | ±0.020 m | 1,372 | Pass (matches 07 socket & clearance contract). |
| `Rover` | `Kit_TetherRing` | `kit_tether_ring.json` | **0.5772** | **0.0930** | **0.3280** | `[-0.29..0.29]` | `[0.17..0.26]` | `[-0.13..0.19]` | ±0.029 m | 1,984 | Pass (matches 07 socket & clearance contract). |
| `Rover` | `LensGlass` | `lens_glass.json` | **0.0400** | **0.1800** | **0.1800** | `[-0.07..-0.03]` | `[-0.09..0.09]` | `[-0.09..0.09]` | ±0.020 m | 1,672 | Pass (matches 07 socket & clearance contract). |
| `Rover` | `RoverModel` | `rover.json` | **1.4726** | **2.0143** | **1.5902** | `[-0.74..0.74]` | `[-0.88..1.14]` | `[0.00..1.59]` | ±0.101 m | 57,980 | **Canonical Anchor** (`1.473 × 2.014 × 1.591 m`; wheel footprint `1.473 × 1.759 m`, head `1.525 m`). |
| `Rover` | `ScanFan` | `scan_fan.json` | **0.1451** | **0.1239** | **0.0940** | `[-0.16..-0.01]` | `[-0.04..0.08]` | `[-0.04..0.05]` | ±0.020 m | 2,980 | Pass (matches 07 socket & clearance contract). |
| `Rover` | `SolarWing` | `solar_wing.json` | **0.4000** | **0.2300** | **0.0238** | `[-0.20..0.20]` | `[-0.22..0.01]` | `[-0.01..0.01]` | ±0.020 m | 1,664 | Pass (matches 07 socket & clearance contract). |
| `Ruins` | `FallenSolarArray` | `fallen_solar_array.json` | **6.7515** | **2.4479** | **1.0589** | `[-3.31..3.44]` | `[-1.12..1.33]` | `[-0.07..0.99]` | ±0.250 m | 4,268 | Pass (`6.75 × 2.45 × 1.06 m` wrecked wing). |
| `Ruins` | `HabitatModules` | `habitat_modules.json` | **10.5400** | **3.2100** | **3.2221** | `[-5.05..5.49]` | `[-1.60..1.60]` | `[-0.02..3.21]` | ±0.250 m | 11,828 | `[PROPOSAL]` Resize `ModuleA_Main` airlock door `1.15× W × 1.55× H` (leaf `0.96×1.32 m -> 1.10×2.05 m`, frame `1.22×1.48 m -> 1.36×2.20 m`, keypad `Z: 0.84 -> 1.25 m`) to fit a `1.75 m` suited human. |
| `Ruins` | `KenjiWorkshop` | `kenji_workshop.json` | **5.2596** | **5.5800** | **3.0800** | `[-2.63..2.63]` | `[-2.77..2.81]` | `[-0.00..3.08]` | ±0.250 m | 9,896 | `[PROPOSAL]` Raise `HoistRail` by `+0.48 m` (`Z: 1.268 -> 1.750 m`) and `HangingLamp` by `+0.40 m` (`Z: 1.350 -> 1.750 m`) to clear 07 (`1.591 m`); scale storage cabinet height `1.55×` (`1.16 -> 1.80 m`). |
| `Ruins` | `RadioTower` | `radio_tower.json` | **5.8331** | **3.7527** | **7.2959** | `[-2.86..2.97]` | `[-1.86..1.90]` | `[-0.08..7.22]` | ±0.250 m | 9,540 | Pass (`5.83 × 3.75 × 7.30 m` leaning truss + base bunker). |
| `Ruins` | `ScatterSet` | `scatter_set.json` | **4.4400** | **2.3451** | **1.4262** | `[-2.36..2.08]` | `[-1.22..1.13]` | `[-0.04..1.39]` | ±0.222 m | 7,880 | `[PROPOSAL]` Scale `SealedCrates` by `0.70×` (`0.92×0.68×0.64 m -> 0.64×0.48×0.45 m`) to match medium field crates, or standardize 3 crate tiers. |
| `Structures` | `AntennaMast` | `antenna_mast.json` | **1.5534** | **1.2646** | **2.7970** | `[-0.63..0.93]` | `[-0.63..0.63]` | `[-0.00..2.80]` | ±0.140 m | 5,252 | Pass (`1.55 × 1.26 × 2.80 m`; `Socket_Power_1` at `Z = 0.44 m`). |
| `Structures` | `BatteryBank` | `battery_bank.json` | **1.4618** | **0.9870** | **1.5975** | `[-0.77..0.69]` | `[-0.47..0.52]` | `[-0.00..1.60]` | ±0.080 m | 5,446 | `[PROPOSAL]` Lower `Socket_Power_1/2` by `-0.55 m` (`Z: 0.90/1.01 m -> 0.35 m`) to match the `0.08–0.45 m` rover-socket band on all other structures. |
| `Structures` | `ChargingPad` | `charging_pad.json` | **2.6150** | **2.6613** | **1.5500** | `[-1.31..1.31]` | `[-1.35..1.31]` | `[-0.00..1.55]` | ±0.133 m | 4,696 | `[PROPOSAL]` Widen `GuideRail_L/R` center X from `±0.68 m` (`1.258 m` clear) to `±0.86 m` (`1.62 m` clear), move wheel lanes from `±0.48 m` to `±0.61 m` (matching 07's `1.22 m` track), widen front ramp `1.63×` (`0.98 -> 1.60 m`), and lengthen deck `+0.54 m` (`2.66 -> 3.20 m`, moving rear bollard to `Y = -1.10 m`). |
| `Structures` | `Fabricator` | `fabricator.json` | **2.2120** | **1.5614** | **1.8400** | `[-1.11..1.11]` | `[-0.61..0.95]` | `[-0.00..1.84]` | ±0.111 m | 5,484 | Pass (`2.21 × 1.56 × 1.84 m`; workbed at `Z = 0.82 m`, `Socket_Power_1/2` at `Z = 0.31/0.45 m`). |
| `Structures` | `PowerPylon` | `power_pylon.json` | **0.7770** | **0.8030** | **1.9457** | `[-0.40..0.37]` | `[-0.37..0.43]` | `[-0.00..1.95]` | ±0.097 m | 3,264 | `[PROPOSAL]` Lower `KnifeSwitch` by `-0.55 m` (`Z: 1.22..1.39 m -> 0.67..0.84 m`) to sit within 07's `0.73 m` manipulator reach. |
| `Structures` | `Smelter` | `smelter.json` | **3.2668** | **1.8590** | **2.3800** | `[-1.59..1.68]` | `[-0.94..0.91]` | `[-0.00..2.38]` | ±0.163 m | 5,340 | `[PROPOSAL]` Lower `Hopper` / `HopperDoor` feed mouth by `-0.50 m` (`Z: 0.85..1.30 m -> 0.35..0.80 m`, hinge `0.65 -> 0.25 m`) or add a `0.55 m` feed ramp so 07's `ScoopBucket` (`Z = 0.12..0.34 m`) can dump ore into it. |
| `Structures` | `SolarArray` | `solar_array.json` | **2.4059** | **1.5662** | **1.9385** | `[-1.21..1.20]` | `[-0.88..0.69]` | `[-0.00..1.94]` | ±0.120 m | 3,616 | Pass (`2.41 × 1.57 × 1.94 m`; `Socket_Power_1` at `Z = 0.30 m`, `BreakerHandle` at `Z = 0.72 m`). |
| `Structures` | `StorageRack` | `storage_rack.json` | **1.7890** | **0.9175** | **1.6200** | `[-0.89..0.89]` | `[-0.49..0.43]` | `[-0.00..1.62]` | ±0.089 m | 5,800 | `[PROPOSAL]` Lower upper tier `Cradle_4..6` by `-0.22 m` (`Z: 0.90 -> 0.68 m`) and overall height `0.86×` (`1.62 -> 1.40 m`) so upper bins sit below 07's `0.73 m` manipulator/cutter reach and `0.80 m` cargo hatch. |
| `Structures` | `UpgradeBay` | `upgrade_bay.json` | **3.1956** | **2.5300** | **2.7450** | `[-1.60..1.60]` | `[-1.26..1.26]` | `[-0.00..2.75]` | ±0.160 m | 8,484 | `[PROPOSAL]` Move `GuideRail_L/R` from `X = ±0.72 m` (`0.056 m` trough) to `X = ±0.61 m` with `0.32 m` trough width (`5.7×` wider trough) to fit 07's `0.257 m` tyres, and retract `WrenchArm_L/R` rest pose outward by `0.35 m` per side (`X = ±0.55 m`, `1.10 m` clear width vs `0.53 m` today) to clear 07's `0.77 m` Head. |
| `Structures` | `VehiclePad` | `vehicle_pad.json` | **3.7605** | **3.3889** | **2.2851** | `[-2.05..1.71]` | `[-1.51..1.88]` | `[-0.00..2.28]` | ±0.188 m | 6,492 | `[PROPOSAL]` Widen `RampLip` by `1.25×` (`1.405 m` clear -> `1.76 m` clear, `1.95 m` outer) to clear 07's `1.473 m` tyres, and raise `HoistHook` rest Z by `+0.52 m` (`1.233 -> 1.750 m`) to clear 07 (`1.591 m`). |

---

## 4. Bảng Đo Đạc Toàn Bộ 116 Prefab Sinh Thủ Tục (`Assets/_Project/Generated/Art/`)

| Nhóm | Tên Prefab | Đường dẫn Asset | Rộng Lõi X (m) | Dài Lõi Z (m) | Cao Lõi Y (m) | Phủ bì Renderer `W × L × H` (m) | Tam giác |
| :--- | :--- | :--- | :---: | :---: | :---: | :---: | :---: |
| `Base` | `CassetteShelf` | `Assets/_Project/Generated/Art/Base/CassetteShelf.prefab` | **0.7564** | **0.3226** | **1.2267** | `1.1240 × 0.6650 × 1.2367` | 13 |
| `Base` | `Lander` | `Assets/_Project/Generated/Art/Base/Lander.prefab` | **21.9445** | **25.4713** | **8.9174** | `22.3445 × 25.6655 × 8.9174` | 168 |
| `Base` | `MuseumShelf` | `Assets/_Project/Generated/Art/Base/MuseumShelf.prefab` | **3.1563** | **1.2311** | **2.0183** | `3.2803 × 1.3311 × 2.0183` | 12 |
| `Base` | `RadioTower_L1` | `Assets/_Project/Generated/Art/Base/RadioTower_L1.prefab` | **2.4709** | **2.6584** | **9.4765** | `2.4709 × 2.8031 × 9.4765` | 16 |
| `Base` | `RadioTower_L2` | `Assets/_Project/Generated/Art/Base/RadioTower_L2.prefab` | **2.4709** | **2.6584** | **12.7288** | `2.4709 × 2.8031 × 12.7288` | 16 |
| `Base` | `RadioTower_L3` | `Assets/_Project/Generated/Art/Base/RadioTower_L3.prefab` | **2.4709** | **2.6584** | **15.9286** | `2.4709 × 2.8031 × 15.9286` | 16 |
| `Base` | `RoverBay` | `Assets/_Project/Generated/Art/Base/RoverBay.prefab` | **6.2349** | **6.3639** | **4.8502** | `6.4067 × 6.5754 × 4.8502` | 35 |
| `Base` | `ShelfCrate_Metal` | `Assets/_Project/Generated/Art/Base/ShelfCrate_Metal.prefab` | **0.3000** | **0.3490** | **0.1240** | `0.3000 × 0.3490 × 0.1240` | 1 |
| `Base` | `ShelfCrate_Optics` | `Assets/_Project/Generated/Art/Base/ShelfCrate_Optics.prefab` | **0.3000** | **0.3490** | **0.1240** | `0.3000 × 0.3490 × 0.1240` | 1 |
| `Base` | `ShelfCrate_OrangeBeads` | `Assets/_Project/Generated/Art/Base/ShelfCrate_OrangeBeads.prefab` | **0.3000** | **0.3490** | **0.1241** | `0.3000 × 0.3490 × 0.1241` | 1 |
| `Base` | `ShelfCrate_Wiring` | `Assets/_Project/Generated/Art/Base/ShelfCrate_Wiring.prefab` | **0.3000** | **0.3490** | **0.1232** | `0.3000 × 0.3490 × 0.1232` | 1 |
| `Base` | `StationPad_160` | `Assets/_Project/Generated/Art/Base/StationPad_160.prefab` | **3.3175** | **3.3193** | **0.0310** | `3.3175 × 3.3193 × 0.0310` | 7 |
| `Base` | `StationPad_240` | `Assets/_Project/Generated/Art/Base/StationPad_240.prefab` | **4.9173** | **4.9626** | **0.0310** | `4.9173 × 4.9626 × 0.0310` | 7 |
| `Base` | `StationPad_300` | `Assets/_Project/Generated/Art/Base/StationPad_300.prefab` | **6.2020** | **6.1161** | **0.0310** | `6.2020 × 6.1161 × 0.0310` | 7 |
| `Fragments` | `Fragment_BoostCoils_0` | `Assets/_Project/Generated/Art/Fragments/Fragment_BoostCoils_0.prefab` | **0.9032** | **0.5610** | **0.4696** | `0.9032 × 0.5610 × 0.4696` | 6 |
| `Fragments` | `Fragment_BoostCoils_1` | `Assets/_Project/Generated/Art/Fragments/Fragment_BoostCoils_1.prefab` | **0.8600** | **0.5962** | **0.4101** | `0.8600 × 0.5962 × 0.4101` | 6 |
| `Fragments` | `Fragment_BoostCoils_2` | `Assets/_Project/Generated/Art/Fragments/Fragment_BoostCoils_2.prefab` | **1.4536** | **0.8752** | **0.7575** | `1.4536 × 0.8752 × 0.7575` | 6 |
| `Fragments` | `Fragment_CargoCradle_0` | `Assets/_Project/Generated/Art/Fragments/Fragment_CargoCradle_0.prefab` | **0.6522** | **1.0406** | **0.5284** | `0.6522 × 1.0406 × 0.5284` | 6 |
| `Fragments` | `Fragment_CargoCradle_1` | `Assets/_Project/Generated/Art/Fragments/Fragment_CargoCradle_1.prefab` | **0.7144** | **0.7450** | **0.3973** | `0.7144 × 0.7450 × 0.3973` | 6 |
| `Fragments` | `Fragment_CellPack_0` | `Assets/_Project/Generated/Art/Fragments/Fragment_CellPack_0.prefab` | **0.5414** | **0.4074** | **0.2901** | `0.5414 × 0.4074 × 0.2901` | 6 |
| `Fragments` | `Fragment_CellPack_1` | `Assets/_Project/Generated/Art/Fragments/Fragment_CellPack_1.prefab` | **0.4932** | **0.3638** | **0.2427** | `0.4932 × 0.3638 × 0.2427` | 6 |
| `Fragments` | `Fragment_CellPack_2` | `Assets/_Project/Generated/Art/Fragments/Fragment_CellPack_2.prefab` | **0.9234** | **0.4466** | **0.3638** | `0.9234 × 0.4466 × 0.3638` | 6 |
| `Fragments` | `Fragment_FocusLens_0` | `Assets/_Project/Generated/Art/Fragments/Fragment_FocusLens_0.prefab` | **0.3482** | **0.4372** | **0.2513** | `0.3482 × 0.4372 × 0.2513` | 6 |
| `Fragments` | `Fragment_FocusLens_1` | `Assets/_Project/Generated/Art/Fragments/Fragment_FocusLens_1.prefab` | **0.5042** | **0.4350** | **0.1644** | `0.5042 × 0.4350 × 0.1644` | 6 |
| `Fragments` | `Fragment_FocusLens_2` | `Assets/_Project/Generated/Art/Fragments/Fragment_FocusLens_2.prefab` | **0.4808** | **0.4810** | **0.3311** | `0.4808 × 0.4810 × 0.3311` | 6 |
| `Fragments` | `Fragment_HoverJump_0` | `Assets/_Project/Generated/Art/Fragments/Fragment_HoverJump_0.prefab` | **0.4650** | **0.4692** | **0.3887** | `0.4650 × 0.4692 × 0.3887` | 6 |
| `Fragments` | `Fragment_HoverJump_1` | `Assets/_Project/Generated/Art/Fragments/Fragment_HoverJump_1.prefab` | **0.9102** | **0.5048** | **0.4067** | `0.9102 × 0.5048 × 0.4067` | 6 |
| `Fragments` | `Fragment_HoverJump_2` | `Assets/_Project/Generated/Art/Fragments/Fragment_HoverJump_2.prefab` | **0.5164** | **0.5864** | **0.8487** | `0.5164 × 0.5864 × 0.8487` | 6 |
| `Fragments` | `Fragment_SideBins_0` | `Assets/_Project/Generated/Art/Fragments/Fragment_SideBins_0.prefab` | **0.9472** | **0.6824** | **0.7926** | `0.9472 × 0.6824 × 0.7926` | 6 |
| `Fragments` | `Fragment_SideBins_1` | `Assets/_Project/Generated/Art/Fragments/Fragment_SideBins_1.prefab` | **1.7296** | **0.7574** | **0.6730** | `1.7296 × 0.7574 × 0.6730` | 6 |
| `Fragments` | `Fragment_WarmHeadlamp_0` | `Assets/_Project/Generated/Art/Fragments/Fragment_WarmHeadlamp_0.prefab` | **0.4412** | **0.4450** | **0.3187** | `0.4412 × 0.4450 × 0.3187` | 6 |
| `Fragments` | `Fragment_WarmHeadlamp_1` | `Assets/_Project/Generated/Art/Fragments/Fragment_WarmHeadlamp_1.prefab` | **0.8398** | **0.5928** | **0.7365** | `0.8398 × 0.5928 × 0.7365` | 6 |
| `Friends` | `Bell` | `Assets/_Project/Generated/Art/Friends/Bell.prefab` | **1.2594** | **0.6770** | **1.9060** | `1.2594 × 0.6770 × 1.9060` | 25 |
| `Friends` | `Bell_Broken` | `Assets/_Project/Generated/Art/Friends/Bell_Broken.prefab` | **1.7921** | **1.6161** | **1.2351** | `1.7921 × 1.6161 × 1.2351` | 25 |
| `Friends` | `Part_BellCone` | `Assets/_Project/Generated/Art/Friends/Part_BellCone.prefab` | **0.3110** | **0.3058** | **0.2672** | `0.3110 × 0.3058 × 0.2672` | 1 |
| `Friends` | `Part_BellKnob` | `Assets/_Project/Generated/Art/Friends/Part_BellKnob.prefab` | **0.3323** | **0.1824** | **0.2650** | `0.3323 × 0.1824 × 0.2650` | 1 |
| `Friends` | `Part_BellValve` | `Assets/_Project/Generated/Art/Friends/Part_BellValve.prefab` | **0.1954** | **0.1256** | **0.3175** | `0.1954 × 0.1256 × 0.3175` | 1 |
| `Friends` | `Part_TillyCell` | `Assets/_Project/Generated/Art/Friends/Part_TillyCell.prefab` | **0.3080** | **0.1600** | **0.1506** | `0.3080 × 0.1600 × 0.1506` | 1 |
| `Friends` | `Part_TillyLens` | `Assets/_Project/Generated/Art/Friends/Part_TillyLens.prefab` | **0.2609** | **0.2041** | **0.3191** | `0.2609 × 0.2041 × 0.3191` | 1 |
| `Friends` | `Part_TillyRotor` | `Assets/_Project/Generated/Art/Friends/Part_TillyRotor.prefab` | **0.3625** | **0.1860** | **0.3077** | `0.3625 × 0.1860 × 0.3077` | 1 |
| `Friends` | `Tilly` | `Assets/_Project/Generated/Art/Friends/Tilly.prefab` | **0.5692** | **0.5692** | **0.6170** | `0.5692 × 0.5692 × 0.6170` | 12 |
| `Friends` | `Tilly_Broken` | `Assets/_Project/Generated/Art/Friends/Tilly_Broken.prefab` | **0.5299** | **0.5747** | **0.6509** | `0.5299 × 0.5747 × 0.6509` | 12 |
| `Landmarks` | `ChasmBridge_Cable` | `Assets/_Project/Generated/Art/Landmarks/ChasmBridge_Cable.prefab` | **14.0756** | **2.5736** | **0.2368** | `14.0756 × 2.5736 × 0.2368` | 1 |
| `Landmarks` | `ChasmBridge_Far` | `Assets/_Project/Generated/Art/Landmarks/ChasmBridge_Far.prefab` | **0.7000** | **3.1850** | **5.4818** | `1.1000 × 3.5569 × 5.4818` | 3 |
| `Landmarks` | `ChasmBridge_Near` | `Assets/_Project/Generated/Art/Landmarks/ChasmBridge_Near.prefab` | **3.1000** | **3.2332** | **5.8718** | `3.5200 × 3.6044 × 5.8718` | 2 |
| `Landmarks` | `DishFrame_0` | `Assets/_Project/Generated/Art/Landmarks/DishFrame_0.prefab` | **6.5267** | **3.5215** | **5.7681** | `6.5726 × 3.9033 × 5.7681` | 2 |
| `Landmarks` | `DishFrame_1` | `Assets/_Project/Generated/Art/Landmarks/DishFrame_1.prefab` | **5.7870** | **3.7024** | **6.1700** | `5.9586 × 4.0840 × 6.1700` | 2 |
| `Landmarks` | `DishFrame_2` | `Assets/_Project/Generated/Art/Landmarks/DishFrame_2.prefab` | **3.5843** | **6.4217** | **5.5330** | `3.7559 × 6.4217 × 5.5330` | 2 |
| `Landmarks` | `RoutePost` | `Assets/_Project/Generated/Art/Landmarks/RoutePost.prefab` | **0.2200** | **0.2200** | **1.3500** | `0.6200 × 0.6044 × 1.3600` | 3 |
| `Pickups` | `BlueprintTube` | `Assets/_Project/Generated/Art/Pickups/BlueprintTube.prefab` | **0.4740** | **0.1008** | **0.1060** | `0.4740 × 0.1008 × 0.1060` | 1 |
| `Pickups` | `Cassette_after_dark_1` | `Assets/_Project/Generated/Art/Pickups/Cassette_after_dark_1.prefab` | **0.1800** | **0.0462** | **0.1147** | `0.1800 × 0.0462 × 0.1147` | 1 |
| `Pickups` | `Cassette_dust_and_honey` | `Assets/_Project/Generated/Art/Pickups/Cassette_dust_and_honey.prefab` | **0.1800** | **0.0462** | **0.1147** | `0.1800 × 0.0462 × 0.1147` | 1 |
| `Pickups` | `Cassette_slow_orbit` | `Assets/_Project/Generated/Art/Pickups/Cassette_slow_orbit.prefab` | **0.1800** | **0.0462** | **0.1147** | `0.1800 × 0.0462 × 0.1147` | 1 |
| `Pickups` | `Material_Metal` | `Assets/_Project/Generated/Art/Pickups/Material_Metal.prefab` | **0.3558** | **0.2529** | **0.1430** | `0.3558 × 0.2529 × 0.1430` | 1 |
| `Pickups` | `Material_Optics` | `Assets/_Project/Generated/Art/Pickups/Material_Optics.prefab` | **0.3331** | **0.2550** | **0.1430** | `0.3331 × 0.2550 × 0.1430` | 1 |
| `Pickups` | `Material_OrangeBeads` | `Assets/_Project/Generated/Art/Pickups/Material_OrangeBeads.prefab` | **0.3253** | **0.3172** | **0.2721** | `0.3253 × 0.3172 × 0.2721` | 1 |
| `Pickups` | `Material_Wiring` | `Assets/_Project/Generated/Art/Pickups/Material_Wiring.prefab` | **0.2936** | **0.3330** | **0.1775** | `0.2936 × 0.3330 × 0.1775` | 1 |
| `Pickups` | `Part_RelaySpare` | `Assets/_Project/Generated/Art/Pickups/Part_RelaySpare.prefab` | **0.3497** | **0.3270** | **0.4372** | `0.3497 × 0.3270 × 0.4372` | 1 |
| `Pickups` | `Part_TowerCoil` | `Assets/_Project/Generated/Art/Pickups/Part_TowerCoil.prefab` | **0.3440** | **0.3272** | **0.3550** | `0.3440 × 0.3272 × 0.3550` | 1 |
| `Pickups` | `Part_Transformer` | `Assets/_Project/Generated/Art/Pickups/Part_Transformer.prefab` | **0.4324** | **0.2526** | **0.3502** | `0.4324 × 0.2526 × 0.3502` | 1 |
| `Props` | `Cabinet` | `Assets/_Project/Generated/Art/Props/Cabinet.prefab` | **0.9800** | **0.5500** | **1.1600** | `1.3000 × 0.8640 × 1.1700` | 6 |
| `Props` | `CoilCrate` | `Assets/_Project/Generated/Art/Props/CoilCrate.prefab` | **0.8240** | **0.5390** | **0.4800** | `1.2000 × 0.8774 × 0.4900` | 6 |
| `Props` | `Derelict_06` | `Assets/_Project/Generated/Art/Props/Derelict_06.prefab` | **1.3907** | **1.3893** | **1.5934** | `1.7409 × 1.5184 × 1.5934` | 9 |
| `Props` | `Derelict_Drone` | `Assets/_Project/Generated/Art/Props/Derelict_Drone.prefab` | **0.6189** | **1.9780** | **3.2068** | `0.7925 × 2.1700 × 3.2068` | 7 |
| `Props` | `Derelict_HaulBot` | `Assets/_Project/Generated/Art/Props/Derelict_HaulBot.prefab` | **1.4625** | **1.9481** | **0.8030** | `1.8260 × 1.9481 × 0.8100` | 9 |
| `Props` | `Derelict_Mule` | `Assets/_Project/Generated/Art/Props/Derelict_Mule.prefab` | **1.9526** | **2.1228** | **2.0828** | `2.2108 × 2.2017 × 2.0828` | 8 |
| `Props` | `HullFragment_0` | `Assets/_Project/Generated/Art/Props/HullFragment_0.prefab` | **1.7266** | **2.2000** | **1.2719** | `1.7266 × 2.2000 × 1.2719` | 2 |
| `Props` | `HullFragment_1` | `Assets/_Project/Generated/Art/Props/HullFragment_1.prefab` | **2.1974** | **1.4139** | **0.9911** | `2.1974 × 1.4139 × 0.9911` | 2 |
| `Props` | `HullFragment_2` | `Assets/_Project/Generated/Art/Props/HullFragment_2.prefab` | **1.7454** | **1.3084** | **0.9206** | `1.7454 × 1.3084 × 0.9206` | 2 |
| `Props` | `HullFragment_3` | `Assets/_Project/Generated/Art/Props/HullFragment_3.prefab` | **1.5173** | **1.7076** | **1.3069** | `1.5173 × 1.7076 × 1.3069` | 2 |
| `Props` | `JumperWreck` | `Assets/_Project/Generated/Art/Props/JumperWreck.prefab` | **1.8244** | **1.9467** | **0.6114** | `2.2244 × 2.3268 × 0.6114` | 5 |
| `Props` | `JunctionBox` | `Assets/_Project/Generated/Art/Props/JunctionBox.prefab` | **0.4640** | **0.3190** | **0.8600** | `0.4640 × 0.4582 × 0.8700` | 6 |
| `Props` | `LightPool` | `Assets/_Project/Generated/Art/Props/LightPool.prefab` | **2.0000** | **2.0000** | **0.2400** | `2.0000 × 2.0000 × 0.2400` | 1 |
| `Props` | `LogCache` | `Assets/_Project/Generated/Art/Props/LogCache.prefab` | **0.5240** | **0.4129** | **0.3278** | `0.9080 × 0.7214 × 0.3378` | 2 |
| `Props` | `OrangeBeadPatch_0` | `Assets/_Project/Generated/Art/Props/OrangeBeadPatch_0.prefab` | **2.2912** | **2.2805** | **0.2433** | `2.2912 × 2.2805 × 0.2433` | 1 |
| `Props` | `OrangeBeadPatch_1` | `Assets/_Project/Generated/Art/Props/OrangeBeadPatch_1.prefab` | **1.8889** | **1.6472** | **0.2393** | `1.8889 × 1.6472 × 0.2393` | 1 |
| `Props` | `OrangeBeadPatch_2` | `Assets/_Project/Generated/Art/Props/OrangeBeadPatch_2.prefab` | **3.3165** | **2.5804** | **0.2443** | `3.3165 × 2.5804 × 0.2443` | 1 |
| `Props` | `RockfallPocket` | `Assets/_Project/Generated/Art/Props/RockfallPocket.prefab` | **1.4407** | **1.1776** | **0.7688** | `1.8208 × 1.3605 × 0.7788` | 3 |
| `Props` | `SpareCrate` | `Assets/_Project/Generated/Art/Props/SpareCrate.prefab` | **0.4124** | **0.1990** | **0.3084** | `0.4124 × 0.1990 × 0.3084` | 5 |
| `Props` | `SupplyPod` | `Assets/_Project/Generated/Art/Props/SupplyPod.prefab` | **2.4350** | **1.7078** | **3.7384** | `2.6411 × 1.9022 × 3.7384` | 7 |
| `Props` | `TapeCase` | `Assets/_Project/Generated/Art/Props/TapeCase.prefab` | **0.3840** | **0.2790** | **0.1600** | `0.7500 × 0.6338 × 0.1700` | 6 |
| `Props` | `TillySled` | `Assets/_Project/Generated/Art/Props/TillySled.prefab` | **1.5483** | **1.6114** | **0.9070** | `1.8574 × 1.9150 × 0.9070` | 10 |
| `Relay` | `Part_RelayModule` | `Assets/_Project/Generated/Art/Relay/Part_RelayModule.prefab` | **0.3450** | **0.1750** | **0.2920** | `0.3450 × 0.1750 × 0.2920` | 1 |
| `Relay` | `RelayHut` | `Assets/_Project/Generated/Art/Relay/RelayHut.prefab` | **3.2225** | **2.1842** | **2.2839** | `3.4225 × 2.4822 × 2.2914` | 2 |
| `Relay` | `RelayMast` | `Assets/_Project/Generated/Art/Relay/RelayMast.prefab` | **5.4800** | **7.7157** | **9.9518** | `5.8800 × 8.0477 × 9.9518` | 10 |
| `Relay` | `RelayMast_Broken` | `Assets/_Project/Generated/Art/Relay/RelayMast_Broken.prefab` | **5.4800** | **7.7157** | **9.7094** | `5.8800 × 8.0270 × 9.7094` | 10 |
| `Relics` | `Relic_astronaut_boot` | `Assets/_Project/Generated/Art/Relics/Relic_astronaut_boot.prefab` | **0.2110** | **0.4000** | **0.3841** | `0.2110 × 0.4000 × 0.3841` | 1 |
| `Relics` | `Relic_cassette_player` | `Assets/_Project/Generated/Art/Relics/Relic_cassette_player.prefab` | **0.0792** | **0.2700** | **0.1386** | `0.0792 × 0.2700 × 0.1386` | 1 |
| `Relics` | `Relic_garden_gnome` | `Assets/_Project/Generated/Art/Relics/Relic_garden_gnome.prefab` | **0.1970** | **0.2175** | **0.5000** | `0.1970 × 0.2175 × 0.5000` | 1 |
| `Relics` | `Relic_golden_record` | `Assets/_Project/Generated/Art/Relics/Relic_golden_record.prefab` | **0.4000** | **0.1163** | **0.3761** | `0.4000 × 0.1163 × 0.3761` | 1 |
| `Relics` | `Relic_rubber_duck` | `Assets/_Project/Generated/Art/Relics/Relic_rubber_duck.prefab` | **0.1592** | **0.2000** | **0.1663** | `0.1592 × 0.2000 × 0.1663` | 1 |
| `Relics` | `Relic_teapot` | `Assets/_Project/Generated/Art/Relics/Relic_teapot.prefab` | **0.2361** | **0.3500** | **0.2102** | `0.2361 × 0.3500 × 0.2102` | 1 |
| `Rocks` | `Rock_00` | `Assets/_Project/Generated/Art/Rocks/Rock_00.prefab` | **0.3500** | **0.3084** | **0.1786** | `0.3500 × 0.3084 × 0.1786` | 1 |
| `Rocks` | `Rock_01` | `Assets/_Project/Generated/Art/Rocks/Rock_01.prefab` | **0.8000** | **0.7218** | **0.5586** | `0.8000 × 0.7218 × 0.5586` | 1 |
| `Rocks` | `Rock_02` | `Assets/_Project/Generated/Art/Rocks/Rock_02.prefab` | **0.9804** | **1.0000** | **1.2134** | `0.9804 × 1.0000 × 1.2134` | 1 |
| `Rocks` | `Rock_03` | `Assets/_Project/Generated/Art/Rocks/Rock_03.prefab` | **1.3000** | **0.9970** | **0.3666** | `1.3000 × 0.9970 × 0.3666` | 1 |
| `Rocks` | `Rock_04` | `Assets/_Project/Generated/Art/Rocks/Rock_04.prefab` | **1.8000** | **1.7948** | **1.2080** | `1.8000 × 1.7948 × 1.2080` | 1 |
| `Rocks` | `Rock_05` | `Assets/_Project/Generated/Art/Rocks/Rock_05.prefab` | **3.4000** | **3.2234** | **2.7057** | `3.4000 × 3.2234 × 2.7057` | 1 |
| `Rover` | `FocusLens` | `Assets/_Project/Generated/Art/Rover/FocusLens.prefab` | **0.3428** | **0.0793** | **0.3407** | `0.3428 × 0.0793 × 0.3407` | 2 |
| `Rover` | `HoverCoils` | `Assets/_Project/Generated/Art/Rover/HoverCoils.prefab` | **0.5278** | **0.8000** | **0.1450** | `0.5278 × 0.8000 × 0.1450` | 9 |
| `Rover` | `Kit_BatterySpine` | `Assets/_Project/Generated/Art/Rover/Kit_BatterySpine.prefab` | **0.1004** | **0.4400** | **0.1191** | `0.1004 × 0.4400 × 0.1191` | 1 |
| `Rover` | `Kit_CapacitorDrum` | `Assets/_Project/Generated/Art/Rover/Kit_CapacitorDrum.prefab` | **0.3889** | **0.5400** | **0.4465** | `0.3889 × 0.5400 × 0.4465` | 2 |
| `Rover` | `Kit_CargoRack` | `Assets/_Project/Generated/Art/Rover/Kit_CargoRack.prefab` | **0.8704** | **0.6222** | **0.4502** | `0.8704 × 0.6222 × 0.4502` | 2 |
| `Rover` | `Kit_LampBar` | `Assets/_Project/Generated/Art/Rover/Kit_LampBar.prefab` | **1.2120** | **0.2829** | **0.7500** | `1.2120 × 0.2829 × 0.7500` | 4 |
| `Rover` | `Kit_SideBin` | `Assets/_Project/Generated/Art/Rover/Kit_SideBin.prefab` | **0.2090** | **0.3120** | **0.1920** | `0.2090 × 0.3120 × 0.1920` | 1 |
| `Rover` | `Kit_TapeDeck` | `Assets/_Project/Generated/Art/Rover/Kit_TapeDeck.prefab` | **0.0880** | **0.3000** | **0.1540** | `0.0880 × 0.3000 × 0.1540` | 1 |
| `Rover` | `Kit_TetherRing` | `Assets/_Project/Generated/Art/Rover/Kit_TetherRing.prefab` | **0.2120** | **0.0240** | **0.2120** | `0.2120 × 0.0240 × 0.2120` | 2 |
| `Rover` | `LensGlass` | `Assets/_Project/Generated/Art/Rover/LensGlass.prefab` | **0.3120** | **0.0200** | **0.3120** | `0.3120 × 0.0200 × 0.3120` | 1 |
| `Rover` | `RoverModel` | `Assets/_Project/Generated/Art/Rover/RoverModel.prefab` | **1.6300** | **2.2733** | **1.6447** | `1.6300 × 2.2733 × 1.6447` | 89 |
| `Rover` | `ScanFan` | `Assets/_Project/Generated/Art/Rover/ScanFan.prefab` | **0.0738** | **0.0129** | **0.0738** | `0.0738 × 0.0129 × 0.0738` | 1 |
| `Sites` | `GaragePort` | `Assets/_Project/Generated/Art/Sites/GaragePort.prefab` | **2.7000** | **1.3299** | **1.6800** | `2.7000 × 1.3299 × 1.6800` | 3 |
| `Sites` | `Site_depot` | `Assets/_Project/Generated/Art/Sites/Site_depot.prefab` | **12.7000** | **13.1989** | **10.0599** | `12.9088 × 13.5087 × 10.0599` | 15 |
| `Sites` | `Site_drill` | `Assets/_Project/Generated/Art/Sites/Site_drill.prefab` | **15.1612** | **11.9014** | **10.2876** | `15.5251 × 12.2069 × 10.2876` | 19 |
| `Sites` | `Site_garage` | `Assets/_Project/Generated/Art/Sites/Site_garage.prefab` | **10.8317** | **12.4099** | **9.8300** | `11.2317 × 12.6413 × 9.8300` | 20 |
| `Sites` | `Site_kestrel` | `Assets/_Project/Generated/Art/Sites/Site_kestrel.prefab` | **12.4800** | **9.4348** | **9.4690** | `12.8469 × 9.8052 × 9.4690` | 23 |
| `Sites` | `Site_lander` | `Assets/_Project/Generated/Art/Sites/Site_lander.prefab` | **8.7603** | **8.2809** | **4.2010** | `9.1265 × 8.6408 × 4.2010` | 20 |
