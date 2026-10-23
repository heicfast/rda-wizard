# UI-MAP — EaseUS Data Recovery Wizard (DRW) Page / State / Flow Inventory

> **Status**: AUTHORITATIVE reconstruction input for the rda-wizard clean-room WPF rewrite.
> Derived exclusively from the `easeus-drw-research` reverse-engineering corpus.
> Any item the corpus does not settle is marked `[CORPUS-AMBIGUOUS]` — never guessed.
> Proprietary strings are quoted only as short identification fragments; the rewrite
> writes original copy.

---

## 1. Overview & Method

**Sources (priority order used):**

1. `re-workspace/artifacts/UIUX/` — all 40 documents: static decodes of DRWUI.exe
   (RTTI class map, moc dispatch tables, QSS census, string/QM inventory, telemetry
   census) plus dynamic ground-truth runs (splash, landing, scan-phase, scan-complete,
   interaction-driver, screenshot atlas).
2. `architecture/05-uiux-architecture.md`, `architecture/07-user-flow-uiux.md`,
   `architecture/04-recovery-workflow.md`.
3. `re-workspace/artifacts/DRWUI-exe--main-ui-architecture.md`,
   `DRWUI-exe--thread-architecture.md`, `DRWUI-subsystems--assistant-grading-preview.md`.
4. `re-workspace/artifacts/MOM-modules/{Device,DeviceManager,Partition,Mom,Common,Error}.md`.
5. `re-workspace/artifacts/SaveScan/rsf-format--scan-session.md` (fully validated parser).
6. `og-codebase/` installed layout (file names only: DRWUI.exe, PartitionRecovery/*.mo,
   SaveScan/*.rsf, diskhealth/, FileFilter.xml, language/*.qm, style.css, raw_signs.json…).

**Confidence levels used throughout:**

- `[DYN]` — pixel/runtime-verified on a real Windows or Wine loader run (highest).
- `[STATIC]` — decoded from the binary (vtable/moc/string evidence), cross-validated.
- `[INFERRED]` — synthesized from ≥2 static sources but not directly decoded.
- `[CORPUS-AMBIGUOUS]` — corpus does not settle the question.

**How to read this map:** Section 2 is the census of every page/dialog family. Section 3
gives per-page detail (regions, controls, states, transitions, strings, gates). Section 4
is the full state machine. Sections 5–12 go deep per subsystem. Section 13 lists what the
rewrite must consciously omit or replace; Section 14 maps every page back to its evidence.

**Shell facts that frame every page** `[STATIC+DYN]`:

- Main window: **1240×840 normal, 800×600 minimum, opens maximized** (`config.ini [windowsSize]`, `window_show_state=StatusMaxed`), high-DPI aware; frameless with custom title bar (~35 px, icon strip click centers at y≈20).
- The wizard is a **`QStackedWidget` (`DrwMainViewManagerWizard`)** with exactly 4 top-level pages — navigation is `setCurrentIndex`, not window swapping. Scan **runs inside the results page** (`CDetailPage`).
- Chrome: custom title bar (`BasicTitleBar`), left sidebar (dynamic: "Data Recovery / Advanced Features / File Backup" sections), nav/flyout rail, in-house message box standard **540×240**.
- 22 shipped languages; UI text English-hardcoded in `QObject::tr` with `.qm` overrides.
- Product identity: "EaseUS Data Recovery Wizard **Free**" (edition badge in title, `version.ini version=free` on the captured build).

---

## 2. Page Inventory Table

IDs group a page and its closely-coupled child surfaces. "Dialog" = modal OS window.

| ID | Name (class family) | Kind | Purpose | Entry points | Exit points |
|----|---------------------|------|---------|--------------|-------------|
| P00 | Splash / `CStartWindow` (`SplashWindow` RTTI) | Window | Boot brand frame; "Reading the disk information, please wait" + progress bar; covers hidden main frame during device enumeration | App launch (DRW.exe loader → DRWUI) | Enumeration complete → main window `[DYN]`; under Wine stays pinned (fake disks) |
| P01 | Main shell / `DrwMainWindow` + `BasicTitleBar` | Window | Persistent chrome: title bar (robot/buy/activate/save-session/hamburger/min/max/close), sidebar, content host | After splash | App exit (via `MainWndExitThread`, interrupted by retention gates) |
| P02 | Landing / `PeripheralHomeView` (stack idx 2, **default**) | Page | Home: sidebar sections, device cards ("Internal drives (2)", "Specify a location" trio), "Search for Lost Data" CTA, Assistant panel | Default after splash; sidebar "Hardware Disk" | Scan click → P06; sidebar pages P02a–P02f; AI panel P36 |
| P02a | SD Card page | Sidebar page | "Connect Your Memory Card…" coaching + 3 feature tiles + links | Sidebar "SD Card" | Back to landing; device attach → device list |
| P02b | More Recovery Tools page | Sidebar page | 6 tool cards (Crashed PC Rescue, NAS and Linux, RAID Recovery, Cloud Recovery, Image Recovery, Disk Health) | Sidebar "More Recovery Tools" | Routes to scenario windows P08–P14, P31 |
| P02c | File Repair page | Sidebar page | 3 repair options (video/photo/document) + Start | Sidebar "File Repair"; AI panel option 2 | → P28 repair home |
| P02d | Lost Partition Rescue / `DrwLostDiskPage` (stack idx 1) | Page | 3 scenario cards (Partition Missing / Cannot Be Opened / BitLocker Missing) + Start | Sidebar; home disk entries; "Drive Not Show Up" | → smart partition scan inside P06 |
| P02e | ChronoSnap Backup page | Sidebar page | "Time Machine for Windows" feature cards + Start | Sidebar | → disk backup flow P40 `[INFERRED]` |
| P03 | PC recovery home / `DrwHomePage` (stack idx 0) | Page | "Select a location to start recovering"; device/partition rows with TRIM chips, per-row Scan; "Continue last scanning" session banner | Home tab switch (stack idx 0) | Scan click → P06; session Load → P06 (restored tree) |
| P04 | Partition start / `PartitionStartWnd` | Window | Standard partition-recovery entry (pre-scan confirm) | Home tile/row | → P06 scan |
| P05 | BitLocker chain / `CBitlockerStartWnd` → `BitlockerUnlockDialog` → `BitlockerUnlockRetDlg`/`RetWithPwdDlg` (+ `BitLockerMSGuideDlg`) | Dialogs | Unlock BitLocker volume (password/recovery key; MS `manage-bde` guide) | Locked-volume tile; "Bitlocker Drive" tile; P02d card 3 | Unlock OK → scan on unlocked volume; Cancel → home |
| P06 | Scan+Results workhorse / `CDetailPage` (stack idx 3) | Page | Scan progress, live 3-branch tree, list, filter, search, preview panel, bottom status bar + Recover CTA | Scan click from any home; session load; deep-scan continuation | Home (back); Recover → P23; preview → P22; exit attempt → retention gate |
| P07 | Quick-scan complete / `CQuickScanCompleteWnd` | Dialog | "Quick Scan Completed"; "Recover Found Deleted %1 files %2" CTA; Advanced-scan coaching (runs in background) | Quick pass finishes inside P06 | Recover now (stays P06); Advanced continues (stays P06) |
| P08 | Scan common pop / `CScanCommonPopDlg` (multimode) | Dialog | Stop confirm ("Stop searching for lost data?"), "Return to Home?", "Organizing found files…", completion stats grid | Stop/Home buttons; phase boundaries | Stop → stopped state; Continue → resume; Go to Recover → P06 results |
| P08a | Scan-complete celebration | **Standalone 540×380 OS window** | "Congratulations! Scan Completed!", 2 stat panels (files found / size), "Go to Recover" CTA; no ESC-reject | Full scan finish `[DYN]` | CTA → P06 results; X → results |
| P09 | Smart partition scan / `CPartitionScanFrame` (+ `DrwPartitionRecoverWindow`) | Frame/window | "Smart Partition Scanning"; "Scanning disk sectors: %1"; found-partition list (Disk/Basic/Dynamic/MBR/GPT/FS); Recover/Stop/Home | Whole-disk scan (P02d) | Select found partition → scan it; Home |
| P10 | NAS chain (13 windows: `CNasStartPage`→`CNasLogonPage`→`CNasSSHPage`(+Pop/Info)→`CNasDownloadPage`→`CNasEnummingPage`→`CNasDevicePage`; error pages `CNasEnumFailedPage`/`CNasNoSystemPage`; tips `CNasHelpTipsWnd`/`CNasPortTipsWnd`/`CNasNetErrorTipsWnd`) | Window chain | NAS login (IP/port/creds) → SSH enable guidance → agent download → share enumeration → device rows (Connect/Scan/Support) → scan | "NAS & Linux" tile (P02b) | Scan → P06 (remote strategy, error overlay attached); Cancel → home |
| P11 | Linux chain / `CLinuxBasePage`→`CLinuxLogonPage`→`CLinuxDownloadPage`(+`CLinuxNoSystemPage`) | Window chain | Mirror of NAS chain (zip agent); Win8.1/8/7 unsupported notice | "Linux Recovery" tile | Scan → P06; Cancel → home |
| P12 | SD/peripheral chain / `CSDCardDeviceListPage` (+`NoDevice`,`ScanTips`,`HelpPop`,`HelpTips`,`FeedBack*`) | Window chain | Peripheral device list + scan tips + own feedback loop | "SD Card" flow | Scan → P06 |
| P13 | RAID / `CRaidDeviceListPage` (+`NoDevice`,`HelpTipsWnd`) | Window chain | RAID array list → scan | "RAID" tile | Scan → P06 |
| P14 | WinPE / `CPEStartWnd` → `CPEConnectWnd` / `CPEBuyRetainWnd` | Window chain | Bootable-media creator/connector + buy/retain offers | "Crashed PC Rescue" / "Boot Repair" tiles | Media creation; retention offers |
| P15 | Cloud / `CHomeCloudDriveView` + `CHomeCloudLoggingWnd` (+`ICloudLoginViewer`) | Window chain | Cloud sign-in (iCloud/Dropbox/GDrive/OneDrive) → drive pick → scan; privacy copy on login | "Cloud Drive" tile; destination Cloud tab | Scan → P06; Cancel |
| P16 | SBackup / `CSBackupStartWnd` + `CSBackupDownloadPage` | Window chain | SBackup product integration (agent download) | Backup tile | Scan → P06 |
| P17 | WeChat / `CWeChatDefaultWnd`→`CWeChatLoadingWnd`→`CWeChatIEWnd` | Window chain | WeChat data recovery via embedded IE | WeChat entry | Scan → P06 |
| P18 | Preview shell / `PreviewMainDialog` (+shadow chrome, `EmbeddedPreviewWindow`) | Modal window | Per-type preview host with Recover CTA; tri-state Loading→Failure/Content | Context-menu "Preview"; preview button (NOT double-click) `[DYN]` | Close; Recover → P23 |
| P18a | Live preview panel | In-page panel (P06) | Double-click row → thumbnail + metadata + "Recovery Chances" gauge + Status (Existing) + Preview button | Double-click file row `[DYN]` | Row change; "Preview" → P18 |
| P19 | Per-type preview windows: `ImagePreviewWindow`, `VideoPreviewWnd`(+`CVideoPreviewTipsWnd`), `RichTextPreviewWindow`, `ArchivePreviewWindow`; `PreviewFailedWnd` | Embedded | Image zoom/pan; 15 s video cap; rich text; browse-inside-archive; failure grading | Type dispatch inside P18 | Close |
| P20 | Repair home / `FileRepairHomePage` + `FileRepairAddPage` + list | Window (3-page stack) | "Add corrupted files to easily repair them." add/drag surface | P02c sidebar page; destination "Enable file repair" | → video/PDF wizards P21/P22r |
| P21 | Video repair wizard / `CRepairVideoHomeWnd`→Add→Items→Advanced(Analyze+GetSample)→Repairing→Save | Window chain | Video repair incl. sample-file Advanced Repair + Deep Video Reconstruct | P20; corrupted-video rows | Save (gated) → P23r; Failed/Original triage |
| P22r | PDF repair wizard / `CRepairPDFHomeWnd`→Add→Decrypt→Progress→Preview→Save | Window chain | PDF repair incl. password decrypt; preview via raster pages | P20 | Save (gated) → P23r |
| P23 | Destination picker / `ChooseDestinationPathDialog` (+`ChooseDestinationPathItem`) | Modal dialog | "Choose a destination path to save the file:"; Local/Cloud tabs; "Enable file repair" toggle (default ON); Choose-folder; Save disabled until dest | Recover with sub-free-limit selection `[DYN]` | Save → P24; Cancel |
| P24 | Recovery progress / `RecoverProgressDialog` | Dialog | Per-file + aggregate progress; "Recovering…" states | After destination Save | Complete → P26; cancel → confirm |
| P25 | Space/destination gates | Dialog | "Insufficient Disk Space" (Continue/Reselect); source-partition block; path validation errors | Save with bad destination `[DYN]` | Reselect → P23; Continue anyway |
| P26 | Recovery result / `RecoverResultDlg` | Dialog | Summary "(%1 files / %2 failed; …recovery log)" | Progress completes | Close → P06 |
| P27 | Free-limit gate / `CRecoverLimitDlg` | Dialog | "Sure to give up recovering remaining valuable files?"; counters "Recovered: %1/ %2"; Give Up vs Recover Remaining | Free quota exhausted mid-recovery | Recover Remaining (needs Pro); Give Up → P26 |
| P28 | Retention gates / `RetentionExitDlg` + `RetentionActiveDlg` | Dialogs | "Wait! You are at risk of losing data!"; rows: Get Up to 40% Off / Online Service / Quit; **periodic nag ~30–60 s on Free builds, any page** | Recover-All with huge set; exit attempt (double-ESC); timer | ESC/X → back; Quit → exit; purchase |
| P29 | Buy/compare dialogs / `CLocalRecoverBuyDlg`, `CRecoverFinishBuyDlg`, `CBuyGuideWnd`, `ChinaMainlandbuy*`, `InnerWebWindow` (embedded IE checkout) | Dialogs | Free-vs-Pro matrix; win-back; minimal "Why Pro?"; web checkout | Every gate's buy buttons | Close; purchase success → re-check gates |
| P30 | Activate / `CEnterLicenceDlg` (+`ApowerActivateDialog`, `ExternalActivateDialog`, `ActivateResultDialog`) | Dialogs | "Enter your license code…" + Paste/Activate; states Expired/Invalid/Locked/Pirate; no ESC | Title-bar buy/activate icons; menu "Activate" | X close (770,150); success → licensed |
| P31 | DiskHealth / `DiskHealth.exe` (Qt Quick) | Separate app | S.M.A.R.T. dashboard: disk selector, health card, attribute table, caution widget, tray guarding | "Disk Health" tool card (P02b) | Tray-hide on close |
| P32 | Hamburger menu / `Drw_More_Menu` (+ Language/Help/Settings/Theme/DPI submenus) | Menu | 16 items: Language, Help(5 sub), Activate, Check Updates, Share, Refer, File Repair, Todo Backup, ChatPDF, Settings(Theme/DPI), Survey, About, Add Driver, Import/Save Scan Result | Title-bar hamburger (874,20) | ESC/re-click; item dialogs |
| P33 | About family / `CAbout*Dlg` (incl. OEM `_cn_fx/_cn_jz/_cn_lc`, Apower, Cisdem) | Dialogs | Product/version/OEM branding | Menu "About" | Close |
| P34 | Update check / `CheckUpgradeDlg` | Dialog | Update availability | Menu "Check for Updates"; background check | Close; install |
| P35 | First-run tour / `IntroductionTipDialog` + 7-slide `Introduction/` assets | Dialog | Guided tour | First run | Finish |
| P36 | AI Assistant panel + solution wizard + FAB | Panel + modal | "Hi! Guess you need:" 4 options; auto-opens ~60–75 s after landing; persistent robot FAB after dismissal; 3-step modal solution wizard | Robot avatar (700,17); auto-timer | Toggle; option routing (repair page, web guide, chat) |
| P37 | AI surfaces in results: Recommend toast/DEDUP window | Toast/window | "AI Recommend available." toast + "View Details"; `DrwAIDedupWindow` dedup results | Results entry `[DYN]`; AI toggle in filter bar | Dismiss; switch views |
| P38 | Auto-Recover suite / `CAutoRecoverSettingWnd` + Entry/Open/Close | Windows | "Auto-Recover after Advanced Scan Completed"; scope (Suggested/All); destination; email notify | Bottom-bar Auto-Recover button (post-scan) | Enable/disable |
| P39 | Rating/feedback / `UserRateDlg`, `CFollowusDlg`, feedback wnds | Dialogs | NPS "How likely are you to recommend…"; follow/social | Scheduled; SD-card flow | Close |
| P40 | Disk backup / `DiskBackupDialog` + `DiskBackProgressPage` + `DiskBackupSaveDlg` | Dialog chain | "Select a drive or a partition to create a backup disk image"; progress; save | "Create Image" on home; ChronoSnap page | Save image; cancel |
| P41 | Session list / `LastScanRecordView` + "Continue last scanning" banner | Banner/list | Prior `.rsf` sessions with metadata (scan time/files/disk) + Load | Home when session exists | Load → P06 restored |
| P42 | Crash reporter / `DRWReport.exe` (MFC/XCrashReport) | External dialog | Crash details, serial number, send options; dumps + upload | DRW.exe watchdog on DRWUI crash | Restart DRWUI |

**Census: 43 primary page/dialog families** (P00–P42) + 8 closely-coupled sub-surfaces
(P02a–P02e sidebar pages, P08a celebration, P18a live preview, P22r PDF wizard) = **50
inventory rows** above, of which: 4 wizard-stack pages, ~15 scenario windows/chains, 2
in-app preview surfaces + 5 preview windows, 9 recovery-funnel dialogs, 11
system/monetization surfaces — plus the in-page states of P06 cataloged in §3.6.

---

## 3. Per-Page Detail

### 3.1 P00 — Splash (`CStartWindow`) `[DYN+STATIC]`

- **Layout**: small frameless card, rounded corners, white/light-blue gradient, drop
  shadow: first-aid-kit icon (`StartWindowIconLabel`), bold title "EaseUS Data Recovery
  Wizard" (`StartWindowTitelLabel`), gray status line "Reading the disk information,
  please wait…", isometric illustration (`bg_downloader.png`), thin blue progress bar
  along the bottom edge (~60–70 % filled while WMI + mountmgr enumeration runs).
- **States**: enumerating (progress sustained); stuck (Wine fake-disk case — splash never
  closes; the main 1600×1000 frame exists but stays hidden beneath).
- **Transitions**: enumeration done → splash closes, stack page 2 revealed. No buttons.
- **Strings**: title + status only (identification fragments above).
- **Note**: DRWUI launched standalone (without DRW.exe loader) shows no UI and exits —
  splash and everything else require the loader environment `[DYN]`. Our rewrite does not
  need a supervisor to boot, but a splash/enumeration gate is still the right pattern.

### 3.2 P01 — Main shell (`DrwMainWindow` + `BasicTitleBar`) `[STATIC+DYN]`

- **Title bar icon strip** (window-relative, click centers y≈20 on 1024-px client):
  AI robot avatar (700,17) · Buy/cart (753,20) · Activate/key (794,20) · Save scan
  session/.rsf (834,20) · Hamburger (874,20) · Min (959,20) · Max (994,20) · Close (1015,20).
  Full button family (27 objectNames) incl. Login/UserHead/Notify/OnlineChat (CN-gated)/
  Follow/Survey — conditional visibility by license/locale/NAS context.
- **Sidebar** (dynamic, pixel-verified rows): section headers "Data Recovery" (~y90),
  "Advanced Features" (~y290), "File Backup" (~y420); rows Hardware Disk (selected,
  135,120) / SD Card (135,174) / More Recovery Tools (135,226) / File Repair (135,322) /
  Lost Partition Rescue (135,374) / ChronoSnap Backup (135,467).
- **States**: first-run (tour dialog P35), no-devices ("Unable to find your hard drive?"
  link + Refresh), session-exists (banner P41), refreshing (modal progress dialog via
  view-manager slot 7), retention-nag overlaid periodically on Free builds.
- **Exit semantics**: single ESC does not exit; **double ESC = exit attempt → retention
  gate** `[DYN]`; window can spontaneously minimize (census −32000,−32000) — restore-resilient.
- **Gate**: Free edition badge; Buy Now chip top-right visible during scan.
- **Win7-era conventions**: frameless custom chrome with hand-rolled min/max/close SVG
  4-state buttons; tray-hide behavior in DiskHealth; "Don't show this again" checkboxes.

### 3.3 P02 — Landing (`PeripheralHomeView`, stack idx 2) `[DYN+STATIC]`

- **Center content**: page title "Select a location to search for lost data" (y≈190);
  "Internal drives (N)" header; device tiles (icon, name "Windows(C:)", used/total
  capacity bar "31.15 GB/149.45 GB"); selection = blue border + checkmark;
  "Specify a location" trio: Recycle Bin / Desktop / Select Folder tiles;
  "Unable to find your hard drive?" link; primary CTA **"Search for Lost Data"**
  (blue, (897,678)).
- **Static 14-tile registry** (tr-verified): "Hardware Disk · SD Card · NAS & Linux ·
  RAID · Bitlocker Drive · Cloud Drive · Linux Recovery · File Repair · Lost Partition
  Rescue · Crashed PC Rescue · Boot Repair · Help · More Recovery Tools · ChronoSnap
  Backup" — tiles route to the scenario windows P05–P17/P31.
  `[CORPUS-AMBIGUOUS]` The exact mapping between the 14-tile static registry and the
  6-row sidebar + card layout observed dynamically (the shipped build's landing) is not
  fully closed; the sidebar is the persistent navigation and the tiles/cards are the
  entries. The rewrite should treat the sidebar+cards layout as ground truth.
- **Device groups** (tr): "External drives (%1)" / "Internal drives (%1)" with counts;
  dead-end state "Drive Not Show Up".
- **States**: enumerating (splash covers), populated `[DYN]`, empty (guide frames
  `NoDeviceGuide*` + "Unable to find…" link), BitLocker volume present (lock icons +
  "unlock Bitlocker" guidance), session exists (P41 banner on P03).
- **Transitions**: CTA/device double-select → scan (P06); sidebar rows → P02a–P02f;
  disk-image "Create Image" → P40.
- **Wiring**: emits `startScanDevice(ScanDeviceInfo)` → view-manager `scanClickedSlot`.

### 3.4 P03 — PC recovery home (`DrwHomePage`, stack idx 0) `[STATIC]`

- **Header**: "Select a location to start recovering" + "Can't find location?" link +
  Refresh; sections "Devices"/"Locations"; "Hard Disk Drivers" [sic] group.
- **Rows** (`DrwHomePartListView` + `CPartitionListItem`): icon by 50-state type map
  (system/USB/lost/lock/unlock/camera/UAV/SD/dashcam/recycle-bin), letter+label, file
  system, capacity bar, **TRIM status chip "TRIM OFF"/"TRIM ON"**, per-row `BtnScan`.
- **Whole-disk group**: "Disks and Partitions" + "Scan the entire disk…" subtext →
  routes to P02d/`CDoScanLostPartition`.
- **Session resume banner**: "Continue last scanning · Scan time: · Scan files: ·
  Scan disk: · Load" → `Result_Loadsession`.
- **States**: populated / empty / refreshing (modal) / session-present.
- Metrics: content margins 40/45/0/48 px, min-height 400 px, 12-px slim scrollbars.

### 3.5 P02d — Lost-disk page (`DrwLostDiskPage`, stack idx 1) `[STATIC+DYN]`

Same list chrome as P03 but lists whole physical disks; 3 scenario cards observed
dynamically: **Partition Missing / Partition Cannot Be Opened / BitLocker Partition
Missing+Damaged** + hard-drive platter graphic + Start CTA. Scanning from here runs the
"Smart Partition Scanning" surface (P09) with `CDoScanLostPartition`.

### 3.6 P06 — Scan+Results workhorse (`CDetailPage`, stack idx 3) — the core page

**The single most important fact: scan progress and results are the SAME page**; the
tree fills progressively while engines run (browse-while-scanning) `[DYN+STATIC]`.

#### 3.6.1 Layout regions (top→bottom)

| Region | Owner | Contents |
|--------|-------|----------|
| Title bar | P01 chrome | unchanged; "Searching:" target shown in page header |
| **Toolbar / NavigateBar** (y≈55–105) | `CNavigateBar` | Home (35,80) / Back (85,80) / Up (130,80) nav cluster; address breadcrumb "Windows (C:)"; view toggle list⇄icon (667,80); Filter funnel (737,80); search field "Search files or folders" (886,80) |
| **Filter bar** (toggle-visible, y≈120–180) | `CFilterBar` (×2 instances: Path mode + Type mode, one visible) | Show / File Type / Date Modified / File Size dropdowns + **AI Recommend toggle**; Reset Filter |
| **Path/Type tab strip** (y≈117) | `CFileTypeFilterTabBar` | "Path" (140,117) / "Type" (234,117) tabs |
| **Left tree** | `CLeftTreeView`/`LostLeftTreeView` (`IsTypeTree()` polymorphism) | 3 verified roots: existing-volume tree ("Windows (C:) 1.1M"), "File Path Lost 4.3K" (warning icon), "Reconstructed" (grows live during carving); + 5 special branches (§6) |
| **Right list** | `CRightContent` (82-method hub) + `CListControl`/`CListHeaderView` | sortable columns Name/Size/Type/Date Modified (+recovery-chance); select-all checkbox; per-row checkboxes; async thumbnails; icon-view mode |
| **Right preview/details pane** | in-page | single-click → folder/file details (icon, Open, Size/Date/Path); double-click → live preview (P18a) |
| **"Searching:" ticker strip** (y≈630–648) | `ScaningRecoverMessage` | light-blue full-width strip: current file path / "Reading sector: X/Y" |
| **Bottom status bar / `CBottomArea`** | `CBottomArea` | circular % gauge (48,687); Pause ‖ (104,698); Stop ■ (127,698); status "Advanced Scanning, Files Found: N (size)"; "Remaining time: 07m43s"; green check + "Scan Completed, Files Found: N (S)" when done; **Recover All…** CTA (916,684) → contextual **Recover…** on selection (892,668); Auto-Recover toggle; moving Buy-guide window |

#### 3.6.2 States

- **Scanning-quick**: MFT pass fast (~21 s observed); status "Quick Scanning%1, Files
  Found:"; then `CQuickScanCompleteWnd` (P07) — advanced scan continues in background
  while user may already recover.
- **Scanning-advanced**: "Advanced Scanning%1, Files Found:" + gauge + sector ticker +
  remaining-time (linear sector-throughput estimate); "Reconstructed" branch grows live
  (1→16 in 6 min observed); a 48×48 `Qt5QWindowToolSaveBits` auxiliary window appears
  (scan-phase indicator).
- **Paused**: "Scan Paused" + red "Scanning Paused." strip; gauge frozen at pause %.
- **Stopping**: stop-confirm dialog (P08) with stats tiles (files found, used time
  00:02:38) + Stop (pink) / Continue (blue).
- **Organizing**: "Organizing found files…" — post-engine tree-building/classify phase
  (async workers; Freeze/Unfreeze protocol locks the UI against model mutations).
- **Complete**: celebration window (P08a) → results; bottom bar flips to "Scan
  Completed, Files Found: N (S)" + Recover All active.
- **Disconnected (mid-scan)**: see §11 error dialogs.
- **Empty result (recycle bin)**: `RecycleBinScanNoRltWnd`.
- **Freeze/Unfreeze**: coordinated UI lock (tree+header+page) during destructive model
  phases — a first-class protocol, not a spinner.
- **Session-loaded**: `.rsf` import → same page with restored tree (`sigLoadFinished`).

#### 3.6.3 View modes (internal QStackedWidget)

Path tree ⇄ Type/classify tree (async rebuild — tab highlights immediately, tree
populates seconds later `[DYN]`) ⇄ search-results view (breadcrumb becomes
"Search results for X" + removable filter chip) — one controller set re-bound per mode
(slot-27 re-binder). Splitter position persisted.

#### 3.6.4 In-page controls inventory (abridged, all `[STATIC]`)

`CSearchLineedit` (9 search types: Pictures/Videos/Documents/Audio/Archive/Emails/
Bookmarks/**Unsaved File**) · filter family (§6) · `CSelAllCheckBox` (tri-state) ·
`ArrowToggleButton` · sort headers · context menu (§6) · thumbnails thread · recover CTA.

#### 3.6.5 Transitions

| Trigger | Target |
|---------|--------|
| Quick pass ends | P07 overlay |
| Full scan ends | P08a celebration (standalone window) |
| Stop / Home | P08 confirm modes |
| Double-click row | P18a live preview (in-page) |
| Context-menu Preview | P18 modal |
| Recover (selection under free limit) | P23 destination |
| Recover All / huge selection on Free | P28 retention gate (intercepts BEFORE P23) `[DYN]` |
| Exit attempt (double-ESC) | P28 retention gate |
| Session save (title bar / menu) | `.rsf` write (P41 semantics) |
| Sidebar/Home/Back | P02/P03 |

### 3.7 P07 — Quick-scan complete dialog `[STATIC]`

"Quick Scan Completed" / "You may now" / **"Recover Found Deleted %1 files %2"** [primary
CTA] / coaching "Tips: Advanced Scan is trying to find more other lost files for you
while recove[ring…]" — i.e., advanced scan continues in the background while the user
may already recover. Result-state alternative (QM): "Good news - some files are
recoverable, you can try recovery or scan deeper."

### 3.8 P08 / P08a — Scan lifecycle dialog family `[STATIC+DYN]`

`CScanCommonPopDlg` multimode modes: stop-confirm ("Stop searching for lost data?" +
risk framing "Remaining data can be rescanned and recovered anytime later, but
continuing now save you time."), repair-coupled stop ("Stop Repairing for corrupted
video?"), navigation confirm ("Return to Home?"), organize states ("Organizing found
files…" / "Stop Organizing Found Files?"), completion ("Congratulations! Scan
Completed!" + stats grid: Files found · Files scanned · Used Time · Size · Organized ·
Completed · Video Repaired · Video to be repaired).
**P08a correction `[DYN]`**: the completion celebration renders as a **standalone
540×380 OS child window** (own hwnd, centered), NOT an in-window overlay; blocks all
toolbar clicks until dismissed; ESC is rejected; exits are the CTA, X, or click-away.
CTA "Go to Recover" at window-fraction (0.4824, 0.6569).

### 3.9 P09 — Smart partition scan `[STATIC]`

`CPartitionScanFrame` (28 tr-strings, most text-dense scan widget): title "Smart
Partition Scanning" → progress "Scanning disk sectors: %1" → completion "Smart
Partition Scan Completed" / "Scan completed for Disk %1!" → stopped "Smart Partition
Scan Stopped". Found-partition list rows: Disk / Basic / Dynamic / MBR / GPT / File
System properties + "Preview for disk %1"; controls Recover / Stop / Home. Fronts the
PartitionRecovery MOM modules (Device/DeviceManager/Partition/.mo) — see §9/§14.

### 3.10 Scenario chains P10–P17 (NAS · Linux · SD · RAID · WinPE · Cloud · SBackup · WeChat) `[STATIC]`

Common patterns (artifact 15):
- **By-reference busy/state queries** (`sigIsBusy(bool&)`), result-code signals
  (`sigConnectRet(int)`, `SigEnumRet(bool)`) → error states with **Retry/Cancel/
  Disconnect triads**.
- NAS specifics: login (IP/port/account), SSH guidance ("Control Panel" on target NAS
  server + port tips), agent download progress objectNames
  (`NasDownloadPageProgressPercent/FileSize`), enumeration ("It will take a few minutes
  to find cloud drive files. Please be patient!"), per-device rows with **Connect /
  Scan / Support** actions; error pages EnumFailed/NoSystem; floating
  `CNasNetErrorTipsWnd` overlay attached to P06 during NAS scans; firewall prep runs at
  app start (netsh Network Discovery, EN+GBK group names).
- Linux: same agent pattern, zip download; "Due to techinical limitation, Windows
  8.1/8/7 or previous are not supported for Linux [recovery]".
- BitLocker (P05): entry copy "Select a location to unlock Bitlocker and start
  recovering"; password/key entry → unlock thread → result dialogs → scan proceeds on
  unlocked volume; icons switch lock→unlock via ImageResource type codes.
- Cloud (P15): "Sign in to iCloud"/Dropbox/GDrive/OneDrive → "Select a Cloud Drive to
  Start Recovering" → scan; privacy copy on login.
- WinPE (P14): media creator + `CPEBuyRetainWnd` retention offers.
- Async icon streaming for remote lists (pixmap push/pull by index) — required because
  lists render over the redirector.

### 3.11 P18/P18a/P19 — Preview surfaces `[STATIC+DYN]`

- **P18a live panel** `[DYN]`: double-click row → in-page render (real thumbnails; HTML/
  text render source inline), metadata stack `Name/[Preview]/Size/Date Modified/Path/
  Status/Recovery Chances`, gauge visualizing the `CCheckNodeThread` grade
  ("Recovery Chances: High" + circular gauge; "Status: Existing"). **The standalone
  `PreviewMainDialog` opens via the "Preview" BUTTON (context menu / panel), not the
  double-click.**
- **P18 shell**: `PreviewMainDialog` (66 v-methods — largest dialog) + shadow chrome +
  `EmbeddedPreviewWindow`; carries a **Recover CTA directly in the shell**; runs under
  the Freeze protocol (Freeze → exec → Unfreeze).
- **P19 per-type windows**: image (zoom/pan, custom `.cur` cursors), video (`timeout()`
  QTimer = the 15 s free cap; `CVideoPreviewTipsWnd` explains limits), rich text,
  archive (browse-inside damaged zips; MVC stack), PDF (raster paging
  `Preview %1 (%2/%3)`), plus tri-state views Loading→Failure/Content and
  `PreviewFailedWnd` failure grading.
- See §7 for formats/viewers/limits.

### 3.12 P23–P27 — Recovery funnel `[STATIC+DYN]`

**P23 destination picker** (29 QSS rules — most-styled surface; dynamically captured):
- Title "Choose a destination path to save the file:" + "You have N file(s) (size) to
  recover."
- **Local | Cloud tabs** (Local (410,222), Cloud (510,222)); destination list rows
  ("Windows (C:) 31.30 GB / 149.45 GB" + warning icons); **"Choose folder…"** row →
  native folder picker; Cloud tab: "You can select a Cloud drive to save the found
  data."
- **"Enable file repair" toggle — ON by default** (pill (255,613)) — repair-during-
  recovery queue (counters appear in P08 completion stats).
- Save button gray until a destination is selected ((707,613)).
- Safety rails: "Cannot save to original partition" (hard block for source);
  coaching ladder "Data might be OVERWRITTEN and LOST PERMANENTLY if you save files to
  original drive…" / "Do not save data to this drive"; recommendation to scan disk
  images; **insufficient-space gate** (P25): "Insufficient Disk Space… Need an
  additional 146.41 GB…" + Continue (proceed anyway) / Reselect (primary); cloud
  re-auth gate ("Login Failed" / Re-authorize / Reselect).
- Path validation errors: "Might be invalid path to save." / "2.The recovery path is
  too long." (numbered error taxonomy shared with the recovery log).

**P24 progress** (`RecoverProgressDialog`, 14 QM strings): per-file progress,
aggregate "Recovering…", "&Recovering: Disk %1", counters "Recovered: %1/%2 files",
cancellation via `doCancelRecovery` checkpoint. `[CORPUS-AMBIGUOUS]` A clean progress-
dialog capture was blocked by retention-gate timing; layout details beyond the QM
strings are not settled.

**P26 result** (`RecoverResultDlg`): "Recovery Successful" / summary "(%1 files /
%2 failed; for details, you may check the recovery log)" + "Recovery experience"
survey; partition-recovery success "%1 partition and its data were successfully
recovered!"; Auto-Recover completion notification variant.

**P27 free-limit gate** (`CRecoverLimitDlg`): "Sure to give up recovering remaining
valuable files?" / "Valuable data might be LOST PERMANENTLY if not recover now." /
counters / **Give Up vs Recover Remaining**; hard stop "No free recovery remaining."
/ "6.The data amount you want to recover has exceeded the free quota." Free quota
family: "The free version restores up to 2GB" (2 GB data cap) — see §12.

**P28 retention gates** `[DYN]`: "Wait! You are at risk of losing data!" + "N files (S)
have been found. More than 50% of permanent data loss occurs because unrecovered files
get overwritten by continued computer use. Take the final step now…" + rows **Get Up to
40 % Off / Online Service / Quit** + X (809,135). Trigger paths (all proven): Recover-
All with large set (intercepts before destination dialog), exit attempt (double-ESC),
and a **periodic ~30–60 s nag on Free builds on ANY page** (modeled as recurring
interruption with cooldown, not one-shot).

### 3.13 P20–P22r — Repair product surfaces `[STATIC]`

- **P20**: `FileRepairHomePage`/`AddPage`/list — 3-page QStackedWidget (0=Home,1=Add,
  2=List) driven by `FileRepairWidgetsMgr::switchToPage(N)`; drag-and-drop + Add Files;
  empty state "Click or drag files from your %1 to start the repair."; "Fix damaged or
  unreadable files."; Remove All / Close; progress header "Repairing:" / "Save:" /
  "Finish".
- **P21 video wizard**: Home → Add ("Click to add videos to start repairing") → Items →
  Advanced (sample) — "Add Sample Video for Advanced Repair" + coaching "Download a
  video of the same format from the Internet." → Analyze ("Analyzing sample video...")
  → Repairing (+ "Enhanced video repairing" / pause / "Repair Stopped, Videos
  repaired:") → Save (gated "Get Pro to Save Repaired Videos") → triage
  `RepairFailedWnd`/`RepairOriginalWnd`/`RepairedBottomWnd` + before/after compare
  (`RepairPreviewCompareWnd`). "Deep Video Reconstruct" = deep mode label.
- **P22r PDF wizard**: Home → Add → Decrypt ("%1 needs to enter password below to
  proceed with repair." → DecryptSuccess) → Progress ("Repair Stopped") → Preview
  (rasterized pages) → Save (gated "Get Pro to Save Repaired Documents") + info tips.
- **Row contract** (identical video/PDF moc blobs): per-row Repair / Preview / Save /
  Stop / Advanced-Repair(bFromChange) / Change / FeedBack buttons + progress bar + %
  label; task queue serializes work (`FileRepairDataMgr`); repair runs FREE for
  repair+preview; **saving repaired output is Pro-gated**.

### 3.14 P31–P42 — System surfaces `[STATIC+DYN]`

- **P31 DiskHealth**: separate Qt Quick app (840×590, frameless, tray-resident "is
  guarding your disk health"); disk selector, health card states "Good / Caution! /
  Normal / Your disk may have a physical failure:", S.M.A.R.T. attribute table
  (ID/Attribute/Current/Worst/Threshold/RawValue/Status/"Temprature" [sic]),
  CautionWidget "View Solutions", startup prompt, Buy/license integration. See §9.
- **P32 hamburger menu**: 13 visible items + 3 separators (positions §4 of atlas);
  conditional items Add Driver (0xC, "Load Driver %1 Success"), Save/Import Scan Result
  (0xF/0x10); submenus Language / Help (Video Tutorial, Online Help, Contact Support,
  Feedback, Logs) / Share / Theme (Use system setting / Light / Dark) / Display zoom
  (100/150/200 %); Survey row with red-dot badge. `[CORPUS-AMBIGUOUS]` Settings dialog
  contents (beyond Theme/DPI submenus) not decoded.
- **P36 AI assistant**: panel x653–922/y42–295, auto-opens ~60–75 s after landing,
  swallows clicks in bounds; 4 options (hard-drive solution wizard → 3-step modal,
  File Repair upsell w/ Pro badge, quick-start guide, chat); persistent FAB after
  dismissal; "Local AI | Secure" on-device inference claim.

---

## 4. Wizard State Machine

### 4.1 Transition table

| # | From | Trigger | Condition | To | Evidence |
|---|------|---------|-----------|----|----------|
| 1 | launch | DRW.exe loader spawns DRWUI | — | P00 splash | 04-userflow §6, DYN |
| 2 | P00 | device enumeration finished | disks present | P02 landing (stack idx 2) | DYN v4 |
| 3 | P02/P03 | Scan click (BtnScan / "Search for Lost Data" / device row) | — | P06 (idx 3) + scan start (`scanClickedSlot` → `CDetailPage::setScanDevice` → orchestrator start) | 04-userflow §4 |
| 4 | P06 | quick pass completes | user hasn't stopped | P07 overlay (advanced continues in background) | 08 §4, DYN |
| 5 | P06 | Stop button | — | P08 stop-confirm | DYN atlas |
| 6 | P08 | Continue | — | P06 scanning resumes | DYN |
| 7 | P08 | Stop | — | P06 stopped state (results preserved; resumable) | 08 §3 |
| 8 | P06 | Pause | — | P06 paused ("Scan Paused") | DYN atlas |
| 9 | P06 | full scan completes | — | **auto pipeline**: cache-reset cascade → Freeze → auto-`*.rsf` save → Unfreeze → `CScanMgr::ScanFinished` → recoverability pass → P08a celebration (540×380 standalone) | 30 §5, DYN |
| 10 | P08a | "Go to Recover" / X | — | P06 results browse + AI Recommend toast + FAB | 30 §2, DYN |
| 11 | P06 | double-click file row | — | P18a live preview panel | 30 §9.1, DYN |
| 12 | P06/P18a | context-menu/panel Preview button | — | P18 modal (`exec()` under Freeze) | 35 §11 |
| 13 | P06 | Recover with selection under free limit | — | P23 destination | 30 §3, DYN |
| 14 | P06 | Recover All / huge selection (Free) | — | **P28 retention gate (intercepts)** | 30 §3, DYN |
| 15 | P23 | Save (destination chosen, space OK, not source) | — | P24 progress | 11 §6 |
| 16 | P23 | insufficient space | — | P25 space gate (Continue/Reselect) | DYN atlas |
| 17 | P24 | progress completes | free quota not exceeded | P26 result | 11 §5 |
| 18 | P24 | free quota exceeded | — | P27 limit gate → P29 buy chain | 11 §4 |
| 19 | P06 | Back/Home/Sidebar | — | P02/P03 (`CGoHomeThread` async) | 04 §6 |
| 20 | P06 | exit attempt (double-ESC / close) | Free | P28 retention gate | 35 §12.5, DYN |
| 21 | any page | retention timer | Free build, ~30–60 s cooldown | P28 retention gate | 35 §12.15, DYN |
| 22 | P02 | locked BitLocker volume selected | — | P05 unlock chain → (unlocked) scan on P06 | 15 §3 |
| 23 | P02b | NAS/Linux/RAID/Cloud/WinPE/SBackup/WeChat tiles | — | P10–P17 chains → scan on P06 (remote/cloud strategy) | 15, 04-recovery §2 |
| 24 | P02d | whole-disk Start | — | P09 smart partition scan → select found partition → P06 | 08 §2 |
| 25 | P06/P03 | session Load / Import Scan Result | `.rsf` exists | P06 with restored tree (`sigLoadFinished`) | SaveScan |
| 26 | P06 | sidebar "File Repair" / destination "Enable file repair" / corrupted rows | — | P20 repair home → P21/P22r wizards | 12 §1 |
| 27 | P06 (NAS target) | scan start | NAS/remote | attach `CNasNetErrorTipsWnd` floating overlay | 04 §4.1 |
| 28 | P06 | driver missing for device | — | "Add Driver" menu (P32) → "Load Driver %1 Success" dialog → device list refresh | 35 §4 |
| 29 | any | DRWUI crash | — | DRW.exe watchdog → P42 DRWReport → DRWUI restart | launcher artifact |
| 30 | P02/P06 | drive removed mid-scan | — | disconnect dialog (Retry/Cancel) | 08 §5 |

### 4.2 ASCII state diagram

```
                        ┌─────────┐
                        │  P00    │  splash "Reading the disk information…"
                        └────┬────┘
                             │ enum done
                             ▼
        ┌───────────────────────────────────────────────┐
        │ HOME  (sidebar + stack; default idx 2)        │◄───────────────────────────┐
        │  P02 landing · P03 PC list · P02d lost-disk   │                            │
        └───┬──────────────┬───────────────┬────────────┘                            │
            │              │               │                                           │
   ┌────────▼───────┐ ┌────▼─────┐  ┌──────▼──────────┐   (all converge on scan)      │
   │ P05 BitLocker  │ │ P10-P17  │  │ P09 smart-part. │                              │
   │ unlock chain   │ │ scenario │  │ scan (lost disk)│                              │
   └────────┬───────┘ │ chains   │  └──────┬──────────┘                              │
            │         └────┬─────┘         │                                           │
            │              │  Scan         │                                           │
            ▼              ▼               ▼                                           │
        ╔══════════════════════════════════════════════════╗                          ║
        ║ P06 CDetailPage — SCAN + RESULTS (single page)    ║   Back/Home/CGoHomeThread║
        ║  quick pass ──► P07 upsell (advanced continues)   ║──────────────────────────╝
        ║  pause / stop ──► P08 confirm                     ║
        ║  NAS target ──► CNasNetErrorTipsWnd overlay      ║
        ║  live tree: Windows(C:) / File Path Lost /        ║
        ║              Reconstructed (grows while carving) ║
        ╚═══════════┬═══════════════════┬══════════════════╝
                    │ full completion   │ browse (3-pane)
                    │ (Freeze→auto.rsf  │
                    │  →Unfreeze)       ├────────────────────────────┐
                    ▼                   │ double-click row           │
        ┌────────────────────┐          │                            ▼
        │ P08a CELEBRATION   │          │                     ┌─────────────┐
        │ 540x380 OS window  │          │                     │ P18a live   │
        │ "Go to Recover"    │──────────┤                     │ preview     │
        └────────────────────┘          │                     │ + gauge     │
                                        │ Preview btn        └──────┬──────┘
                                        └──────────► P18 modal ◄────┘
                    ┌─────────────────────────────────────────┐
                    │ Recover (selection < free limit)        │
                    ▼                                         │
        ┌────────────────────┐   insuff. space   ┌─────────┐  │
        │ P23 destination    │◄─────Reselect────│ P25     │  │
        │ Local|Cloud tabs   │──────────────────►gate     │  │
        │ +repair toggle     │                   └─────────┘  │
        └─────────┬──────────┘                                │
                  │ Save                                      │
                  ▼                                           │
        ┌────────────────────┐  quota exceeded  ┌─────────┐   │
        │ P24 progress       │─────────────────►│ P27     │───┘ buy chain P29
        └─────────┬──────────┘                  │ limit   │
                  │ done                        │ gate    │
                  ▼                             └─────────┘
        ┌────────────────────┐
        │ P26 result + log   │
        └────────────────────┘

        Recover All / huge set / exit / periodic (Free) ──► P28 RETENTION GATE
                                                          (Get 40% Off / Online Service / Quit)
        Repair branch: P02c/P20 ──► P21 video wizard ──► Save gate (Pro)
                                  P22r PDF wizard (decrypt) ──► Save gate (Pro)
        Sessions: title-bar save / menu Import ──► .rsf ──► P41 banner ──► P06 restored
        Crash: DRWUI dies ──► DRW.exe watchdog ──► P42 DRWReport ──► DRWUI restart
```

---

## 5. Scan Modes & Progress Surfaces

**Mode names (UI-facing)** `[STATIC]`:

| Mode | Engine work | UI copy fragments |
|------|-------------|-------------------|
| Quick (default) | FS-metadata pass (MFT walk / FAT chains / inodes) via `EUCreate<FS>QuickSearcher` | "Quick Scanning%1, Files Found:" → "Quick Scan Completed" |
| Advanced / Deep | ALL FS searchers in full mode + `RAWSearcher` carving (190+ format analysers, `raw_signs.json`), gated by settings flag +0x60 | "Advanced Scanning%1, Files Found:" / "Advanced Scan is searching files sector bu sector…" [sic] |
| Smart Partition Scanning | whole-disk partition analysis (PartitionRecovery MOM) | "Scanning disk sectors: %1" |
| Deep Video Reconstruct | repair-line deep mode | repair wizard label |

**Staged model (runtime-verified)** `[DYN]`: quick ≈21 s (1.1 M files via MFT) →
advanced pass with sector ticker + linear remaining-time estimate ("Remaining time:
07m43s" at 34 %, "Reading sector: 134316032/31413598") → full session 14.7 min / 149 GB
→ completion pipeline (cache-reset cascade → Freeze → auto-`.rsf` → Unfreeze →
`CScanMgr::ScanFinished` → recoverability grading pass).

**Progress surfaces**: circular % gauge bottom-left; "Searching:" full-width ticker
(current file path); status line "Advanced Scanning, Files Found: N (size)";
`AnalysisGridWidget` sector-grid visualization; `RadiusProgressBar`/`RotateLabel`/
`UnllocateProgressLabel` family; "%1 file chunks scanned..." milestones; 48×48 tool
window during scan. Progress is **pull-polled** (38 `timeout()` consumers) while
lifecycle events push via signals `[STATIC]`.

**Live-results behavior**: results stream in during scan; the 3 tree roots render
mid-scan; "Reconstructed" grows live (1→16 in 6 min); search/filter/preview/recover
available mid-scan; "Recover All" active even mid-scan `[DYN]`.

**Pause/cancel semantics**: Pause = reversible freeze of gauge + "Scan Paused" strip;
Stop = confirm dialog (P08) framing rescue-anytime-later risk; `CStopScanThread`
cooperative cancel; scan-abort is a Win32 manual-reset event (`CScanMgr+0x108`);
stopped scans keep partial results and are resumable/recoverable.

**Scan→filter interplay** `[DYN]`: pre-set filter conditions are pushed INTO the scan
controller (`CDoScanPartition::PushFolderFilter`) — filters constrain incremental tree
insertion, not just post-hoc list filtering.

---

## 6. Results Explorer Anatomy

**Left tree — verified roots** `[DYN]` (match the shipped `.rsf` sample roots exactly):
1. **Existing-volume tree** — "Windows (C:) 1.1M" (structured FS results).
2. **File Path Lost** — "4.3K", warning icon (folder+question mark); deleted files
   whose paths couldn't be reconstructed.
3. **Reconstructed** — RAW-carved regrouped (folder+puzzle icon); grows live.

**Special branches (static coaching copy, 5 total)** `[STATIC]`:
- **Tags Extracted** — "We classify and organize the found files by extracted tags…"
- **File Name Lost** — "…files whose original name cannot be found…"
- **File Path Lost** — "Files we found with Advanced Scan, but the file path was fully
  or partially missing…"
- **Versions** — "It is another version of the file in the same directory…" (NTFS
  record multi-version; ties to the nested-event-loop row-decision modal)
- **Unimportant** — "…we organize some unimportant files here." (collapsed junk bucket;
  `usefullostfiles.xml` promotes carved RAW photos out of it)

**Type/classify tree**: same `CLeftTreeView` class switched by `IsTypeTree()`
(override-only subclass); 8 classify tabs (Audio/Document/Photo/Video/Archives/Emails/
Databases/Bookmarks telemetry) mapping FileFilter.xml's 8 categories / 1,489 extensions;
rebuild is async (3 s transition captured mid-state).

**List columns**: Name / Date Modified / Type / Size + recovery-chance column; sortable
headers (`CListHeaderView`); select-all tri-state checkbox; per-row checkbox; view
toggle list⇄icon (icon grid has 160 px icons with `LargeIconShadow.png`, 7 QSS
elements per item — full grid capture partial `[CORPUS-AMBIGUOUS]`); async per-row
thumbnails; 6-px slim scrollbars.

**Filter bar family** (two instances — one per Path/Type mode; shown on demand):
- Buttons: **Show / File Type / Date Modified / File Size** + **AI Recommend toggle**
  (+ CFilterLostFilesBtn "Lost files only").
- Dropdown option sets `[STATIC]`: File Type (Pictures/Videos/Documents/Archive/Emails/
  Bookmarks/Others/**Unsaved File**/Custom "Enter the file type suffix, such as *.doc…");
  Date Modified (Yesterday/Last 7 days/Last 1 month/Custom from–to); File Size (<512 KB/
  512 KB–1 MB/1 MB–512 MB/512 MB–1 GB/Custom); toggles (Existing / Hide 0KB Files /
  Hide System Files / Hide Temp Files / Hide Browser Cache); per-category "Select All
  (%1)" live counts; **Reset Filter**.
- Pipeline: menu check → 0x48-byte `AdvanceFilterCondition` (7 sub-condition groups:
  type/time/size/state/tag/path/search) → `CRightContent::onFilterChanged` →
  `CSetFilterThread` (TimeCriticalPriority) → model `SetFilter` virtual; empty condition
  = clear; per-mode "last filter" persistence (`CAdvanceFileFilterMgr` 3 slots).
- Count badges per button; `CFilterNoRlt` = no-result state; `CFilterGuideWnd` first-use
  coach; blocked-state copy "The file was filtered. Please clear the filter and then
  [search] again."

**Search**: inline box with 9-type dropdown; breadcrumb → "Search results for X" +
removable filter chip; empty-results coach `CSearchNltPopWnd` ("How to find files if it
searched nothing" + 3 strategies + Learn More / Got it).

**Selection model**: checkbox columns (`checkboxStateChanged`), select-all, context-menu
"Select unrecovered files" / "Smart select best files" / "Clear selection"; selection
checkpointed by reference (`sigGetRightIndex`); selection re-triggers recoverability
re-grading `[DYN]`.

**Context menu** (`CShowContextMenu`, 6 items) `[DYN]`: Recover · Preview · Open file
location · Select unrecovered files · Smart select best files · Clear selection.
Caution: Recover on a huge selection routes to the retention gate, not destination.

**Grading/confidence badges**: per-file "Recovery Chances" (High observed; grade 0/2 in
engine; circular gauge in preview panel); Status "Existing"; `RecoveryChancesTipsWidget`
coaching on rows; TRIM chips at device level; per-selection re-grading telemetry
("Recovery ability matched=%1, total=%2").

**AI layer in results**: "AI Recommend available." toast + persistent FAB; "AI Recommend
is on. Showing %1 recommended files." + "Switch views anytime to see all files.";
`DrwAIDedupWindow` (dedup results); "AI Recovery Insight:" cards.

---

## 7. Preview Surfaces

**Formats & viewers** `[STATIC]`:

| Content family | In-app window | External process |
|----------------|---------------|------------------|
| Images | `ImagePreviewWindow` (zoom/pan, custom cursors) | `EURawImg.exe` (RAW images) |
| Video/Audio | `VideoPreviewWnd` + `AVPreviewStub` | `EaseUS_VideoViewer.exe` (VideoViewer/; TCP `127.0.0.1:20171/20172` + ZeroMQ control; [602-byte media header][≤1 MiB BMP] thumbnail protocol) |
| Rich text / HTML | `RichTextPreviewWindow` (renders source inline for HTML) — | — |
| Office docs | `OfficePreviewStub` → | `EUOfficeViewer.exe`, `EUHwpViewer.exe` (HWP) |
| PDF | raster paging via `CPDFRepairGetPixmapThread` | `PDFViewer.exe` |
| Archives | `ArchivePreviewWindow` (browse-inside damaged zips) | `EUArchiveRepair.exe` (repair) |
| In-memory | `MemPreviewStub` | — |

Stub plugin family implements `IPreviewStub`: Mem/Office/Pdf/Image/RtfTxt/Archive/AV.
Streaming via `IInWindowStream` (range reads for large files/scrubbing).

**Preview during scan**: "You can preview and recover your files during the scanning
process." — available mid-scan from the session's in-memory metadata.

**Failure surfacing**: tri-state Loading→Failure/Content; `PreviewFailedWnd` copy
"Oops, file is not available for preview." / "File format does not support preview." /
"Preview Failed"; **damage grading bridges to repair**: "But this file is likely
recoverable based on its structure." + "Recovery Chances: High" → "Corrupted Video"
rows route to the repair flow.

**Limits/gates (free tier)**: video **15 s** cap (`VideoPreviewWnd::timeout()` QTimer);
**100 MB** size cap (HTML-styled warning); "Trial version is preview-only." /
"Preview only. Unlock Pro…" upsell; "Preview is not supported, recover to view this
partition" (FS needs recovery first).

---

## 8. Recovery Flow

1. **Selection** → Recover CTA (contextual: "Recover All …" ⇄ "Recover …" on selection).
2. **Interception layer** (Free): Recover-All/huge-set → retention gate P28 (before any
   destination UI) `[DYN]`.
3. **Destination** (P23): Local/Cloud; folder picker; repair-during-recovery toggle
   (default ON); source-partition hard block; overwrite-warning ladder; image-file
   recommendation; **space check precedes recovery** (Continue = proceed anyway /
   Reselect); cloud re-auth.
4. **Progress** (P24): per-file + aggregate; recovery-type dispatch (type 2 normal /
   3 alt / 5 Auto-Recover / 6 upgrade-flow when free-limit triggers); destination
   validated by opening `\\.\X:` raw; ACL-preserving copy; path-separator
   normalization; cancel via `doCancelRecovery`.
5. **Free-limit gate** (P27): counters, Give Up vs Recover Remaining; "6.The data
   amount you want to recover has exceeded the free quota."
6. **Completion** (P26): "Recovery Successful"; "(%1 files / %2 failed; for details,
   you may check the recovery log)"; recovery log; survey; urgency follow-up copy
   "Any unrecovered files risk being permantently overwritten by continued computer
   use."
7. **Collision handling**: destination validation + path-length/invalid-path numbered
   errors; `[CORPUS-AMBIGUOUS]` same-name collision overwrite/rename behavior at the
   destination is not explicitly decoded in the corpus (the engine dedups by
   reconstructing paths; no rename dialog is evidenced). Rewrite must define its own
   collision policy and should treat "no silent overwrite" as the safe default.
8. **Auto-Recover** (P38): bottom-bar offer post-scan; scope "Suggested Recovery" /
   "All Files"; destination validation ("Source Drive! Change another one." /
   "Looks good!"); optional email notification ("Invalid mailbox!"); completion
   notification dialog.

---

## 9. Health & Repair Surfaces

**DiskHealth.exe UI** (separate Qt Quick app, 54 QML components + shared EuS library):
frameless 840×590 dashboard; disk selector ("Which disk do you care about?" / "Disk %1");
health card states (Good/Caution!/Normal/"Your disk may have physical failure:");
S.M.A.R.T. attribute table (ID/Attribute/Current/Worst/Threshold/RawValue/Status);
CautionWidget with "View Solutions"; tray-guarding model (close hides to tray;
"Disk Health is guarding your disk health."); auto-start prompt; Buy/license surfaces.
In DRW proper: `BadSectorTipsDlg` coaching; "Launch Disk Health" entry from tool cards.

**Repair flows** — see §3.13 for wizard stage tables. Key contracts for the rewrite:
- Home→Add→List page flow (`switchToPage(N)`, drag-drop + file picker).
- Row contract (identical across video/PDF): Repair/Preview/Save/Stop/Advanced(bFromChange)/
  Change/FeedBack + progress.
- Task queue: `sigDoNextRepairTask` dequeue; `sigFinishRepairTasks` drains → batch Save;
  progress fan-out.
- Sample-file Advanced Repair (video): "Add Sample Video" + same-device guidance.
- Repair-during-recovery: "Enable file repair" toggle → counters in completion stats
  ("Video Repaired / Video to be repaired") → save gate at destination.
- Save is Pro-gated; preview/compare (before/after `RepairPreviewCompareWnd`) free.
- All repair workers run at TimeCriticalPriority (vendor convention — UI stays Normal).

**MOM module backing for P09/Partition surfaces**: Device/DeviceManager/Partition/.mo
modules enumerate devices/partitions behind the smart-scan UI (VDS + WMI + mountmgr
enumeration, error model shared via Error.mo). The rewrite's device enumeration service
maps to this layer.

---

## 10. Session Save/Load UI

**UI entries** `[STATIC+DYN]`:
- Title-bar **save-session button** (floppy/upload icon, (834,20)) — "1-Save Scan
  Result" telemetry.
- Hamburger menu: **Save Scan Result** (action 0xF; disabled on landing with no
  session) and **Import Scan Result** (0x10) → `.rsf` file picker ("Recovery State
  File(*.rsf)|*.rsf").
- Home **"Continue last scanning"** banner (P03/P41): "Continue last scanning · Scan
  time: · Scan files: · Scan disk: · Load" → `Result_Loadsession`; `LastScanRecordView`
  lists past sessions.
- **Auto-save on every scan completion** `[DYN]`: `SaveResultPath =
  ...\SaveScan\auto_Windows (C_)10_17_16_25_59.rsf` — naming scheme
  **`auto_<Volume>_<MM_DD_HH_MM_SS>.rsf`** (volume label with `:` → `_`).
  Shipped samples follow it: `auto_Local Disk (G_)10_15_00_44_10.rsf`,
  `auto_Local Disk (H_)10_14_23_42_58.rsf`, `auto_Local Disk (I_)10_14_23_13_21.rsf`.

**`.rsf` format (UI contract implications)** — fully reversed/validated:
- 4 records per session: (1) session state; (2) volume info incl. volume name (100-char
  field), device type, **SYSTEMTIME scan timestamp** (matches filename), elapsed-ms and
  tick stats — exactly the banner's "Scan time/Scan files/Scan disk" data; (3) partition
  context incl. disk identity + **512-byte boot-sector snapshot** (restore-time identity
  check); (4) **the complete found-file tree** (CFileNode/CFileSystemNode serialize;
  36 MB decompressed for a 1.1 M-file scan).
- Container: 16-byte magic, record headers chaining zlib-chunked data (4 KB chunks,
  level 5, stored-raw fallback per chunk); **graceful degradation** — on inflate
  failure the reader falls back to raw memcpy (recovery-product robustness).
- Save is off-thread (`SaveRestoreThread`); restore shows a wait-dialog (slot 38
  `QDialog::exec`) then `sigLoadFinished(bool)`.

**Rewrite contract**: keep an equivalent auto-saved session model (metadata record +
full tree), the resume banner, and import/export entries; our on-disk format need not
be `.rsf`-compatible, but the *UX contract* (auto-save silently on completion; explicit
save/import menu entries; resume banner with time/files/disk summary) should be
preserved.

---

## 11. Error & Edge-State Dialog Inventory

| Situation | Surface | Copy fragments | Controls | Evidence |
|-----------|---------|----------------|----------|----------|
| Drive removed mid-scan | P06 dialog | "Drive Disconnected — Unable to continue scanning. Reconnect your drive and try again." | Retry / Cancel | 08 §5 |
| NAS offline | P06 dialog | "NAS Disconnected — Check network and make sure NAS is on. Then try again." | Retry / Cancel / Disconnect | 08 §5 |
| Linux box offline | P06 dialog | "Disconnect with Linux" flow (same pattern) | Retry / Cancel / Disconnect | 08 §5 |
| Cloud session lost | P06 dialog | "Cloud Drive Disconnected — Please check your network connection…" | Retry / Cancel / OK | 08 §5 |
| Network generic (param) | P06 dialog | "Check your network to make sure the %1 server is accessible and then r[etry]" | Retry / Cancel | 08 §5 |
| NAS scan overlay | `CNasNetErrorTipsWnd` | floating tips, resize-tracking | — | 04 §4.1 |
| NAS connect failure | NAS chain | "Incorrect IP or port or disabled SSH service"; retry ("retry %d") | Retry / Cancel | 04-recovery §10 |
| NAS enumeration failure | `CNasEnumFailedPage` | — | retry path | 03 §4 |
| NAS no system | `CNasNoSystemPage` | — | — | 03 §4 |
| BitLocker unlock failure | `BitlockerUnlockRetDlg` / `RetWithPwdDlg`; MS guide `BitLockerMSGuideDlg` | unlock result states; manage-bde walkthrough | Retry / guide | 15 §3 |
| Partition enumeration failure | `CPartitionEnumFailFrame` | — | Retry | 03 §5 |
| Empty recycle-bin scan | `RecycleBinScanNoRltWnd` | empty-result state | — | 03 §5 |
| Stop scan | `CScanCommonPopDlg` | "Stop searching for lost data?" + risk framing | Stop / Continue | 08 §3, DYN |
| Stop repair | `CScanCommonPopDlg` mode | "Stop Repairing for corrupted video?" | — | 08 §3 |
| Stop organizing | `CScanCommonPopDlg` mode | "Stop Organizing Found Files?" | — | 08 §3 |
| Driver missing for device | "Add Driver" menu (0xC) | "Load Driver %1 Success" | — | 35 §4 |
| Search no results | `CSearchNltPopWnd` | "How to find files if it searched nothing" + 3 strategies | Learn More / Got it | 09 §4 |
| Filter no results | `CFilterNoRlt` | empty-state | Reset | 09 §4 |
| Filter blocked search | message | "The file was filtered. Please clear the filter and then [search] again." | — | 09 §4 |
| Preview failure | `PreviewFailedWnd` | "Oops, file is not available for preview." / "File format does not support preview." | grading + repair handoff | 10 §2 |
| Video preview limit | `CVideoPreviewTipsWnd` | "15s preview only" | upsell | 10 §3 |
| Preview size cap | warning | "Files larger than 100MB are not available for preview." | — | 10 §3 |
| Insufficient destination space | P25 gate | "Insufficient Disk Space… Need an additional %1 of space…" | Continue / Reselect | 11 §2, DYN |
| Save to source partition | P23 block | "Cannot save to original partition" / "Do not save data to this drive" | Reselect | 11 §2 |
| Invalid/long destination path | P23 validation | "Might be invalid path to save." / "2.The recovery path is too long." | — | 11 §2 |
| Free quota exceeded | `CRecoverLimitDlg` | "6.The data amount you want to recover has exceeded the free quota." | Give Up / Recover Remaining | 11 §4 |
| Cloud login failed | P23 cloud tab | "Login Failed" / Re-authorize / Reselect cloud | — | 35 §10 |
| License states | `CEnterLicenceDlg` | Expired / Invalid length / Invalid / Locked / **Pirate** | Activate | 11 §4 |
| Host tampering (anti-piracy) | `PirateVictimsTipDlg` | "…host settings have been maliciously tampered…" | — | 11 §4 |
| Bad sectors | `BadSectorTipsDlg` | bad-sector coaching | — | 06 §1 |
| Stuck/frozen UI | `CExitHelpWnd` (+`FreezeAnimalDialog`) | "Stuck on refreshing?" coaching | — | 16 §5 |
| Refresh flow | refresh modal (slot 7) | device-refresh progress | — | 04 §5 |
| Repair failure | `RepairFailedWnd` / rows | "Quick Repair Failed" → "Try Advanced Repair" | FeedBack | 07-userflow §4 |
| App crash | `DRWReport.exe` (MFC) | crash details + "Serial Number: %s" + send options | send/restart | DRWReport artifact |
| Cloud destination privacy | login sheet | "EaseUS NEVER keeps records of your account…" | — | 02 §3 |
| Win7/8 unsupported (Linux flow) | Linux chain | "Due to techinical limitation, Windows 8.1/8/7 or previous are not supported…" | — | 15 §2 |
| SD no device | `CSDCardNoDevicePage` | connect-guidance | — | 03 §4 |
| RAID no device | `CRaidNoDevicePage` | — | — | 03 §4 |

---

## 12. Feature Gates (Free vs Pro)

**Free tier limits (observed + static)**:

| Capability | Free | Evidence |
|------------|------|----------|
| Recovery quota | **2 GB data cap** ("The free version restores up to 2GB" family); "Recovered: %1/%2" counters; hard stop "No free recovery remaining." | 30 §3, 11 §4 |
| Preview | allowed, but video 15 s, >100 MB blocked, "Trial version is preview-only." | 10 §3 |
| Repair | repair + preview free; **saving repaired output Pro-gated** ("Get Pro to Save Repaired Videos/Documents") | 12 §5 |
| Scan | full scan allowed ("Scan disk only") | 11 §4 |
| Monetization chrome | Buy Now chip; ad banners; retention nag periodic; win-back dialogs; VIP chat | 16/17 |

**Pro feature matrix** (`CLocalRecoverBuyDlg`, verbatim fragments): "One-time Purchase -
NO SUBSCIPTION" [sic] · "Recover Unlimited Data" · "Recover SD Card" · "Recover Nas,
Linux and RAID" · "Rescue Lost Partition" · "Rescue Crashed PC" · "Repair Videos,
Photos, Documents and Audio". FREE column: "Preview only" · "Limited recovery" ·
"Scan disk only" · "Preview before recovery" · "Technical support".

**Licensing surfaces**: `CEnterLicenceDlg` (+ Paste/Activate, "Don't have a license?"
→ Buy), Apower/External activate dialogs, activation result states, VIP tier
("1-Get VIP…" / "Try VIP Chat"), `TitleGiftDlg` expiry nag, Enterprise/Professional
welcome banners, CN-mainland payment variants (`ChinaMainlandbuyIeWnd` embedded IE),
in-app web checkout (`InnerWebWindow`), `RetentionActiveDlg`/`RetentionExitDlg`,
`CRecoverFinishBuyDlg` win-back with social proof ("%1 users choose to activate Pro…",
"30 million").

**Gate mechanics for the rewrite**: the gate pivot is the recovery-export path —
quota check occurs at/after progress starts (`CRecoverLimitDlg` mid-recovery) and on
Recover-All (retention interception before destination). Licensing is woven into the
scan path itself (`CVersionCtrl` prep calls at every scan start) — i.e., version state
is consulted before scan, not only at recovery.

---

## 13. Gap Notes for Our Rewrite (Omit / Replace Decisions)

**Consciously OMIT (out of scope by project policy)**:

1. Cloud-drive recovery (P15, cloud destinations, iCloud/OAuth surfaces) — transport
   and account handling out of scope; local-disk focus.
2. NAS & Linux recovery chains (P10/P11, EuMrx redirector, SSH agent) — requires kernel
   redirector + agent downloads; out of scope. Rationale: our driver stack is local-only.
3. RAID reconstruction (P13) — out of scope.
4. WeChat / SBackup integrations (P16/P17) — third-party product bridges.
5. WinPE media builder (P14) — bootable-media creation out of scope for the wizard
   rewrite; keep a documented stub only if needed later.
6. ChronoSnap backup product (P02e/P40) — separate product line; disk-image *creation*
   UI omitted (disk-image *scanning* is a core-engine concern, not UI).
7. AI layer (assistant panel, AI Recommend ranking, AI dedup, "AI Recovery Insight")
   — on-device inference claims and ranking funnel out of scope; omit the toast/FAB
   surfaces and the AI filter toggle.
8. Monetization surfaces: ad banners, gift/discount frames, retention gates, win-back
   dialogs, VIP chat, referral/share, NPS rating, anti-piracy dialog, embedded-IE
   checkout — all omitted by policy (no dark patterns; the rewrite is not funnel-first).
9. Telemetry (195-event analytics, Aliyun upload, Firebase remote config, DRWReport
   upload) — replaced by local structured logging only.
10. Holiday/seasonal skinning, FreezeAnimalDialog marketing animation — omit.
11. Multi-language `.qm` matrix — the rewrite ships EN first; keep the string-table
    discipline (no hardcoded literals scattered in markup) so localization remains
    possible.
12. OEM/white-label About variants (Apower/Cisdem/Foxit builds) — single identity only.
13. Driver-add flow ("Add Driver %1") — our stack owns its driver; keep a driver-status
    surface instead.

**Consciously REPLACE (behavior kept, surface redesigned)**:

1. Free/pro gates → a simple local license state (or none initially) with an honest
   quota indicator; no pressure copy, no periodic nag, no exit interception.
2. Purchase webviews (embedded IE + EUinApp emulation shim) → external browser link at
   most; never an in-app IE control. (Win7-era vestige.)
3. Crash handling → standard .NET unhandled-exception reporting + structured local
   logs; no 2 MB shared-memory supervisor handshake. (Keep the *behavioral* guarantee:
   scan sessions survive crashes because they auto-save — that is worth keeping.)
4. Splash enumeration gate → keep (short, cancellable), with real progress and a
   "scanning for devices…" status.
5. QSS-in-stylesheet layout discipline → WPF resource dictionaries + proper layout
   system; keep the token discipline (palette slots, 4-step neutral ramp, spacing
   constants) as design tokens.
6. Custom `.cur` cursors / hand-rolled title bar → native WPF chrome; custom cursor
   visuals only where meaningful (preview pan/zoom).
7. The 540×240 fixed in-house message box → standard WPF dialog system with consistent
   visual language; content patterns (title/body/icon/button triads) preserved.
8. The "solution wizard" modal chain and coaching overlays → modern help/insight pane;
   behavior (3-step guidance) preserved, presentation modernized.
9. TRIM chip, recovery-chances gauge, overwrite warnings, path-lost coaching → keep all
   (trust engineering), with modern visuals.
10. Two-phase quick→advanced scan with live tree → keep exactly (core UX); the quick-
    scan upsell dialog becomes a non-blocking banner offering "continue deeper scan".

**Win7-era conventions to modernize (behavior preserved)**: embedded IE webviews;
`.cur` cursors; VC90/VC120-era redist manifests; GBK-encoded commands for CN locale;
tray-hide-on-close; "Don't show this again" checkboxes; modal-everything interaction
(rewrite can keep modals where the corpus shows blocking semantics, e.g., stop-confirm,
destination picker, but should prefer non-blocking for progress).

---

## 14. Traceability Appendix

| Page/Section | Evidence (artifact §) |
|--------------|----------------------|
| P00 splash | UIUX/dynamic-ground-truth--splash-captured.md (whole); 03 §2; 04 §6 |
| P01 shell/title bar/sidebar | UIUX/16 §1–3; 35 §2 (pixel map); 05 §2 (geometry); dynamic-ground-truth--landing-page-windows.md §1–2 |
| P02 landing | UIUX/07 §3; 35 §5; dynamic-ground-truth--landing-page-windows.md §2 (verbatim strings) |
| P02a–P02f sidebar pages | 35 §5 (sidebar destination pages, round-6 captures) |
| P03 PC home | UIUX/07 §2 (ctor+moc+QSS) |
| P02d lost-disk | UIUX/07 §4; 35 §5 (3 scenario cards) |
| P04/P05 partition & BitLocker starts | 03 §4; 15 §3; 07-userflow §2 |
| P06 scan+results page | UIUX/04 §4 (scanClickedSlot); 08 (whole); 09 (whole); 14 (42 slots); 34 (toolbar/status bar); dynamic-ground-truth--scan-phase-captured.md (§2 + addenda); 30 §2/§5/§9 |
| P07 quick-complete | UIUX/08 §4 |
| P08/P08a scan dialogs/celebration | UIUX/08 §3; 30 §1 (standalone-window census); 35 §7 |
| P09 smart partition scan | UIUX/08 §2; MOM-modules/Device.md + DeviceManager.md + Partition.md (enumeration backing); 04-recovery §2 |
| P10 NAS chain | UIUX/15 §1 (signal contracts); 03 §4; 07-userflow §2 |
| P11 Linux chain | UIUX/15 §2 |
| P12 SD chain | 03 §4; 07-userflow §2 |
| P13 RAID | 03 §4; 21 §2 (QSS Raid section) |
| P14 WinPE | UIUX/15 §4; 03 §4 |
| P15 Cloud | 03 §4; 07-userflow §2; 04-recovery §2 |
| P16 SBackup | 03 §4 |
| P17 WeChat | 03 §4; 13 §7 |
| P18/P18a/P19 preview | UIUX/10 (whole); 18 (class system); 30 §9.1 (live capture); 35 §11; DRWUI-exe--main-ui-architecture.md §9.5 (VAPreview ports) |
| P20–P22r repair | UIUX/12 (whole); 32 (row contracts + page flow + queue); 07-userflow §6 |
| P23 destination | UIUX/11 §1–2; 35 §10 (dynamic internals); 21 §5 (29 QSS rules); 33 §6 (objectName identity) |
| P24 progress | UIUX/11 §3; 04-recovery §7 (export chain) |
| P25 space gate | 35 §10 (capture); 11 §2 |
| P26 result | UIUX/11 §5 |
| P27 limit gate | UIUX/11 §4 |
| P28 retention gates | 30 §3; 35 §10.15 + §12.5 (trigger paths); 17 §1 (retention stage) |
| P29 buy dialogs | UIUX/11 §4; 17 §1; 23 §5 (checkout correction) |
| P30 activate | 35 §4b (capture); 11 §4 |
| P31 DiskHealth | UIUX/19 (whole); 01 §6 |
| P32 hamburger menu | 35 §4 (open capture + static inventory) |
| P33 About family | 16 §2 (OEM matrix); 21 §2 |
| P34 update check | 35 §4 |
| P35 first-run tour | 07 §5; 03 §10 |
| P36 AI assistant | 35 §3; 16 §4 |
| P37 AI results surfaces | 30 §2; 16 §4; 31 §3.3 |
| P38 Auto-Recover | 09 §6 (16 settings strings); 34 §2.2 (runtime controller); 25 §6 (wiring closure) |
| P39 rating/feedback | 16 §5; 06 §1 |
| P40 disk backup | 03 §10; 33 §3 (QSS targets) |
| P41 sessions | SaveScan/rsf-format--scan-session.md (feature strings + format); 07 §2 (banner); 35 §4 (menu items); 30 §5 (auto-save naming) |
| P42 crash reporter | DRWReport-exe--crash-reporter.md; DRW-exe--launcher.md §2 |
| State machine (§4) | UIUX/04 (17-slot view manager + journey); 25 §3 (flow wiring edges); dynamic-ground-truth--scan-phase-captured.md §8 (end-to-end flow) |
| Scan modes (§5) | 04-recovery §3–4; dynamic-ground-truth--scan-phase-captured.md §3/addenda; 08 §1–2 |
| Results anatomy (§6) | 09 (whole); 31 (filter chain); 20 (tree dispatcher); 22 §4 (CRightContent); 30 §9 (interactions); 35 §8–9 |
| Preview (§7) | 10; 18; DRWUI-subsystems--assistant-grading-preview.md §4; 04-recovery §6 |
| Recovery (§8) | 11 (whole); 04-recovery §7; 30 §3; 35 §10 |
| Health/repair (§9) | 19; 12; 32; MOM-modules/{Device,DeviceManager,Partition,Error,Mom,Common}.md |
| Sessions (§10) | SaveScan/rsf-format--scan-session.md §2–5 (validated); 30 §5 (auto-save); og-codebase/SaveScan/ filenames |
| Errors (§11) | 06 (dialog+telemetry census); 08 §5; 04-recovery §10; DRWReport artifact |
| Gates (§12) | 17 (funnel map); 11 §4; 30 §3 |
| Wiring/threading background | 13 (singletons/state); 25 (722 connect pairs); DRWUI-exe--thread-architecture.md (45 threads) |

**Known corpus ambiguities (decision list for the rewrite owner):**

1. Settings dialog contents beyond Theme/DPI submenus — `[CORPUS-AMBIGUOUS]`.
2. Exact 14-tile registry vs. shipped sidebar/card landing composition mapping —
   `[CORPUS-AMBIGUOUS]` (recommend sidebar+cards as ground truth).
3. Clean `RecoverProgressDialog` layout (capture blocked by retention gate) — strings
   known, geometry not — `[CORPUS-AMBIGUOUS]`.
4. Standalone `PreviewMainDialog` full internals (trigger mapped, exec-flow decoded,
   window capture blocked) — `[CORPUS-AMBIGUOUS]` on exact chrome.
5. Icon (grid) view full layout — partially captured — `[CORPUS-AMBIGUOUS]`.
6. Dark-theme rendering (bundle extracted, switch blocked in captures) — tokens known,
   live rendering not captured — `[CORPUS-AMBIGUOUS]`.
7. Destination same-name collision policy — not evidenced — `[CORPUS-AMBIGUOUS]`.
8. Language/Help/Settings submenu dialogs (About/CheckUpdates) — inventory + action IDs
   ready, dialogs not captured — `[CORPUS-AMBIGUOUS]`.
9. Repair-wizard pages dynamic capture — static decode only (labeled "future" in 35 §13).
10. NAS/BitLocker/lost-disk flows — static decode only; no dynamic pixel captures.

---

*End of UI-MAP.md — Task 2-a deliverable. Corrections should be filed against the
traceability table above, citing the same artifact §IDs.*
