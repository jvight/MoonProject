namespace MoonProject.Audio
{
    /// <summary>
    /// Cue ids of tools/audio/sfx_manifest.json, the single source of truth (a test checks every id exists there).
    /// </summary>
    public static class AudioCueIds
    {
        public const string RoverHum = "rover_hum";
        public const string DustCrunch = "dust_crunch";
        public const string SuspensionCreak = "suspension_creak";
        public const string LandingThump = "landing_thump";
        public const string RadioStatic = "radio_static";
        public const string RadioCarrier = "radio_carrier";
        public const string CutSizzle = "cut_sizzle";
        public const string CutCrack = "cut_crack";
        public const string StrapTuk = "strap_tuk";
        public const string DockWallsRise = "dock_walls_rise";
        public const string DockClamp = "dock_clamp";
        public const string DockWallsSettle = "dock_walls_settle";
        public const string DockWallsDrop = "dock_walls_drop";
        public const string BoostWhine = "boost_whine";
        public const string BoostRelease = "boost_release";
        public const string PageTick = "page_tick";
        public const string SaveTick = "save_tick";
        public const string CountTick = "count_tick";
        public const string CutIgniteTier = "cut_ignite_tier";
        public const string CutUndertone = "cut_undertone";
        public const string SegmentSlide = "segment_slide";
        public const string CargoTok = "cargo_tok";
        public const string HoldBoot = "hold_boot";
        public const string HoldSlot = "hold_slot";
        public const string ItemSlide = "item_slide";
        public const string SealCut = "seal_cut";
        public const string ContainerOpen = "container_open";
        public const string ChargeBrace = "charge_brace";
        public const string ChargeWhine = "charge_whine";
        public const string ChargeHum = "charge_hum";
        public const string ChargeDetent = "charge_detent";
        public const string ChargeLock = "charge_lock";
        public const string ScanSweep = "scan_sweep";
        public const string ScanStep = "scan_step";
        public const string ScanLiftoff = "scan_liftoff";
        public const string SheetComplete = "sheet_complete";
        public const string PartPick = "part_pick";
        public const string PartDrop = "part_drop";
        public const string PartRefuse = "part_refuse";
        public const string UiRelayClick = "ui_relay_click";
        public const string UiTubeThunk = "ui_tube_thunk";
        public const string UiTapeTick = "ui_tape_tick";
        public const string PickupLift = "pickup_lift";
        public const string PickupClunk = "pickup_clunk";
        public const string RadioTune = "radio_tune";
        public const string RecoveryLift = "recovery_lift";
        public const string RecoverySettle = "recovery_settle";
        public const string UiPanelOpen = "ui_panel_open";
        public const string UiPanelClose = "ui_panel_close";
        public const string UiCursorMove = "ui_cursor_move";
        public const string UiConfirm = "ui_confirm";
        public const string UiCancel = "ui_cancel";
        public const string UiToast = "ui_toast";
        public const string UiHoldFill = "ui_hold_fill";
        public const string UiHoldComplete = "ui_hold_complete";
        public const string RoomTone = "room_tone";
        public const string RoverLampHum = "rover_lamp_hum";
        public const string RoverServo = "rover_servo";
        public const string RoverMetalTick = "rover_metal_tick";
        public const string SalvageChime = "salvage_chime";
        public const string SalvageCutMetal = "salvage_cut_metal";
        public const string SalvageCutWiring = "salvage_cut_wiring";
        public const string SalvageCutOptics = "salvage_cut_optics";
        public const string SalvageCutTone = "salvage_cut_tone";
        public const string SalvageBreak = "salvage_break";
        public const string StargazeSwell = "stargaze_swell";
        public const string AsleepStatic = "asleep_static";
        public const string GhostChord = "ghost_chord";
        public const string WakePowerThump = "wake_power_thump";
        public const string LidTick = "lid_tick";
        public const string NeckServo = "neck_servo";
        public const string DockChargeHum = "dock_charge_hum";
        public const string DockFull = "dock_full";
        public const string BatteryCell = "battery_cell";
        public const string BatteryLow = "battery_low";
        public const string BatterySigh = "battery_sigh";
        public const string LampTick = "lamp_tick";
        public const string RoverLightsSwitch = "rover_lights_switch";

        // R1 Group 1: 07's face
        public const string FaceBoot = "face_boot";
        public const string FaceBlink = "face_blink";
        public const string FaceHappy = "face_happy";
        public const string FaceWorried = "face_worried";
        public const string FaceAsleep = "face_asleep";
        public const string FaceStartled = "face_startled";
        public const string FaceProud = "face_proud";

        // R1 Group 2: Sky
        public const string SkyDiamondRing = "sky_diamond_ring";
        public const string SkyDawnSweep = "sky_dawn_sweep";
        public const string SkyNightfallSettle = "sky_nightfall_settle";

        // Building: ghost, invalid, stage complete and deconstruct are R1; place, snap, weld and complete are sampled
        public const string BuildGhost = "build_ghost";
        public const string BuildSnap = "build_snap";
        public const string BuildInvalid = "build_invalid";
        public const string BuildPlace = "build_place";
        public const string BuildWeldLoop = "build_weld_loop";
        public const string BuildStageComplete = "build_stage_complete";
        public const string BuildStructureComplete = "build_structure_complete";
        public const string BuildDeconstruct = "build_deconstruct";

        // Opening v2: the base powering on, its lamps at the first nightfall
        public const string StructurePowerOn = "structure_power_on";
        public const string BaseLampOn = "base_lamp_on";

        // The New Game self-test (sampled): the checks' motions, and the fault result
        public const string BootLens = "boot_lens";
        public const string BootLampFlicker = "boot_lamp_flicker";
        public const string BootLampRamp = "boot_lamp_ramp";
        public const string BootHeadSweep = "boot_head_sweep";
        public const string BootHeadNod = "boot_head_nod";
        public const string BootWheel = "boot_wheel";
        public const string BootMotorShudder = "boot_motor_shudder";
        public const string BootMotorCreep = "boot_motor_creep";
        public const string BootCutterSpark = "boot_cutter_spark";
        public const string BootWingJerk = "boot_wing_jerk";
        public const string BootWingStall = "boot_wing_stall";
        public const string BootWingFold = "boot_wing_fold";
        public const string BootAntennaRise = "boot_antenna_rise";
        public const string BootAntennaSignal = "boot_antenna_signal";
        public const string BootBattery = "boot_battery";
        public const string BootAssembly = "boot_assembly";
        public const string BootBeginTurn = "boot_begin_turn";
        public const string BootBeginShiver = "boot_begin_shiver";
        public const string BootBeginLamps = "boot_begin_lamps";
        public const string BootFault = "boot_fault";

        // R1 Group 4: Machines
        public const string MachineSolarTrack = "machine_solar_track";
        public const string MachineWingsUnfold = "machine_wings_unfold";
        public const string MachineWingsFold = "machine_wings_fold";
        public const string MachinePadContacts = "machine_pad_contacts";
        public const string MachineTransformerLoop = "machine_transformer_loop";
        public const string MachinePylonSwitch = "machine_pylon_switch";
        public const string MachineCableDrums = "machine_cable_drums";
        public const string MachineLanderRamp = "machine_lander_ramp";
        public const string MachineCargoHatch = "machine_cargo_hatch";
        public const string MachineCrateLid = "machine_crate_lid";

        // R1 Group 6: Ambience beds
        public const string AmbienceEarthlitNight = "ambience_earthlit_night";
        public const string AmbienceSunlitDay = "ambience_sunlit_day";
        public const string AmbienceEclipse = "ambience_eclipse";
    }
}
