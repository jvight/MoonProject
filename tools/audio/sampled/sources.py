"""The recorded sources: each a slice of one file of the Sonniss GDC 2026 library (path inside the library, start and
length in seconds) and, for the tonal ones, the fundamental measured on that slice (Hz) so a recipe can varispeed it
onto a D major pentatonic note."""
from dataclasses import dataclass


@dataclass(frozen=True)
class Source:
    path: str
    start: float
    length: float
    f0: float = 0.0


_QUAD = "Sonik Sound Library - Toy Quadcopter/AERORadio_Quadcopter, Toy, Flying Sustained Close to Mic_KSL_KS013.wav"
_SQUEAL = ("Epic Stock Media - HD Game Materials/ICEFric_Dry Ice High Metal Squeal Groan Bright Squeak Dissonant Short"
           " 13_ESM_HDGM.wav")
_CRANE = "Victor Ermakov - Industrial Ambiences - Ship Repair Factory/MACHInd_Crane Onboard Ride Squeaks Motors_CW.wav"
_FRIDGE = ("Jake Fielding - Fridge Hums Vol.2/MACHAppl_Deep Fridge Hum, Buzz, Electric Tone, Deep_Jake Fielding_Fridge"
           " Hums Pt2.wav")
_FRIDGE_BUZZ = ("Jake Fielding - Fridge Hums Vol.2/MACHAppl_Rhythmic Electric Fridge Hum, Buzz, Static,_Jake Fielding"
                "_Fridge Hums Pt2.wav")
_LOCK = "Epic Stock Media - HD Lock And Mechanism Sound Design Kit/"
_RADIO = "Sonic Bat - Vintage Radio/"
_ARC = "InMotionAudio - Arc/"
_METAL = "The Noisery - Moaning Metal/"

SOURCES = {
    # Motors and whines (tonal): the voice's carriers.
    "motor_whine": Source(_QUAD, 9.0, 10.0, 558.5),
    "metal_squeal": Source(_SQUEAL, 0.6, 2.4, 958.0),
    "crane_motor": Source(_CRANE, 0.0, 30.0, 791.0),
    "servo_thump": Source("Epic Stock Media - Tower Defense Game/ROBTMvmt_Tower Deploy Hitech Robot Motor Dark Thump"
                          " Servo Whine 04_ESM_TDG.wav", 0.05, 0.95),
    "fridge_hum": Source(_FRIDGE, 10.0, 30.0, 100.3),
    "fridge_buzz": Source(_FRIDGE_BUZZ, 1.5, 8.0),
    "bowed_metal": Source(_METAL + "DSGNTonl_Designed Metal Bowed Screech Tonal Reverb 7_The Noisery_Moaning Metal.wav",
                          0.6, 8.0, 113.6),
    # Clicks, relays and latches.
    "relay_click": Source("InMotionAudio - USA Hotel/MECHClik_USALightSwitch_On05_InMotionAudio_USAHotel.wav", 0.0, 0.2),
    "latch_thunk": Source(_LOCK + "MECHLtch_Click Deep Mechanism Latch Button Nearfield Thunk 02_ESM_HDLM.wav", 0.0,
                          0.25),
    "count_tick": Source(_LOCK + "MACHMech_Mechanism Counting Machine Interact Loose Container Short 01_ESM_HDLM.wav",
                         0.04, 0.22),
    "tape_spring": Source(_LOCK + "MECHMisc_Tool Tape Measure Pull Retract Spring Slide Spin Long 07_ESM_HDLM.wav", 0.0,
                          3.26),
    "spring_clatter": Source(_LOCK + "METLTonl_Item Spring Wire Impact Flick Top Clatter Light Tap Roll Handling Short"
                             " 01_ESM_HDLM.wav", 0.95, 1.7),
    "lamp_switch": Source("344 Audio - Car Foley Vol. 1/VEHInt_LIGHTING CONTROL, RELEASE, HEADLIGHT FLASH_344 Audio_Car"
                          " Foley Vol 1.wav", 0.03, 0.45),
    "fan_dial": Source("344 Audio - Car Foley Vol. 1/VEHInt_CLIMATE CONTROL SYSTEM, FAN SPEED DIAL, FAST_344 Audio_Car"
                       " Foley Vol 1.wav", 0.08, 1.6),
    "vintage_click": Source("Epic Stock Media - Board Game - Sound Set Kit for Tabletop and Digital Games/UIClick_UI"
                            " Button Analog Vintage Double Click Neutral Dry Press 11_ESM_BG.wav", 0.0, 0.17),
    "radio_power": Source(_RADIO + "SBvr_Power Button 003.wav", 0.36, 0.42),
    "radio_wheel": Source(_RADIO + "SBvr_Mode Select Wheel 006.wav", 0.0, 1.25),
    "radio_sweep": Source(_RADIO + "SBvr_AM EQ Sweeping 002.wav", 20.0, 30.0),
    "rotary_dial": Source("344 Audio - Antique Telephone/COMTelph_Antique Telephone Rotary Dial Number  9_344 Audio"
                          "_Antiques - Telephone.wav", 0.0, 2.45),
    "winder": Source("Sonic Bat - Music Boxes/SBmb_Music Box C Wind Up 006.wav", 0.03, 1.5),
    "carabiner_screw": Source("344 Audio - Climbing Gear Foley Vol. 1/EQUISprt_Climbing Gear, Carabiner, Screwing Lock"
                              " 01_344 Audio_Climbing Gear Foley Vol 1.wav", 0.6, 1.9),
    "typewriter_carriage": Source("344 Audio - Antique Typewriter/COMType_Typewriter Carriage Movement, Typewriter_344"
                                  " Audio_Antiques - Typewriter_01.wav", 1.6, 1.6),
    "case_latch": Source("InMotionAudio - Instrument Case/OBJLug_Case_Closed06_InMotionAudio_InstrumentCase.wav", 0.0,
                         0.6),
    "case_down": Source("InMotionAudio - Instrument Case/OBJLug_CaseDown_Concrete12_InMotionAudio_InstrumentCase.wav",
                        0.0, 1.0),
    "pedal_clunk": Source("SoundBits - Cars - Mad Mustang Mercury/VEHMech_Car Foey Gas and Brakes Pedals 08 B_SNDBTS"
                          "_CRS-MMM.wav", 0.13, 0.55),
    "door_clunk": Source("InMotionAudio - USA Hotel/DOORMetl_StairWellDoor01_InMotionAudio_USAHotel.wav", 0.1, 0.75),
    "metal_tap": Source("Epic Stock Media - HD Game Materials/METLImpt_Metal Old File Impact Tap Against Tire Iron"
                        " Metallic Hit 01_ESM_HDGM.wav", 0.07, 0.4),
    "metal_thud": Source(_METAL + "DSGNImpt_Metal Hit Thud Thump Low Ring Geofon 1_The Noisery_Moaning Metal.wav", 0.0,
                         3.0),
    # Electric.
    "arc_spark": Source(_ARC + "ELECArc_ArcDesign15_InMotionAudio_Arc.wav", 0.0, 1.04),
    "arc_powerup": Source(_ARC + "ELECArc_ArcPowerUpDesign04_InMotionAudio_Arc.wav", 0.0, 7.4),
    "arc_buzz": Source(_ARC + "ELECBuzz_Buzz27_InMotionAudio_Arc.wav", 0.0, 9.8),
    # Grit and dust.
    "dust_wipe": Source("InMotionAudio - Scratch Card/OBJMisc_ScratchCard_SurfaceWipe_04_InMotionAudio_ScratchCard.wav",
                        0.3, 0.75),
    "grit_roll": Source("Epic Stock Media - HD Game Materials/WOODFric_Wood Shaker Microphone Head Roll Table Alternate"
                        " Grainy 05_ESM_HDGM.wav", 0.2, 3.9),
    "metal_rattle": Source("344 Audio - Elemental Palette Designed Vol. 1/METLMisc_Metal, Slow Whoosh, Rattle, Pass By"
                           " x4 01_344 Audio_Elemental Palette Designed Vol 1.wav", 0.0, 1.21),
    "scrape_grit": Source("InMotionAudio - Sinister Textures 5/GOREMisc_Cladding_Scratch06_InMotionAudio"
                          "_SinisterTextures5.wav", 0.03, 1.5),
    "crackle": Source("InMotionAudio - Velcro/OBJTape_VelcroSqueeze01_InMotionAudio_Velcro.wav", 0.45, 1.5),
    "ice_crack": Source("Alexander Kopeikin - 100 kHz Designed Ice/ice, crack, ice block snapping-001.wav", 0.5, 1.7),
}
