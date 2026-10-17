"""R1 Audio Audition Catalog — metadata, loudness targets, Vietnamese descriptions, and A/B picks.

Defines all 37 cues across the 6 R1 groups:
1. 07's CRT face (7 cues)
2. The sky (3 cues)
3. Building (8 cues)
4. Machines (10 cues)
5. The CRT UI (6 cues)
6. Ambience beds (3 loopable 60 s cues)

Loudness hierarchy (aligned with ``tools/audio/feel_loudness.md``):
- Guide ceiling: Target LUFS <= -18.0 LUFS, Effective dB <= -19.9 dB.
- Non-UI one-shots & loops: Target LUFS in [-18.5, -24.5] LUFS, Effective dB in [-20.7, -31.5] dB.
- CRT UI cues (``ui_*``): Target LUFS in [-28.5, -30.5] LUFS, Effective dB in [-34.5, -38.0] dB —
  strictly the quietest family in the entire set by Target LUFS, measured WAV LUFS, RMS, and Effective dB.
"""

from __future__ import annotations

from dataclasses import dataclass
import math
from typing import Callable

import numpy as np

from . import ambience_cues, build_cues, face_cues, machine_cues, sky_cues, ui_cues


GUIDE_MAX_TARGET_LUFS = -18.0
GUIDE_MAX_EFFECTIVE_DB = -19.9


@dataclass(frozen=True)
class CueSpec:
    cue_id: str
    group_id: str
    group_title: str
    category: str
    loop: bool
    target_lufs: float
    volume: float
    desc_a_vi: str
    desc_b_vi: str
    pick: str
    pick_reason: str
    fn: Callable[[int, np.random.Generator], np.ndarray]

    @property
    def effective_db(self) -> float:
        return self.target_lufs + 20.0 * math.log10(max(self.volume, 1e-6))


GROUPS: list[tuple[str, str, str]] = [
    ("face", "1. 07's CRT Face", "Gương mặt CRT của 07 — 7 cues"),
    ("sky", "2. The Sky", "Bầu trời Mặt Trăng & Nhật thực — 3 cues"),
    ("building", "3. Building", "Xây dựng & Lắp ráp công trình — 8 cues"),
    ("machines", "4. Machines", "Máy móc & Cơ cấu cơ điện — 10 cues"),
    ("ui", "5. The CRT UI", "Giao diện bàn đài & màn hình CRT (nhỏ nhất) — 6 cues"),
    ("ambience", "6. Ambience Beds (60 s Loopable)", "Nền không gian 60 giây lặp mượt — 3 cues"),
]


def _build_catalog() -> list[CueSpec]:
    return [
        # ===================================================================
        # 1. 07's CRT FACE (7 cues)
        # ===================================================================
        CueSpec(
            cue_id="face_boot",
            group_id="face",
            group_title="1. 07's CRT Face",
            category="oneshot_face",
            loop=False,
            target_lufs=-20.0,
            volume=0.72,
            desc_a_vi="Công tắc bakelite đóng mạch, biến áp tim đèn D2–A2–D3 ấm dần và tia quét mở mắt D4–A4 qua loa giấy.",
            desc_b_vi="Cuộn khử từ CRT rung nhẹ qua mặt nạ thép kèm bánh đà capstan khởi động và hợp âm phím nỉ D4–F#4–A4.",
            pick="A",
            pick_reason="Bản A thể hiện đúng chất bàn vô tuyến đèn (C2 Radio bench): tiếng tim đèn ấm lên và tia quét mở mắt qua màng loa giấy nghe rất gần gũi.",
            fn=face_cues.face_boot,
        ),
        CueSpec(
            cue_id="face_blink",
            group_id="face",
            group_title="1. 07's CRT Face",
            category="oneshot_face",
            loop=False,
            target_lufs=-23.5,
            volume=0.62,
            desc_a_vi="Hai nhịp lá thép rơ-le chớp nhẹ bọc nỉ kèm âm cộng hưởng màng loa nhỏ ở nốt A4–D4.",
            desc_b_vi="Lá chắn cửa trập đôi khép-mở nhanh với âm gỗ kalimba gõ dịu ở nốt F#4–D4.",
            pick="B",
            pick_reason="Bản B dùng cơ cấu lá chắn cửa trập (aperture shutter) kết hợp âm gỗ dịu cực kỳ tự nhiên cho hành động chớp mắt mà không gây mỏi tai khi lặp nhiều lần.",
            fn=face_cues.face_blink,
        ),
        CueSpec(
            cue_id="face_happy",
            group_id="face",
            group_title="1. 07's CRT Face",
            category="oneshot_face",
            loop=False,
            target_lufs=-20.5,
            volume=0.70,
            desc_a_vi="Rơ-le bước nhảy mở đầu cho câu nhạc ba nốt đi lên ấm áp D4–F#4–A4 qua màng loa giấy bàn đài.",
            desc_b_vi="Con cóc bánh răng xoay nhẹ điểm chuỗi hộp nhạc đồng & phím nỉ A4–B4–D5 trong trẻo.",
            pick="A",
            pick_reason="Bản A giữ trọn độ ấm của loa giấy cổ điển và độ ngân nhẹ của mạch đèn, nghe như 07 đang mỉm cười thật sự.",
            fn=face_cues.face_happy,
        ),
        CueSpec(
            cue_id="face_worried",
            group_id="face",
            group_title="1. 07's CRT Face",
            category="oneshot_face",
            loop=False,
            target_lufs=-21.0,
            volume=0.68,
            desc_a_vi="Hai tiếng rơ-le ngập ngừng kèm câu nhạc đi xuống A4–F#4–E4–D4 rung nhẹ kiểu đèn sụt áp.",
            desc_b_vi="Cuộn dây servo chùng nhịp và thanh lưỡi gà đồng giảm tốc F#4–E4–D4 nghe băn khoăn nhưng không báo động.",
            pick="A",
            pick_reason="Bản A truyền tải sự ngập ngừng, lo lắng nhẹ nhàng qua độ chùng áp đèn (voltage sag) đúng tinh thần cô đơn nhưng bình yên.",
            fn=face_cues.face_worried,
        ),
        CueSpec(
            cue_id="face_asleep",
            group_id="face",
            group_title="1. 07's CRT Face",
            category="oneshot_face",
            loop=False,
            target_lufs=-22.0,
            volume=0.65,
            desc_a_vi="Tiếng rì rầm tim đèn D2–A2–D3 thở chậm cùng hai nhịp chốt nỉ khép mắt êm dịu.",
            desc_b_vi="Ống bễ gỗ thở nhịp nhàng hòa cùng âm phím nỉ D3–A3–F#4 lắng dần vào giấc ngủ.",
            pick="A",
            pick_reason="Bản A tái hiện tiếng thở tim đèn (slow tube purr) ấm áp và yên tĩnh của một thiết bị vô tuyến đang nghỉ ngơi.",
            fn=face_cues.face_asleep,
        ),
        CueSpec(
            cue_id="face_startled",
            group_id="face",
            group_title="1. 07's CRT Face",
            category="oneshot_face",
            loop=False,
            target_lufs=-20.0,
            volume=0.72,
            desc_a_vi="Rơ-le nhảy kép cùng màng loa giấy bật nảy D4 lên A4–D5 rồi lắng ngay về F#4 ấm áp.",
            desc_b_vi="Lò xo cơ khí bật nảy chạm thanh chuông đồng-kính A4–D5–F#5 kèm tiếng rung khung máy ngắn.",
            pick="A",
            pick_reason="Bản A bắt được phản xạ giật mình của rơ-le và màng loa giấy rất duyên mà vẫn hoàn toàn nằm trong ngũ cung Rê trưởng êm dịu.",
            fn=face_cues.face_startled,
        ),
        CueSpec(
            cue_id="face_proud",
            group_id="face",
            group_title="1. 07's CRT Face",
            category="oneshot_face",
            loop=False,
            target_lufs=-19.5,
            volume=0.74,
            desc_a_vi="Bốn nấc chuyển mạch xoay mở ra chuỗi âm loa giấy ấm D4–F#4–A4–D5 với nền trầm D3 tự hào.",
            desc_b_vi="Mô-tơ nâng cổ nhẹ nhàng kết hợp hợp âm chuông đồng & phím nỉ D4–A4–D5–F#4 sáng ấm.",
            pick="B",
            pick_reason="Bản B có tiếng servo nâng nhẹ đầu của 07 kèm hợp âm chuông đồng và phím nỉ nghe rất tự hào và sống động.",
            fn=face_cues.face_proud,
        ),

        # ===================================================================
        # 2. THE SKY (3 cues)
        # ===================================================================
        CueSpec(
            cue_id="sky_diamond_ring",
            group_id="sky",
            group_title="2. The Sky",
            category="oneshot_sky",
            loop=False,
            target_lufs=-20.0,
            volume=0.74,
            desc_a_vi="Rơ-le viễn thông điểm nhẹ mở ra chuỗi hài âm đèn A4–D5–F#5–A5 ngân rung dịu qua băng từ.",
            desc_b_vi="Hạt bụi tĩnh điện phớt nhẹ nở thành âm thanh kính kéo cung và đàn lưỡi gà D4–A4–F#5–A5.",
            pick="B",
            pick_reason="Khoảnh khắc vòng kim cương nhật thực rất hợp với chất âm kính kéo cung (bowed glass) kết hợp đàn lưỡi gà của bản B: vừa kỳ ảo vừa hữu cơ.",
            fn=sky_cues.sky_diamond_ring,
        ),
        CueSpec(
            cue_id="sky_dawn_sweep",
            group_id="sky",
            group_title="2. The Sky",
            category="oneshot_sky",
            loop=False,
            target_lufs=-19.5,
            volume=0.75,
            desc_a_vi="Bình minh ~6 giây: mạch đèn bàn đài ấm dần từ D3–A3 sang hợp âm F#4–A4–D5 cùng nhịp rơ-le đón nắng.",
            desc_b_vi="Bình minh ~6 giây: khung vỏ trạm giãn nở nhiệt nhẹ hòa cùng đàn harmonium và chuông đồng đón nắng sớm.",
            pick="A",
            pick_reason="Bản A mang lại cảm giác cả trạm vô tuyến và cánh pin mặt trời từ từ ấm lên dưới nắng sớm, rất gắn kết với thế giới C2 Radio bench.",
            fn=sky_cues.sky_dawn_sweep,
        ),
        CueSpec(
            cue_id="sky_nightfall_settle",
            group_id="sky",
            group_title="2. The Sky",
            category="oneshot_sky",
            loop=False,
            target_lufs=-20.5,
            volume=0.72,
            desc_a_vi="Hoàng hôn lắng xuống: công tắc ngắt tải, giai điệu đèn A4–F#4–E4–D4 trầm dần về nền D3 ấm.",
            desc_b_vi="Khung thép nguội dần tích-tắc nhẹ cùng bốn nốt phím nỉ B3–A3–F#3–D3 tan vào tĩnh lặng.",
            pick="A",
            pick_reason="Bản A khép lại ngày làm việc bằng tiếng công tắc ngắt tải và âm đèn trầm ấm lắng xuống, gợi cảm giác bình yên dưới ánh Trái Đất.",
            fn=sky_cues.sky_nightfall_settle,
        ),

        # ===================================================================
        # 3. BUILDING (8 cues)
        # ===================================================================
        CueSpec(
            cue_id="build_ghost",
            group_id="building",
            group_title="3. Building",
            category="oneshot_build",
            loop=False,
            target_lufs=-23.0,
            volume=0.64,
            desc_a_vi="Rơ-le đóng nhẹ và cuộn máy chiếu CRT ngân ấm ở D3–A3–F#4 khi khung bóng công trình hiện ra.",
            desc_b_vi="Cửa trập lá đồng trượt mở trên giấy can cùng tiếng ngân thanh thủy tinh ở nốt A4–D5.",
            pick="A",
            pick_reason="Bản A đúng nghĩa tiếng máy chiếu CRT (CRT projector hum) rất nhẹ và ấm, giúp người chơi di chuyển bóng công trình liên tục mà không mỏi tai.",
            fn=build_cues.build_ghost,
        ),
        CueSpec(
            cue_id="build_snap",
            group_id="building",
            group_title="3. Building",
            category="oneshot_build",
            loop=False,
            target_lufs=-21.5,
            volume=0.68,
            desc_a_vi="Khấc xoay định vị nảy gọn cùng tiếng chốt nam châm bắt khớp ở nốt A4–D5.",
            desc_b_vi="Chốt lò xo bi thép sập vào rãnh đồng sắc gọn với âm cộng hưởng thân gỗ ngắn F#4–B4.",
            pick="B",
            pick_reason="Bản B có cảm giác cơ khí bi-lò-xo bắt vào ngàm (detent ball-lock) cực kỳ rõ ràng, phản hồi xúc giác chính xác khi căn khớp lưới xây dựng.",
            fn=build_cues.build_snap,
        ),
        CueSpec(
            cue_id="build_invalid",
            group_id="building",
            group_title="3. Building",
            category="oneshot_build",
            loop=False,
            target_lufs=-21.0,
            volume=0.68,
            desc_a_vi="Công tắc bakelite bị vướng bật trả lại kèm hai nốt trầm đi xuống E3–D3 qua màng loa giấy, không còi báo lỗi.",
            desc_b_vi="Chốt gỗ chạm đệm cao su kịch nhẹ với âm cộng hưởng thùng gỗ trầm A3–D3.",
            pick="A",
            pick_reason="Bản A truyền đạt rõ 'không đặt được' bằng tiếng công tắc vướng cơ học và nhịp trầm ấm, hoàn toàn tránh tiếng buzzer điện tử khó chịu.",
            fn=build_cues.build_invalid,
        ),
        CueSpec(
            cue_id="build_place",
            group_id="building",
            group_title="3. Building",
            category="oneshot_build",
            loop=False,
            target_lufs=-19.0,
            volume=0.76,
            desc_a_vi="Khung thép đặt xuống nền đất Mặt Trăng đầm chắc, khóa chốt và ngân ấm ở D3–A3–D4.",
            desc_b_vi="Chốt thủy lực ép chân đế xuống nền đá kèm tiếng khóa cam thép và hồi âm thân giàn D3–F#3–A3.",
            pick="A",
            pick_reason="Bản A cân bằng hoàn hảo giữa sức nặng vật lý của khung thép và âm xác nhận ấm áp của bàn đài C2.",
            fn=build_cues.build_place,
        ),
        CueSpec(
            cue_id="build_weld_loop",
            group_id="building",
            group_title="3. Building",
            category="loop_3d",
            loop=True,
            target_lufs=-21.5,
            volume=0.68,
            desc_a_vi="Vòng lặp 4 giây: tiếng biến áp hàn hồ quang ngân ấm ở D2–A2–D3 điểm những hạt lửa lách tách mềm như đĩa than.",
            desc_b_vi="Vòng lặp 4 giây: mỏ hàn dây đồng cơ điện quay đều với tiếng tiếp điểm trượt và tia lửa kim loại ấm.",
            pick="A",
            pick_reason="Bản A nghe như nghe tiếng máy hàn qua hệ thống liên lạc nội bộ bằng bóng đèn: êm, dày và lặp hoàn toàn không thấy mối nối.",
            fn=build_cues.build_weld_loop,
        ),
        CueSpec(
            cue_id="build_stage_complete",
            group_id="building",
            group_title="3. Building",
            category="oneshot_build",
            loop=False,
            target_lufs=-19.5,
            volume=0.74,
            desc_a_vi="Chốt cơ khí đóng gọn mở ra hai nốt chuông đèn đi lên A4–D5 chúc mừng hoàn tất một công đoạn.",
            desc_b_vi="Bánh cóc siết bu-lông đóng khớp cùng ba nhịp búa nỉ gõ thanh đồng F#4–A4–D5 sáng rõ.",
            pick="B",
            pick_reason="Bản B dùng tiếng chốt bánh cóc và búa nỉ gõ thanh đồng tạo cảm giác một tầng kết cấu vừa được khóa xong rất sảng khoái.",
            fn=build_cues.build_stage_complete,
        ),
        CueSpec(
            cue_id="build_structure_complete",
            group_id="building",
            group_title="3. Building",
            category="oneshot_build",
            loop=False,
            target_lufs=-18.5,
            volume=0.76,
            desc_a_vi="Công tắc chính đóng mạch, đèn báo bật sáng ấm cùng câu nhạc năm nốt D4–F#4–A4–B4–D5 ngân vang.",
            desc_b_vi="Khóa ngàm cơ khí lớn đóng sập mở ra dàn chuông đồng & phím nỉ D4–F#4–A4–D5 tròn đầy.",
            pick="A",
            pick_reason="Bản A mang lại cảm giác thành tựu tròn đầy, ấm cúng đúng hình ảnh 'ngọn đèn bật sáng' trên công trình vừa hoàn thiện.",
            fn=build_cues.build_structure_complete,
        ),
        CueSpec(
            cue_id="build_deconstruct",
            group_id="building",
            group_title="3. Building",
            category="oneshot_build",
            loop=False,
            target_lufs=-20.0,
            volume=0.72,
            desc_a_vi="Ba nhịp tháo chốt rơ-le, tiếng xả hơi giảm chấn nhẹ và câu nhạc đi xuống D5–A4–F#4–D4 thu hồi gọn gàng.",
            desc_b_vi="Tời dây cót quay ngược thu hồi kết cấu kèm chuỗi âm thanh gỗ-đồng đi xuống B4–A4–F#4–D4.",
            pick="A",
            pick_reason="Bản A kết hợp tiếng tháo chốt tuần tự, hơi xả êm và giai điệu đi xuống nhẹ nhàng, tạo cảm giác tháo dỡ cẩn thận chứ không phá hủy.",
            fn=build_cues.build_deconstruct,
        ),

        # ===================================================================
        # 4. MACHINES (10 cues)
        # ===================================================================
        CueSpec(
            cue_id="machine_solar_track",
            group_id="machines",
            group_title="4. Machines",
            category="oneshot_machine",
            loop=False,
            target_lufs=-21.5,
            volume=0.68,
            desc_a_vi="Rơ-le bước kích hoạt trục vít nâng góc pin mặt trời ngân nhẹ ở A2–D3 rồi chốt khóa êm.",
            desc_b_vi="Cơ cấu bánh cóc đồng hồ xoay từng khấc tinh tế kèm tiếng ngân khung nhôm-đồng D3–A3.",
            pick="A",
            pick_reason="Bản A có tiếng mô-tơ trục vít (worm-gear) rất êm pha chút ấm của mạch đèn, hoàn hảo cho chuyển động xoay theo mặt trời.",
            fn=machine_cues.machine_solar_track,
        ),
        CueSpec(
            cue_id="machine_wings_unfold",
            group_id="machines",
            group_title="4. Machines",
            category="oneshot_machine",
            loop=False,
            target_lufs=-19.5,
            volume=0.74,
            desc_a_vi="Chốt khóa mở, xi-lanh thủy lực đẩy êm từ D3 lên A3 cùng các nhịp bản lề vươn cánh.",
            desc_b_vi="Khung giàn pantograph bung mở nhiều đốt nối tiếp, kết thúc bằng tiếng khóa chốt cuối chắc chắn.",
            pick="B",
            pick_reason="Bản B mô tả cực kỳ sống động các đốt bản lề cơ khí nối tiếp nhau bung ra và khóa chặt ở vị trí mở hoàn toàn.",
            fn=machine_cues.machine_wings_unfold,
        ),
        CueSpec(
            cue_id="machine_wings_fold",
            group_id="machines",
            group_title="4. Machines",
            category="oneshot_machine",
            loop=False,
            target_lufs=-20.0,
            volume=0.72,
            desc_a_vi="Xi-lanh thủy lực thu cánh đi xuống A3–D3, xả hơi giảm chấn và sập chốt nghỉ bọc nỉ.",
            desc_b_vi="Các thanh giằng pantograph xếp gọn từng nấc và tựa vào đệm cao su trầm ấm ở cuối hành trình.",
            pick="B",
            pick_reason="Bản B đồng bộ ngôn ngữ cơ khí pantograph với bản mở cánh, tạo cặp âm thanh đóng-mở cánh pin rất thuyết phục.",
            fn=machine_cues.machine_wings_fold,
        ),
        CueSpec(
            cue_id="machine_pad_contacts",
            group_id="machines",
            group_title="4. Machines",
            category="oneshot_machine",
            loop=False,
            target_lufs=-19.5,
            volume=0.74,
            desc_a_vi="Cơ cấu nâng đưa má đồng tiếp điểm sập khớp kèm tiếng xì khí nén ngắn và cuộn cảm nạp điện ấm D3–A3.",
            desc_b_vi="Ngàm kẹp lò xo đôi ôm lấy thanh cái đồng, vang tiếng ngân kim loại ấm F#3–D4–A4.",
            pick="A",
            pick_reason="Bản A thể hiện rõ cả ba lớp clunk-hiss-hum: tiếng má đồng chạm cơ học, hơi khí nén và dòng điện ấm bắt đầu chạy qua.",
            fn=machine_cues.machine_pad_contacts,
        ),
        CueSpec(
            cue_id="machine_transformer_loop",
            group_id="machines",
            group_title="4. Machines",
            category="loop_3d",
            loop=True,
            target_lufs=-22.0,
            volume=0.66,
            desc_a_vi="Vòng lặp 4 giây: tiếng lõi thép biến áp ngâm dầu ngân trầm ấm ở D2–A2–D3–F#3 không một gợn nối.",
            desc_b_vi="Vòng lặp 4 giây: máy변 áp kèm quạt tản nhiệt lá đồng quay êm trên nền cộng hưởng D2–D3–A3.",
            pick="A",
            pick_reason="Bản A là tiếng ngân biến áp ấm chuẩn mực của C2 Radio bench: dày, êm, đứng cạnh lâu vẫn dễ chịu.",
            fn=machine_cues.machine_transformer_loop,
        ),
        CueSpec(
            cue_id="machine_pylon_switch",
            group_id="machines",
            group_title="4. Machines",
            category="oneshot_machine",
            loop=False,
            target_lufs=-19.5,
            volume=0.74,
            desc_a_vi="Cầu dao lưỡi đồng lớn gạt dứt khoát vào ngàm sứ, công-tắc-tơ đóng mạch và dây dẫn ngân nhẹ D3–A3–D4.",
            desc_b_vi="Cần gạt dao cách ly lò xo lớn bật sang ngàm đồng với tiếng ngân sứ cách điện và thanh cái.",
            pick="A",
            pick_reason="Bản A có tiếng gạt cầu dao lưỡi đồng và tiếng đóng công-tắc-tơ rất đầm, vừa nghe đã biết mạng điện vừa thông mạch.",
            fn=machine_cues.machine_pylon_switch,
        ),
        CueSpec(
            cue_id="machine_cable_drums",
            group_id="machines",
            group_title="4. Machines",
            category="oneshot_machine",
            loop=False,
            target_lufs=-20.5,
            volume=0.70,
            desc_a_vi="Tang trống gỗ-thép quay nhả cáp qua các khấc bánh cóc mềm, kết thúc bằng tiếng chốt hãm êm.",
            desc_b_vi="Tời cáp đồng nặng quay đầm kèm tiếng dây cáp bọc vải trượt qua ròng rọc.",
            pick="A",
            pick_reason="Bản A diễn tả rõ từng vòng quay của tang trống cáp và tiếng cá hãm cuối hành trình rất gọn gàng.",
            fn=machine_cues.machine_cable_drums,
        ),
        CueSpec(
            cue_id="machine_lander_ramp",
            group_id="machines",
            group_title="4. Machines",
            category="oneshot_machine",
            loop=False,
            target_lufs=-18.5,
            volume=0.78,
            desc_a_vi="Chốt khóa lớn nhả, xi-lanh thủy lực hạ cầu dẫn tàu đổ bộ và tiếng chạm đất rỗng đầm chắc ở D2–A2–D3.",
            desc_b_vi="Tời xích cơ khí thả cầu dẫn từng mắt xích nặng, kết thúc bằng cú đáp nền bụi Mặt Trăng trầm ấm.",
            pick="A",
            pick_reason="Bản A tạo được quy mô lớn nhất trong nhóm máy móc nhờ tiếng thủy lực dày và cú chạm đất rỗng trầm ấm, uy nghi.",
            fn=machine_cues.machine_lander_ramp,
        ),
        CueSpec(
            cue_id="machine_cargo_hatch",
            group_id="machines",
            group_title="4. Machines",
            category="oneshot_machine",
            loop=False,
            target_lufs=-19.5,
            volume=0.74,
            desc_a_vi="Tay quay mở chốt khóa khoang hàng, gioăng cao su xì hơi nhẹ và nắp thép mở ra ở D3–A3.",
            desc_b_vi="Vô-lăng khóa hầm tàu xoay từng nấc, lò xo đối trọng nâng nắp khoang kèm tiếng ngân thân vỏ.",
            pick="B",
            pick_reason="Bản B có tiếng xoay vô-lăng khóa và lò xo đối trọng nâng nắp hầm rất giàu chi tiết cơ khí.",
            fn=machine_cues.machine_cargo_hatch,
        ),
        CueSpec(
            cue_id="machine_crate_lid",
            group_id="machines",
            group_title="4. Machines",
            category="oneshot_machine",
            loop=False,
            target_lufs=-20.5,
            volume=0.70,
            desc_a_vi="Hai khóa gạt hòm đồ bật mở kèm tiếng nắp gỗ lót nỉ trượt và nhấc lên ở D4–A4.",
            desc_b_vi="Móc cài đồng bật nảy khỏi hòm gỗ thông, nắp gỗ dội nhẹ âm thùng đàn ấm D4–F#4–A4.",
            pick="A",
            pick_reason="Bản A tái hiện đúng chiếc hòm thiết bị vô tuyến cổ điển với hai khóa gạt bên hông và lớp lót nỉ bên trong.",
            fn=machine_cues.machine_crate_lid,
        ),

        # ===================================================================
        # 5. THE CRT UI (8 cues — strictly the quietest family in the set)
        # ===================================================================
        CueSpec(
            cue_id="ui_panel_open",
            group_id="ui",
            group_title="5. The CRT UI",
            category="ui",
            loop=False,
            target_lufs=-28.5,
            volume=0.48,
            desc_a_vi="Chốt bakelite mở nhẹ, tiếng thẻ giấy trượt êm và hai nốt đèn ấm A4–D5 cực nhỏ qua loa bàn đài.",
            desc_b_vi="Nắp đồng thiết bị mở bản lề nhỏ cùng tiếng phím nỉ F#4–B4 dịu dàng.",
            pick="A",
            pick_reason="Bản A rất êm và gọn, mở bảng giao diện như mở một ngăn phiếu trên bàn vô tuyến mà không lấn át âm thanh thế giới.",
            fn=ui_cues.ui_panel_open,
        ),
        CueSpec(
            cue_id="ui_panel_close",
            group_id="ui",
            group_title="5. The CRT UI",
            category="ui",
            loop=False,
            target_lufs=-28.5,
            volume=0.48,
            desc_a_vi="Thẻ giấy trượt vào ngăn, chốt nỉ khép êm cùng hai nốt đèn đi xuống D5–A4 dịu nhẹ.",
            desc_b_vi="Ngăn kéo nhỏ chạm đệm da cùng âm gỗ kalimba B4–F#4 lắng gọn.",
            pick="A",
            pick_reason="Bản A tạo cặp đối xứng hoàn hảo với ui_panel_open bản A, đóng lại êm như đặt tay lên chốt nỉ.",
            fn=ui_cues.ui_panel_close,
        ),
        CueSpec(
            cue_id="ui_cursor_move",
            group_id="ui",
            group_title="5. The CRT UI",
            category="ui",
            loop=False,
            target_lufs=-30.5,
            volume=0.42,
            desc_a_vi="Một khấc chuyển mạch xoay wafer thật khẽ kèm cộng hưởng màng loa giấy ấm ở nốt A4.",
            desc_b_vi="Tiếng lá thép con cóc siêu nhỏ chạm mũ phím gỗ hồng sắc ở nốt D5.",
            pick="A",
            pick_reason="Bản A là âm thanh nhỏ nhất trong toàn bộ bộ mẫu: khấc xoay wafer ấm và ngắn, lướt qua hàng chục mục vẫn êm tai.",
            fn=ui_cues.ui_cursor_move,
        ),
        CueSpec(
            cue_id="ui_confirm",
            group_id="ui",
            group_title="5. The CRT UI",
            category="ui",
            loop=False,
            target_lufs=-28.5,
            volume=0.48,
            desc_a_vi="Nút nhấn bakelite ăn khớp cùng hai nốt chuông đèn ấm D4–A4 xác nhận dịu dàng.",
            desc_b_vi="Chốt điện từ nhỏ gõ nhẹ hai thanh chuông đồng bọc nỉ F#4–D5 trong trẻo.",
            pick="A",
            pick_reason="Bản A cho cảm giác bấm nút vật lý thật tay kèm độ ấm đèn D4–A4 rất thân thiện, không hề giống tiếng bíp điện tử.",
            fn=ui_cues.ui_confirm,
        ),
        CueSpec(
            cue_id="ui_cancel",
            group_id="ui",
            group_title="5. The CRT UI",
            category="ui",
            loop=False,
            target_lufs=-29.0,
            volume=0.46,
            desc_a_vi="Nút nhấn nhả lò xo về vị trí nghỉ kèm hai nốt trầm ấm A4–D4 qua màng loa giấy.",
            desc_b_vi="Con cóc cơ khí nhả ngàm cùng hai nốt gỗ kalimba bọc nỉ F#4–D4 đi xuống.",
            pick="A",
            pick_reason="Bản A nghe như nhả một nút khóa trên mặt máy vô tuyến: nhẹ nhàng thu hồi lệnh mà không mang sắc thái trách phạt.",
            fn=ui_cues.ui_cancel,
        ),
        CueSpec(
            cue_id="ui_toast",
            group_id="ui",
            group_title="5. The CRT UI",
            category="ui",
            loop=False,
            target_lufs=-28.5,
            volume=0.48,
            desc_a_vi="Hơi ống chuyển thư khí nén phớt nhẹ, rơ-le khẽ chạm và tiếng chuông đèn B4–F#5 báo tin êm ái.",
            desc_b_vi="Thẻ giấy trượt ra kèm một tiếng gõ chuông đồng bàn điện báo bọc nỉ ở nốt A4–D5.",
            pick="B",
            pick_reason="Bản B gợi hình ảnh một phiếu tin nhắn vừa trượt ra kèm tiếng chuông đồng bọc nỉ rất đặc trưng và tinh tế.",
            fn=ui_cues.ui_toast,
        ),

        # ===================================================================
        # 6. AMBIENCE BEDS (3 cues, 60 s loopable)
        # ===================================================================
        CueSpec(
            cue_id="ambience_earthlit_night",
            group_id="ambience",
            group_title="6. Ambience Beds (60 s Loopable)",
            category="ambience",
            loop=True,
            target_lufs=-24.0,
            volume=0.60,
            desc_a_vi="Nền 60s lặp mượt: tiếng tim đèn bàn đài D2–A2 êm dịu, hơi băng từ thở chậm và những nốt chuông đèn thưa thớt dưới ánh Trái Đất.",
            desc_b_vi="Nền 60s lặp mượt: đàn harmonium trầm D2–A2–F#3 hòa cùng âm kính kéo cung và tiếng vỏ trạm nguội dần trong đêm.",
            pick="A",
            pick_reason="Bản A định hình trọn vẹn bầu không khí đêm C2 Radio bench: cô đơn nhưng ấm áp, yên bình bên chiếc bàn vô tuyến đang thức.",
            fn=ambience_cues.ambience_earthlit_night,
        ),
        CueSpec(
            cue_id="ambience_sunlit_day",
            group_id="ambience",
            group_title="6. Ambience Beds (60 s Loopable)",
            category="ambience",
            loop=True,
            target_lufs=-23.5,
            volume=0.62,
            desc_a_vi="Nền 60s lặp mượt: thanh cái năng lượng mặt trời ngân ấm D2–A2–F#3 cùng những hợp âm đàn đèn thong thả và nhịp rơ-le hướng dương.",
            desc_b_vi="Nền 60s lặp mượt: tiếng bánh đà năng lượng rì rầm êm ái điểm những nét gảy kalimba & phím nỉ ngũ cung Rê trưởng dưới nắng.",
            pick="A",
            pick_reason="Bản A mang sắc nắng vàng ấm áp qua các hợp âm đèn thong thả và nhịp rơ-le pin mặt trời, tạo nền làm việc thư thái suốt ban ngày.",
            fn=ambience_cues.ambience_sunlit_day,
        ),
        CueSpec(
            cue_id="ambience_eclipse",
            group_id="ambience",
            group_title="6. Ambience Beds (60 s Loopable)",
            category="ambience",
            loop=True,
            target_lufs=-24.5,
            volume=0.58,
            desc_a_vi="Nền 60s lặp mượt: bóng tối nhật thực bao trùm với âm trầm D2–A2 sâu lắng, tiếng sôi băng lọc thấp và hài âm bóng thủy tinh cao trong trẻo.",
            desc_b_vi="Nền 60s lặp mượt: chuông đồng mềm & thanh kính ngân dài trong vùng bóng tối bán nguyệt tĩnh lặng.",
            pick="B",
            pick_reason="Bản B dùng âm chuông đồng mềm và thanh kính tạo chiều sâu không gian kỳ vĩ, khác biệt rõ rệt khi bóng Trái Đất che khuất Mặt Trời.",
            fn=ambience_cues.ambience_eclipse,
        ),
    ]


CATALOG: list[CueSpec] = _build_catalog()
CATALOG_BY_ID: dict[str, CueSpec] = {spec.cue_id: spec for spec in CATALOG}
