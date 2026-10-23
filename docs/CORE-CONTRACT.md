# CORE-CONTRACT.md — the `core_recovery` ⇄ UI integration contract

**Single source of truth for the rda-wizard WPF UI.** The UI must never invent a
capability that is not documented here; everything here is derived line-by-line from
the `core_recovery` public headers and the `rrecov` CLI implementation.

- Repo under review: `heicfast/core_recovery` @ **183dfcd** (`fix(win)!: alignment-descriptor
  offsets were the CACHE-LINE fields - slice creation never worked on direct-path volumes`)
- Public surface audited: **45 headers** under `include/cr/`, the CLI `src/app/rrecov.cpp`
  (1191 lines), orchestrator implementation `src/core/orchestrator/cr_scan.cpp` (638 lines),
  writer `src/core/recovery/cr_safewriter.cpp`, Win32 layer `src/win/*.cpp`.
- Authority rule: **headers win** over docs; implementation spot-checks resolve
  "declared but not wired" questions. Every discrepancy found is flagged `DISCREPANCY`.
- Stabilization posture: session-5 audit is **closed** (AUDIT.md: "every row below is
  worked"); the surface has been stable through the v0.2 waves. Residual risk: the
  Windows layer (`src/win`) is compile-verified on CI only (not locally verifiable on
  the Linux dev box), and the project is an active recovery engine — treat function
  *signatures* as stable, treat *fill-rates of struct fields* as evolving (noted inline).

---

## 1. Overview

`core_recovery` is a user-mode Windows data-recovery engine stack (clean-room rebuild of
the EaseUS DRW reference architecture, evidence-cited in `docs/ground-truth-contracts.md`):

- **Pure core** (`src/core`, static lib `cr_core`, C++20, cross-platform Linux+Windows):
  all filesystem engines (NTFS, FAT12/16/32, exFAT, ext2/3/4, HFS+/HFSX, Btrfs, ZFS,
  ReFS **full engine**, LDM **full decode + dynamic-volume reconstruction**), lost-partition
  discovery, grammar+raw-sign carving (30 grammar formats + 139 raw-sign formats), the
  scan orchestrator, the result-tree graph, CRSF sessions, the safe recovery writer,
  transactional repair (RawFixer), decompression (lzjb/zle/gzip/lz4/zstd), SHA-256.
- **Windows platform layer** (`src/win`, static lib `cr_win`, MSVC only): disk/volume
  enumeration, RDA kernel-driver client + direct-handle transports, SMART/NVMe health,
  TRIM query, BitLocker detect/unlock, Win32 destination IO.
- **CLI** (`rrecov.exe`, `src/app/rrecov.cpp`): `list / scan / tree / recover / health /
  fix / bench` — the CLI-parity and contract-test surface (§13).

Version note (DISCREPANCY, cosmetic): CMake `project(core_recovery VERSION 0.1.0)` while
`docs/roadmap.md` declares v0.2 shipped. Treat the roadmap as the feature-level truth.

**How the UI links it** — two sanctioned models, see §2 for the exact constraints:

1. **In-process**: a UI-owned native shim (C++/CLI or flat C export DLL) compiled with the
   same MSVC toolchain linking `cr_core` + `cr_win`.
2. **Subprocess**: drive `rrecov.exe`, exchange state through `.crsf` session files and
   parse stdout (formats frozen in §13).

---

## 2. Linkage model

### 2.1 What actually exists (no C ABI today)

- Build targets: `cr_core` (STATIC), `cr_win` (STATIC, links `cfgmgr32 Advapi32`), `rrecov`
  (exe). External deps: zlib (sessions), liblz4 + zstd (ZFS compressed extents; optional at
  build time — `compress::codec_available()` reports).
- **There is no `extern "C"`, no exported DLL, no flat C interface anywhere in
  `include/cr/`.** The public surface is C++20: namespaces (`cr::`, `cr::tree::`,
  `cr::orchestrator::`, …), classes with virtuals (`IStorageDevice`, `DestIo`,
  `IWriteDevice`, `CancelToken`), `std::unique_ptr` returns, `std::u16string`,
  `std::function` (carve namer), raw function-pointer callbacks.
- Consequence: an in-process UI **must** compile its shim against these headers with a
  binary-compatible MSVC (same major toolset, same C++ runtime flavor) — or the UI team
  adds its own flat-C wrapper DLL (UI-owned; not a core deliverable). See GAP **G-01**.
- Calling convention: default MSVC `__cdecl` everywhere; no `__stdcall`, no varargs in the
  public headers. All callbacks are plain function pointers (no `__cdecl` annotations).

### 2.2 Universal error model — `cr::Err` (`cr_error.h`)

Every API returns `u32`; **0 (`cr::OK`) is success**, everything else is an `HRESULT`-style
code. `const char* err_text(u32 code)` renders text (used by the CLI; stable strings).

| Family | Codes |
|---|---|
| open/transport `0x5002xxxx` | `ERR_OPEN_FAILED 0x50020001`, `ERR_READ_FAILED …002`, `ERR_WRITE_FAILED …003`, `ERR_RANGE_OUT_OF_BOUNDS …004`, `ERR_CANCELLED …005`, `ERR_RETRY_EXHAUSTED …006` |
| format/parse `0x5003xxxx` | `ERR_BAD_MAGIC 0x50030001`, `ERR_BAD_CHECKSUM …002`, `ERR_BAD_PARAMETER …003`, `ERR_TRUNCATED …004`, `ERR_FIXUP_FAILED …005`, `ERR_CYCLE_LIMIT …006`, `ERR_DEPTH_LIMIT …007`, `ERR_UNSUPPORTED …008`, `ERR_NO_MEMORY …009` |
| session `0x5004xxxx` | `ERR_SESSION_FORMAT 0x50040001`, `ERR_SESSION_IO …002`, `ERR_SESSION_VERSION …003` |
| writer `0x5005xxxx` | `ERR_DEST_OVERLAP 0x50050001`, `ERR_DEST_UNWRITABLE …002`, `ERR_HASH_MISMATCH …003` |
| BitLocker `0x5006xxxx` | `ERR_FVE_LOCKED 0x50060001` |
| health/repair `0x5007/8` | `ERR_HEALTH_UNAVAILABLE 0x50070001`, `ERR_REPAIR_REFUSED 0x50080001`, `ERR_REPAIR_UNDO_FAILED …002` |
| internal | `ERR_INTERNAL 0x50090001` (task exceptions in the pool) |

### 2.3 Threading model — `cr_parallel.h`

- Engines are **single-threaded per unit of work**; the orchestrator fans units out over
  `cr::ThreadPool` (fixed workers, MPMC FIFO, cooperative abort). `ThreadPool(workers=0)`
  = auto (hw concurrency − 1, clamped 1..8). `run(task)`, `abort()` (idempotent, wakes all
  workers, drops queued tasks), `aborted()`, `pending()`, `wait_idle()`, `busy()`.
- `PoolCancelToken` adapts a pool's abort state to a `CancelToken`.
- `u32 parallel_for(ThreadPool&, u64 count, fn(u64)->u32)` — blocks; `ERR_CANCELLED` on
  abort; exceptions in `fn` surface as `ERR_INTERNAL` per item.
- **Every long-running API is synchronous** (`ScanController::run`, `SafeWriter::process`,
  `lost::discover`, engine scans). The UI must call them from worker threads. Callbacks
  fire on the calling/scan thread — return fast, never block.
- Cancellation is **cooperative**: a `CancelToken*` polled at loop heads, plus
  progress-callback `return false` where documented (§5.4). There is **no pause/resume
  API**; resume = session persistence only (§9, GAP **G-07**).

### 2.4 Memory ownership rules (every out-parameter)

| API shape | Ownership |
|---|---|
| `u32 f(..., T& out)` / `(T* out)` | Caller-owned value; core fills |
| `std::unique_ptr<T>` returns (`PartitionSlice::create`, `CachedReader::create`, `RetryingDevice::create`, `NtfsVolume::open`, `RdaClient::open`, `win::open_*`, `lost::materialize`) | Caller owns; `unique_ptr` destroys |
| `tree::FileProperties*` | **Refcounted**: `add_file()` takes one reference per call; caller releases its own. `clone()` returns a caller-owned ref. Never `delete` directly |
| `tree::FileNode*` / `FullNode*` | **Tree-owned** (arena, bulk-freed by `~ResultTree`); never freed by caller |
| `std::vector<u8>& out` (serializers, undo log) | Caller-owned buffer |
| Borrowed raw pointers: `ScanController(tree&, cfg)`, `SafeWriter(DestIo&, IStorageDevice* source, cfg)`, `PartitionSlice(parent)` (parent NOT owned — caller must keep parent device alive), `LdmVolumeDevice(layout&, resolver)` | Borrowed; must outlive the borrower |
| Session `Writer`/`Reader` | Pure-core: IO via caller callbacks (`WriteFn/TellFn/SeekFn`, `ReadFn/SeekFn/SizeFn`) + `void* user` |
| Strings | Node names are `std::u16string` (UTF-16, exact); win layer uses `std::wstring`; CLI narrows lossily for display only |

---

## 3. Types inventory (UI-relevant)

### 3.1 Scalars & time (`cr_types.h`)

`u8/u16/u32/u64/i8..i64`. **`FileTimes`** — `{u64 created, modified, changed, accessed}`,
all **NTFS-style 100 ns since 1601-01-01**, preserved exactly (R2.2). Converters:
`ntfs_time_to_unix_ns`, `ext4::unix_to_filetime`, `zfs::unix_to_filetime`,
`btrfs::timespec_to_filetime(sec,nsec)`, `hfsplus::mac_to_filetime` (1904 epoch),
`fat_date_to_unix(date,time,tenths)`.

### 3.2 Filesystem-kind codes — TWO namespaces (do not mix)

**`cr::FsKindCode`** (`cr_types.h`, corpus FACTORY map — this is what **nodes carry** in
`FileProperties::fs_kind` and what the RESULT_TREE session record stores):

| Value | Kind | | Value | Kind |
|---|---|---|---|---|
| 0 | Fat12 | | 8 | Hfsplus |
| 1 | Fat16 | | 9 | Ext4 |
| 2 | Fat32 | | 10 | **Refs** |
| 3 | Ntfs | | 11 | Btrfs |
| 6 | Raw (carved) | | 13 | Zfs |
| 7 | Exfat | | | |

Legacy-session remap on load: 12 → 13 (content-map ZFS); legacy Btrfs-7 is
indistinguishable from exFAT-7 (documented as F16).

**`cr::orchestrator::FsFamily`** (`cr_scan.h`, probe/dispatch enum — this is what
`ScanConfig::fs_hint` and **`session::VolumeInfo::fs_kind` actually store**):
`Unknown=0, Ntfs=1, Fat=2, Exfat=3, Ext4=4, CarvedOnly=5, Hfsplus=6, Btrfs=7, Zfs=8,
Refs=9`. Note `CarvedOnly` is declared but never produced by `probe_family`.
DISCREPANCY (impl vs header comment): `cr_records.h:47` comments VolumeInfo.fs_kind as
factory codes ("NTFS 3 … carved 0xFF") but `cr_scan.cpp:573` writes `u32(cfg_.fs_hint)`
(FsFamily). Also `0xFF` is stale — carved is Raw=6 since audit row 45.

**`table::FsKind`** (`cr_table_parse.h`, partition classification):
`Unknown=0, Ntfs, Fat12, Fat16, Fat32, Exfat, Refs, Ldm, Extended, Protective, Reserved,
Linux, Other`. LDM is first-class here (`FsKind::Ldm`, MBR type 0x42, GPT LDM
metadata/data GUIDs in `cr_table_wire.h`).

### 3.3 Result-tree node shape (`tree/cr_node.h`)

```cpp
enum class Origin      : u8 { Structured=0, Carved=1, Lost=2, Virtual=3 };
enum class DeletedState: u8 { Active=0, Deleted=1, PartiallyOverwritten=2, Damaged=3 };
enum class Confidence  : u8 { Unknown=0, Excellent=1, PartialRisk=2,
                              Overwritten=3, MetadataOnly=4 };
```

**Confidence semantics (as returned):** `Excellent` = all extents verified unallocated in
$Bitmap (NTFS; ReFS allocator-grading parity); `PartialRisk` = some extents reused;
`Overwritten` = all extents reused (metadata-only salvage value); `MetadataOnly` = no
extents at all (torn/residentless record). Carved nodes additionally carry
`carve_confidence` (0..1000, see §6.3).

`FileProperties` (refcounted content object, hard links share it):

| Field | Type | Meaning |
|---|---|---|
| `record_ref` | u64 | NTFS MFT reference `record \| seq<<48` (hard-link grouping); 0 for carved |
| `fs_kind` | u32 | `FsKindCode` (see §3.2) |
| `logical_size` / `allocated_size` | u64 | real / allocated bytes |
| `times` | FileTimes | 4× 100ns/1601 |
| `attributes` | u32 | DOS attributes — **bit 0x10 = directory** (the CLI's dir test) |
| `flags` | u16 | internal; bit 4 = system-filter (corpus) |
| `name` | std::u16string | full display name (LFN / carved name) |
| `resident_data` | vector<u8> | NTFS resident $DATA/ADS, ZFS embedded block pointers, Btrfs inline extents — emitted before extents |
| `extents` | vector<Extent> | **absolute device byte offsets**; `Extent{u64 offset; u64 length; u32 codec; u64 psize}` — codec≠0 ⇒ on-disk `psize` bytes at `offset` decode to `length` logical bytes via `compress::decompress_block` |
| `carve_format` / `carve_confidence` | u32 / u32 | signature-registry format id; 0..1000 |
| `sha128[16]` + `hash_valid` | u8[16], bool | truncated SHA-256 content id (**carved files ≤ 8 MiB only** — structured files have no stored hash) |

`FileNode`: `parent()/first_child()/next_sibling()/prev_sibling()` (intrusive links),
`props()`, `origin()/deleted_state()/confidence()` + setters, `node_id()` (stable within
tree), `is_full()`, `name_utf8()` (lossy convenience). `FullNode` adds `Stats
{file_count, dir_count, total_logical, deleted_count}` + `role` (u16string:
`"Found"`, `"Deleted"`, `"Lost Files"`, `"Other Deleted Files (Carved)"`,
`"Candidate @ <LBA>"`, `"Dynamic Volume <name>"`) — stats refreshed by
`ResultTree::refresh_stats()`.

### 3.4 Scan-stage / session / health structs

`ScanConfig` / `ScanStats` / `StageWeights` — §5. `session::SessionState /
VolumeInfo / PartitionContext` — §9. `win::health::SmartReport / NvmeHealth` — §10.
`repair::RepairPlan / RepairAction / UndoLog` — §11. `lost::Candidate` — §12.

---

## 4. Device & target model

### 4.1 Identity

- **Physical disks**: numbered `0..max_disks-1` (Windows `\\.\PhysicalDriveN`).
  `win::PhysicalDisk { u32 number; wstring model; wstring serial; u64 size_bytes;
  u32 logical_sector; u32 physical_sector; bool removable; table::DiskTable table;
  bool table_ok; }` — no stable GUID; display identity = model+serial+size; the GPT
  `disk_guid[16]` lives in `DiskTable` when a GPT exists.
- **Volumes**: drive letters. `win::VolumeEntry { wstring letter_path ("\\.\C:");
  wchar_t letter; u32 disk; u64 offset_on_disk; u64 bytes; wstring fs_name; wstring
  label; bool bitlocker; }`. Correlation letter→disk via
  `win::volume_letter_to_disk(letter, &disk, &part)` and
  `win::volume_extent(letter, &disk, &start, &len)` (the latter feeds the writer's
  overlap refusal).
- **Lost partitions**: not OS objects — `lost::Candidate {start_lba, sector_count,
  CandidateFs fs, int score, vector<Evidence>, u32 fs_probe}` materialized as virtual
  `PartitionSlice`s (§12). **Dynamic (LDM) volumes**: virtual `LdmVolumeDevice`s (§3.2/§12).
- **RDA driver targets**: `\\.\RDA\<pid-token>\<disk>\<part>`, `\<token>\<disk>`,
  `\\.\RDA\<token>\Volume\<n>` (legacy grammar) or structured `OPEN_TARGET`
  (`kTargetKindVolume=1/Disk=2/DiskPart=3`). Service name default `"RDA"`;
  `win::rda_available(service, &last_error)` probes.

### 4.2 Geometry & the IStorageDevice contract

`cr::Geometry { u64 total_bytes; u32 logical_sector; u32 physical_sector; u64
sector_count; }` with `valid()`, `is_4kn()` — **byte-anchored everywhere; no fixed 512**
(fix F1). Enumeration overrides geometry-EX with the storage-access-alignment descriptor
(logical@0x10, physical@0x14 — cache-line fields at +8/+12 were the historic bug fixed by
183dfcd).

`IStorageDevice`: `u32 read_at(u64 offset, void* buffer, u32 length)` (all-or-error,
bounds-checked `ERR_RANGE_OUT_OF_BOUNDS`), `const Geometry& geometry()`, `u64 size()`.
Implementations must be safe for **concurrent** `read_at` from engine worker threads.
Wrappers (all `IStorageDevice`): `PartitionSlice` (bounded read-only window — engines
cannot read outside their partition by construction), `SliceShim` (non-owning adapter),
`CachedReader` (bounded LRU, default 1 MiB/64 KiB blocks, hit/miss counters),
`RetryingDevice` (retry policy + bisection bad-sector isolation to sector granularity;
`RetryMode::Fail` default / `Skip` zero-fills only the failing sectors and returns OK;
accumulates `ReadErrorMap`, exposes `failed_bytes()`).

`ReadErrorMap` — sorted `ReadErrorRange {u64 offset, length; u32 attempts}`; `add`,
`merge`, `total_bytes`, serialize/deserialize. Travels with scan results and sessions.

---

## 5. Orchestrator / scan API (`orchestrator/cr_scan.h`)

### 5.1 Surface

```cpp
u32  weighted_progress(const StageWeights&, u32 quick_pm, u32 deep_pm, u32 carve_pm);
class ScanController {
    ScanController(tree::ResultTree& tree, ScanConfig cfg);
    static FsFamily probe_family(IStorageDevice* slice);      // boot-sector OEM probe
    u32  run(IStorageDevice* slice, CancelToken* cancel,
             bool (*progress)(u32 overall_permille, void*), void* progress_user,
             ScanStats& stats);
    u32  save_session(session::Writer& w, const ScanStats& stats) const;
    static u32 load_session(session::Reader& r, tree::ResultTree& tree, ScanStats& stats);
    const ScanConfig& config() const;
};
```

`ScanConfig { bool run_quick=true; bool run_deep=true; bool run_carve=true;
bool whole_disk=false; FsFamily fs_hint=Unknown; StageWeights{quick=40,deep=35,carve=25};
u32 threads=0; }` — `threads`: 0 = auto (hw−1 clamp 1..8), 1 = single-threaded,
>1 explicit (impl clamps at 64 — `resolve_threads`). `whole_disk=true` enables
lost-partition discovery (CLI `--deep`; ignored with a note when target is a volume).

`ScanStats { u64 files_found, deleted_found, carved_found, lost_partitions,
bytes_scanned, elapsed_ms, cached_hits, cached_misses; ReadErrorMap errors;
vector<lost::Candidate> lost_candidates; }` — see §5.4 for what is actually filled.

### 5.2 Stage model — exactly as implemented (`cr_scan.cpp`)

1. **`run()`** wraps the caller's slice in the R1.6 pipe: `SliceShim → RetryingDevice
   (max_attempts=3, **Skip mode** — one dead spot never aborts a scan) → CachedReader
   (8 MiB, 64 KiB blocks)`. `fs_hint` auto-probed when `Unknown` (and quick or deep run).
   Stages run in order quick → deep → carve; **first non-OK return code aborts the run**
   (with `stats.elapsed_ms` stamped). At the very end: `stats.errors.merge(retry pipe)`,
   cache counters copied, `tree_.refresh_stats()` (NOT called on the error path).
2. **Quick** = ONE structured engine pass. If deep will also run, that single pass already
   uses deep settings (single MFT walk — no double walk / no FAT double emission). Roots
   pre-created: `"Found"`, `"Deleted"`, `"Lost Files"` (+ `"Other Deleted Files
   (Carved)"` when carve runs).
3. **Deep** = the extras quick did not do: (NTFS only) `scan_mft_remnants` post-format
   remnant sweep with parallel chunk fan-out; and, when `whole_disk`, `lost::discover` +
   parallel per-candidate volume scans (ThreadPool; candidate misses never abort). When
   quick was disabled, deep runs the engine pass itself with full deep semantics.
   **LDM hook**: when `fs_hint==Unknown`, `parse_ldm_database` is tried on the (whole-disk)
   slice; each non-Broken layout becomes a root `"Dynamic Volume <name>"` scanned through
   an `LdmVolumeDevice` (single-disk resolver: members on other disks read as missing;
   RAID-5 XOR-reconstructs one missing data column).
4. **Carve** = `carve::carve_slice` over the whole slice with dedup against the structured
   tree (`&tree_` passed as `dedup_tree`), namer `"Carved%06u.<ext>"`, threads =
   resolved count. `stats.carved_found = cs.validated` (grammar-confirmed only — raw-sign
   layer-1 hits that grammar rejected are counted in `CarveStats::sign_hits` but not in
   `carved_found`).

**Merge behavior**: one merged `ResultTree` per run. Carved duplicates of structured
results are dropped via first-extent+size-class dedup (F9). Lost-partition candidates
each get their own root; LDM volumes their own roots.

### 5.3 Progress callback

`bool (*progress)(u32 overall_permille, void* user)` — called on the scan thread.
`overall_permille` is 0..1000 monotonic (weighted: `sum(w_i*pm_i)/total`, single division).
**Frequency/coverage — exactly as wired**: fine-grained engine progress reaches the
callback **only for the NTFS walk** (a trampoline maps engine `(done,total)` on a 0..10000
scale into the overall permille). All other engines, the remnant sweep, lost discovery and
carving are invoked with `progress = nullptr` — the UI sees **stage-boundary ticks only**
(`weighted(1000,0,0)`, `weighted(1000,1000,0)`, `weighted(1000,1000,1000)`). GAP **G-04**.
Returning `false` cancels the NTFS walk through the trampoline; at stage boundaries the
orchestrator ignores the return (`(void)progress(...)`) — **use `CancelToken` as the
reliable cancel path**. The writer honors `false` (§8).

### 5.4 Cancellation / stats fill-rates / live visibility

- **Cancel**: pass a `CancelToken*`; engines and transports poll it at loop heads
  (`ERR_CANCELLED` returned). `ThreadPool::abort()` from any thread (e.g. a UI Stop
  button handler) also works for pool-driven stages. **No pause. No resume.**
- **`ScanStats` fill-rates (impl truth)**: `files_found`/`deleted_found` accumulate per
  engine pass and candidates; `carved_found` end of carve; `lost_partitions` end of deep
  (whole-disk only); `bytes_scanned` **carve bytes only** (GAP **G-05** — no
  throughput/ETA inputs for structured stages; only `elapsed_ms`); `errors` merged
  **only after the whole run** (GAP **G-06** — no live bad-sector map); `cached_hits/
  misses` at end; `lost_candidates` after deep.
- **Live results (answered from the code, not guesswork)**: `ResultTree` mutations and
  index lookups serialize on an internal mutex and engines emit from worker threads, but
  **`roots()`/`node_count()` are documented stable only between stages, and child-sibling
  traversal (`first_child()/next_sibling()`) is unsynchronized** — traversing while an
  engine emits is a data race. There is no snapshot/lock API for readers. GAP **G-02**:
  the UI must not enumerate the tree mid-scan; live feedback = progress callback + stats
  only.
- **Mid-scan recovery**: `SafeWriter` borrows the same `IStorageDevice*` (concurrent
  `read_at` is contract-safe) but reads node extents from the tree — which is being
  mutated. **Mid-scan recovery is NOT core-safe.** The sanctioned pipeline is
  scan → (save session) → recover from the session (or from the quiesced in-memory tree
  after `run()` returned). GAP **G-03**. Note the CLI only saves a session when the scan
  returned OK — a cancelled CLI scan saves nothing; a library-linked UI CAN call
  `save_session` after a cancelled `run()` (the tree retains everything emitted), it just
  must call `refresh_stats()` itself first.

---

## 6. Result tree / query API

### 6.1 `cr::tree::ResultTree` — the complete surface

```cpp
FullNode*  add_root(const std::u16string& role);
FileNode*  add_file(FileNode* parent, FileProperties* props);   // takes a reference
FileNode*  find_by_record(u64 record_ref) const;                // hard-link index
FileNode*  find_by_extent(u64 first_offset, u64 length_class) const; // dedup index
const std::vector<FileNode*>& roots() const;
size_t     node_count() const;      // every node ever added (light+full)
size_t     file_count() const;      // light nodes only
u64        total_logical() const;
void       refresh_stats();         // rebuild FullNode::stats — call after mutations
void       index_node(FileNode*);   // public for external index additions (thread-safe)
void       reparent(FileNode* node, FileNode* new_parent);
void       reset();                 // clear without destroying
```

Navigation: `parent/first_child/next_sibling/prev_sibling` — **no paging, no lazy
loading, no iteration cursor**; the whole forest is in memory (arena-allocated,
bulk-freed — million-node scans stay memory-disciplined, R6.3's "disk-backed
checkpoints" are the CRSF file, not an in-API database).

### 6.2 Filtering — core-side vs client-side (truth)

**The core provides NO filter, sort, or query API.** All of path/type/date/size/
confidence/extension filtering is **client-side over the tree** (the CLI itself does
substring+deleted-only filtering in its own walker — `name_matches()` in rrecov.cpp).
Counting: `node_count()/file_count()/total_logical()` + per-root `FullNode::stats`.
GAP **G-12** (only if the UI wants core-side filtering at million-node scale).

### 6.3 Raw/extra groupings & carve confidence

Standard root set from the orchestrator: `Found` (active), `Deleted` (deleted /
partially-overwritten / damaged), `Lost Files` (orphans, remnants, USN-jrnl names),
`Other Deleted Files (Carved)` (carved), `Candidate @ <LBA>` roots (lost partitions),
`Dynamic Volume <name>` roots (LDM). Carved nodes: `Origin::Carved`,
`carve_format` = `carve::FormatId` (30 grammar formats: JPG PNG TIFF CR2 WEBP HEIC PDF
ZIP DOCX 7Z SQLITE MP4 MOV AVI MKV RAR4 PST RAR5 DNG ORF RAF RW2 PSD MP3 WAV M4A ASF FLV
CAB ISO), `carve_confidence` 0..1000 = `(base_confidence + validator_confidence)/2`
capped 1000; raw-sign layer-1 hits (139 formats from the corpus `raw_signs.json`):
header-only = sign confidence, header+tail = `(hdr+tail)/2 + 100` capped 1000.
`Signature` rows: ext, 16-byte magic, header_offset, min/max size, base_confidence,
validator (F8: every grammar entry MUST validate — `validate_registry()` fails closed).

---

## 7. Preview / data access API

**What exists:**

- `FileProperties::extents` (+ `resident_data`) fully describe content placement.
- `IStorageDevice::read_at` (through any transport the UI opens) is the only content-read
  primitive — the UI *can* read extents itself when linking the library in-process.
- Compressed extents decode via `compress::decompress_block(codec, src, psize, dst,
  lsize)` (`kRaw/kLzjb/kZle/kGzip/kLz4/kZstd`; `codec_available()`; `codec_from_zfs`).
- Carve grammar validators cover raw camera formats (CR2/DNG/ORF/RAF/RW2) and archives.

**What does NOT exist (all → GAP G-10):** a bounded preview reader API, size caps,
thumbnail/EXIF/media-metadata extraction, Office text extraction, hex-preview helper,
"safe preview workers" (R6.7 defers them: "out of scope for CLI v0.1 but the node graph
carries preview metadata slots" — note the node graph has **no dedicated preview
metadata fields** beyond size/times/carve fields; that requirements sentence is
aspirational). The UI must implement preview in managed code on top of extents+read_at,
or shell out. **NEEDS-CORE-ISSUE G-10.**

---

## 8. Recovery / writer API (`recovery/cr_safewriter.h` + `win/cr_destio.h`)

```cpp
class DestIo {                       // platform IO seam (Win32 impl: win::WinDestIo)
    virtual u32 create_temp(u64 item_id, const u16string& final_path_utf16,
                            const u16string& temp_suffix_utf16) = 0;
    virtual u32 write(u64 item_id, u64 offset, const void* data, u32 len) = 0;
    virtual u32 commit(u64 item_id) = 0;      // flush + close + atomic rename
    virtual void close(u64 item_id) = 0;      // deletes uncommitted temp
    virtual bool exists(const u16string& path_utf16) = 0;
};
class SafeWriter {
    SafeWriter(DestIo& io, IStorageDevice* source, WriterConfig cfg);
    void set_destination(u64 dest_disk_start, u64 dest_disk_bytes);  // overlap window
    u32  enqueue(tree::FileNode* node, const std::u16string& dest_dir);
    u32  process(CancelToken*, bool (*progress)(u64 done, u64 total, void*),
                 void* progress_user, WriterStats& stats);
    const std::vector<QueueItem>& queue() const;
    std::u16string resolve_collision(const std::u16string& dir, const std::u16string& base) const;
    static std::u16string sanitize_name(const std::u16string& in);
};
```

- `WriterConfig { u64 read_chunk = 1 MiB; u32 rename_attempts = 64; bool verify_hash =
  true; bool skip_hashless = false; }`.
- `QueueItem { node, dest_dir, final_name, temp_suffix (".part-<id>"), item_id, state
  (Pending/Reading/Writing/Verifying/Done/Error/Refused), error, bytes_done, bytes_total,
  hash_verified }` — the per-item progress surface (poll `queue()` on the UI side).
- `WriterStats { queued, done, refused, errors, hash_mismatch, resumed }`.
- **Overlap refusal (R7.2/F12, code-level)**: `enqueue()` returns `ERR_DEST_OVERLAP`
  (item marked Refused) when any node extent intersects the destination extent given to
  `set_destination()` — extents are absolute device bytes; the CLI maps the destination
  letter with `win::volume_extent()`.
- **Collision naming**: `name` → `name (2)`, `name (3)` … inserted before the last dot,
  up to 64 attempts, then `name_dup`. Sanitizer replaces `/ * : ? " < > |` with `_`
  (never empty; `.`/`..` prefixed).
- **Write path per item**: skip if final exists (**resume = idempotent re-runs**,
  `stats.resumed`); create temp `final + ".part-<id>"`; write resident_data first, then
  extents chunked at `read_chunk` (compressed extents decoded whole-block, ≤ 256 MiB
  guard; unreadable spans **zero-filled** — Skip policy — hash intentionally mismatches);
  SHA-256 computed on the fly; **verification compares the truncated 128-bit id when
  `hash_valid`** (carved files ≤ 8 MiB; structured files hash-less → `hash_verified=
  false`, not an error unless `skip_hashless`); mismatch ⇒ content still committed,
  counted in `stats.hash_mismatch` + item `ERR_HASH_MISMATCH` (salvage-over-purity);
  `commit()` = flush + atomic rename (`MOVEFILE_REPLACE_EXISTING`); cancel mid-item ⇒
  temp deleted, `ERR_CANCELLED`.
- **Progress**: `(done_bytes, total_bytes)` after each item; `return false` ⇒ `process()`
  returns `ERR_CANCELLED` (honored, unlike the scan path).
- **Folder reconstruction / empty folders — NOT SUPPORTED**: neither `SafeWriter` nor
  `WinDestIo` creates directories; the CLI builds nested dest paths but never calls
  `CreateDirectoryW` — nested recoveries fail `ERR_DEST_UNWRITABLE` unless the destination
  tree already exists; directory nodes are never enqueued (no empty-folder preservation).
  The UI MUST pre-create directories itself (it owns the filesystem side). GAP **G-08**.
- **Report generation**: none (CLI prints a one-line summary). GAP **G-09**.

---

## 9. Session API (CRSF, `.crsf` — the CLI calls them `.crsf` files)

Container (`cr_crsf.h`, `docs/crsf-format.md`): magic `"CRSFMT01"`, version 1, 64-byte
header (record_count patched at `finalize()`), 512-byte unit granularity; records
(24-byte header `"CRSR"` + type + session_id + chain + **CRC32 of the decompressed
payload**); chunk framing u16 (bit15 deflate level-5, bits0-11 len−1, ≤ 4096; stored when
deflate doesn't shrink; reader falls back to raw payload per chunk; corrupt record ⇒
empty record + `ERR_BAD_CHECKSUM`, reader **skips forward** in 512-byte steps to the next
`"CRSR"` — resumable by construction).

Record types: `REC_SESSION_STATE=1, REC_VOLUME_INFO=2, REC_PARTITION_CONTEXT=3,
REC_RESULT_TREE=4, REC_ERROR_MAP=5`.

- **Save**: `session::Writer(write_cb, tell_cb, seek_cb, user)` → `open(session_id)` →
  `ScanController::save_session(w, stats)` (emits state, volume info, one partition
  context per lost candidate, the whole tree DFS, the error map) → `finalize()`.
  CLI session id constant: `0x52434F56` ("RCOV").
- **Load**: `session::Reader(read_cb, seek_cb, size_cb, user)` → `open(&count)` →
  `read_record(i, Record&)` → `ScanController::load_session(r, tree, stats)` restores
  the **tree + `files_found` + error map**; corrupt records skipped (F10). **Impl
  truth**: `load_session` ignores `REC_VOLUME_INFO` and `REC_PARTITION_CONTEXT`
  (`lost_candidates` are NOT restored — GAP **G-23**), does not re-create lost-partition
  roots' candidate metadata, and `stage_done_mask` is stored but never consumed.
- **Resume-a-scan**: **absent** — no API continues a partially-scanned session (load is
  display/recover-only). GAP **G-07**. Architecture.md §4's "pause/resume through session
  persistence" overstates: persistence exists, resume does not.
- **Auto-checkpointing**: **absent** — sessions are written only when the caller saves
  (CLI: only after a fully successful scan). GAP **G-13**.
- **Corrupt-file behavior**: per-record CRC + skip-forward (above); container-level
  magic/version mismatch ⇒ `ERR_SESSION_FORMAT`/`ERR_SESSION_VERSION`.
- **`.rsf` (EaseUS) import**: **absent entirely** (GAP **G-11**); the corpus `.rsf`
  reversal is used only as the format's evidence base (and by benchmark scripts).

---

## 10. Health API (`win/cr_health.h`) — report-only

```cpp
struct SmartAttribute { u8 id; u16 value; u16 worst; u8 raw[6]; bool failing; std::string name; };
struct SmartReport {
    bool available; std::string transport;      // "ata-passthrough" / "nvme" / "legacy"
    u16 ata_version, smart_version; std::vector<SmartAttribute> attributes;
    bool failing_now;                            // derived verdicts:
    u64 reallocated_sectors;                     // attr 5 raw
    u64 pending_sectors;                         // attr 197 raw
    u64 uncorrectable;                           // attr 198 raw
    u64 power_on_hours;                          // attr 9 raw
    u64 spin_retry;                              // attr 10 raw
    u64 ssd_wear_level;                          // attr 231/233 (vendor)
    u64 temperature_c;                           // attr 194/190 raw (vendor)
    bool trim_enabled, trim_known;               // fsutil query
};
struct NvmeHealth { bool available; u8 critical_warning; u16 temperature_k;
    u8 available_spare, available_spare_threshold; u64 percentage_used,
    data_units_read, data_units_written, power_cycles, power_on_hours,
    unsafe_shutdowns, media_errors; };           // NVMe log page 2
u32  read_smart(void* disk_handle, SmartReport& out);   // admin handle; ERR_HEALTH_UNAVAILABLE
u32  read_nvme_health(void* disk_handle, NvmeHealth& out);
u32  query_trim_state(bool& enabled);                  // global, via fsutil child process
bool looks_like_ssd(void* disk_handle);                // IOCTL_STORAGE_QUERY_PROPERTY heuristics
const char* smart_attr_name(u8 id);
```

The UI opens `\\.\PhysicalDriveN` itself (`GENERIC_READ|GENERIC_WRITE`, admin) and passes
the HANDLE as `void*`. `read_smart` tries ATA passthrough (0x4D004, SMART 0xB0 D0/D1/D8,
0xEC IDENTIFY) with the legacy `SMART_RCV_DRIVE_DATA 0x7C088` fallback. SSD/HDD
classification: `looks_like_ssd` (bus NVMe ⇒ SSD; HDD registry + rotation-rate-0 ⇒ SSD).
**TRIM toggle: NOT implemented** — policy recommendation only, never executed silently
(by design). Nothing in health writes to the device. USB bridges may refuse passthrough
(`ERR_HEALTH_UNAVAILABLE`) — the CLI's own guidance: pending/uncorrectable ⇒ "recover
data FIRST, image before any scan"; TRIM enabled ⇒ "disable before recovery" warning.

---

## 11. Repair API (RawFixer, `repair/cr_rawfixer.h`) — write-capable, S1–S6

```cpp
class IWriteDevice { virtual u32 read_at(u64,u32); virtual u32 write_at(u64,const void*,u32);
                      virtual u64 size(); virtual u32 sector_bytes(); };   // platform: win::open_disk_rw
u32 analyze(IStorageDevice* slice, RepairPlan& plan);        // READ-ONLY dry-run
u32 execute(IWriteDevice* dev, const RepairPlan& plan, UndoLog& undo);  // transactional
u32 undo(IWriteDevice& dev, const UndoLog& log);             // replay original bytes
```

`RepairPlan { vector<RepairAction> actions; bool refused; string refusal_reason;
string fs_family ("ntfs"/"fat"/"exfat"); bool detected; }`;
`RepairAction { ActionKind kind (RestoreBootFromBackup=1 / RepairBootChecksum=2 /
RepairGeometryField=3); u64 offset; u32 length; vector<u8> after; string reason; }`;
`UndoLog` — durable container, magic `"CRUNDO01"`, CRC32-checked, `serialize/
deserialize`, entries `{offset, length, original}`.

Consent model: **analyze() is read-only and nothing is ever written until execute() is
explicitly called with a plan**; clean volumes are refused with an empty plan (S2);
ambiguous repairs are refused (S3); every written sector is snapshotted BEFORE the write,
failed actions roll back all prior actions in reverse (S4); written structures are
re-validated, mismatch ⇒ full rollback (S5); scope = boot sectors, checksums, derived
geometry only — FAT chains / MFT records / directory content are NEVER rewritten (S6).
The undo log is populated even on success (operator may revert later). CLI: `fix` is
dry-run by default, `--apply` executes, undo log auto-saved (default
`rrecov-fix-disk<N>.undo`), `--undo-apply` replays. Write device: `win::open_disk_rw`
(RDA GENERIC_WRITE handle or direct `\\.\PhysicalDriveN`, sector-aligned staged writes).

---

## 12. Lost partition API (`lost/cr_lostpart.h`) + LDM

```cpp
enum class CandidateFs : u8 { Ntfs=0, Fat, Exfat, GptTable, MbrTable, Unknown };
struct Candidate { u64 start_lba; u64 sector_count; CandidateFs fs; int score;
                   std::vector<Evidence> evidence; u32 fs_probe; };  // fs_probe = engine rc
struct DiscoveryResult { std::vector<Candidate> candidates; /*score desc*/ u64 sectors_scanned; u64 read_errors; };
u32  discover(IStorageDevice* disk, const std::vector<table::PartitionInfo>& known,
              u32 stride_sectors, CancelToken*, bool (*progress)(u64,u64,void*), void*,
              DiscoveryResult& out);
enum class Overlap : u8 { None=0, Inside, Overlaps, Contains };
Overlap classify_overlap(u64 start, u64 count, const std::vector<table::PartitionInfo>& known);
std::unique_ptr<PartitionSlice> materialize(IStorageDevice* disk, const Candidate& c);
```

Discovery strides the whole disk for boot/table signatures, validates per-format, scores
evidence (tags like `"boot-sig"`, `"bpb-valid"`, `"mft-probe"`), dedups against the live
table and between candidates; candidates inside live regions are still reported (damaged
inner volumes) but marked. **Integration**: orchestrator deep stage (whole-disk mode
only) → live table from `parse_disk_table` → `discover` → per-candidate `materialize` →
full engine scan under a `"Candidate @ <LBA>"` root, parallel over the pool. The user's
partition table is NEVER repaired (R3.2). LDM (§3.2, `table/cr_ldm.h`): MBR 0x42 span +
GPT metadata partitions; `parse_ldm_database` returns the full VBLK graph +
`LdmVolumeLayout`s (Simple/Spanned/Striped/Mirrored/Raid5/Broken, chunk/columns, missing
members); `LdmVolumeDevice` renders each as a read-only `IStorageDevice` (mirrored:
copies tried in order; RAID-5: XOR reconstruction of one missing data column;
`LdmStats` counts bad VBLKs). Multi-disk memberships beyond the scanned disk read as
missing members — single-disk scans only (honest limitation).

---

## 13. CLI contract (`rrecov`) — parity + contract-test surface

`wmain` usage (authoritative; the file-top comment omits some flags — CLI doc drift):

```
rrecov list
rrecov scan --disk N | --volume C: [--deep] [--quick-only] [--no-carve]
            [--threads N] [--out file.crsf] [--log file] [--json] [--csv]
            [--filter s] [--deleted-only]
rrecov tree --in file.crsf [--filter s] [--deleted-only] [--csv]
rrecov recover --in file.crsf --disk N --to DIR [--deleted-only] [--filter s]
rrecov health --disk N
rrecov fix --disk N [--part P] [--apply] [--undo file] [--undo-apply file] [--no-rda]
rrecov bench --disk N [--mode seq|rand|open|read|info] [--mb N] [--block-kb N]
             [--iterations N] [--seed N] [--json] [--no-rda]
```

### Behavior details

- **list**: `enumerate_disks(200,…)` + volumes; prints `N disk(s), M volume(s)`, per disk
  `Disk %u: %llu MiB (model) serial [removable|fixed] [GPT|MBR|no-table]`, per volume
  `    %c: fs label %llu MiB [BitLocker]`. **Exit 0 even when enumeration fails** (prints
  `enumeration failed`) — GAP **G-20** for subprocess error detection.
- **scan**: `--disk N` whole-disk view (RDA first: `open_rda_partition(L"RDA",N,0)`,
  prints `[transport] RDA driver path (\\.\RDA)` / direct fallback with the CreateFile
  GLE); `--volume C:` → BitLocker check/auto-unlock → `volume_extent` → slice over the
  disk view; `--deep` ⇒ `whole_disk=true` (skipped with a note on volume targets);
  `--quick-only` ⇒ deep+carve off; `--no-carve` ⇒ carve off; `--threads N`. Progress
  line `\r  %5.1f%%  (%lld s)`; on success one summary line (`scan complete: … in %llu.%01u s`)
  or `--json` `{"files":N,"deleted":N,"carved":N,"lost_partitions":N,"bytes_scanned":N,
  "elapsed_ms":N}`; then the tree; `--out` saves the session (`session saved: <path>`).
  `--log file` writes a detailed stage log (`scan start`, `scan end rc=… files=… …
  elapsed_ms=… errors=…`, per-range `read-error range: @… +… (attempts …)`).
  **Exit 0 ok / 1 any failure (open, unknown option, scan rc≠OK, session save failure).**
- **tree**: loads `.crsf` (via `ScanController::load_session`), prints `session: N
  file(s)` + tree. Tree text format: `== <root>` then depth-indented `name (size)
  [deleted|risk] [carved|lost]`; `--csv`: `"full\path",logical_size,origin,deleted_state,
  confidence,first_extent_offset` (no header row). Exit 0/1.
- **recover**: requires `--in --disk --to`. Loads session, opens source disk, maps
  destination extent (`[F12] WARNING: destination is on the SOURCE disk %u` when same
  disk), enqueues every file node matching `--deleted-only` / `--filter` (substring in
  the utf8 name; directories recursed), prints `queued N item(s) (M refused:
  destination overlaps source)`, then progress + `recovered: %llu done, %llu refused,
  %llu error(s), %llu hash mismatch`. **Exit 0 ok / 1 setup failures / 2 when
  `errors || hash_mismatch` ≠ 0 (partial recovery still on disk).**
- **health**: opens the drive read-write (admin), prints the SMART table
  (`ID value worst raw name` rows + `reallocated=… pending=… uncorrectable=… hours=…
  temp=…C`, `WARNING: failing attribute present`, `WARNING: unstable media - recover
  data FIRST, image before any scan`), NVMe health (`temp=…C spare=…%/threshold=…%
  used=…% read=… written=… power-on=…h unsafe-shutdowns=… media-errors=…`,
  `WARNING: critical warning bits 0x%02x`), `seek-penalty (SSD heuristics): …`, `TRIM:
  enabled (WARNING: TRIM erases deleted data on SSDs - disable before recovery) /
  disabled`. Exit 0 / 1 (cannot open).
- **fix**: `--part P` slices partition P (1-based) from the parsed table, else whole-disk
  LBA0 view. Prints `REFUSED: <reason>` (+ reminder nothing is written without `--apply`),
  `clean <family> volume - nothing to repair (S2: …)`, or the plan
  (`repair plan for <family> volume: N action(s)` + `  [kind] offset=… len=… : reason`).
  Dry-run default (`dry-run only - re-run with --apply …`). `--apply`: executes,
  `execute OK: N sector(s) written + verified`, undo log saved with the restore command
  line. `--undo-apply file` replays. **Exit 0 refused/clean/dry-run/success / 1 usage /
  2 open/analyze/execute/undo failures** (`undo FAILED … - verify the disk state!`).
- **bench**: modes `info` (geometry JSON/text), `read` (LBA0 through the transport, first
  16 bytes), `open` (open/close churn, mean ns, ops/s), `seq` (MiB/s), `rand` (LCG
  offsets, mean µs, IOPS). `--json` per-mode objects (`{"mode":"seq","transport":"rda",
  "block":…,"bytes":…,"mib_per_s":…}` etc.). RDA preferred unless `--no-rda`. Exit 0 /
  1 unknown mode / 2 open or IO failure.

**CLI-parity requirement for the UI**: every wizard flow must be reproducible as one of
these command lines; when the UI links in-process it must produce byte-identical
`.crsf` sessions for the same inputs (same record order and tree grammar).

---

## 14. Gap list for the UI (each = tracked `NEEDS-CORE-ISSUE`)

Classification: **[absent]** nothing exists · **[declared-not-wired]** header/enum exists
but no code path produces it · **[win-incomplete]** Windows-layer caveat · **[by-design]**
deliberate scope, UI degrades truthfully rather than filing.

| ID | Gap | Class | UI degradation |
|---|---|---|---|
| G-01 | No stable C ABI / no exported DLL — C++20 class surface only | absent | UI owns a MSVC-compiled shim DLL, or CLI-subprocess mode |
| G-02 | No mid-scan result-tree snapshot/read lock — traversal during emission is a data race | absent | Results pane populates at stage boundaries / scan end only |
| G-03 | Mid-scan recovery unsupported (tree mutation + no API); CLI saves sessions only on scan success | absent | Recover only after scan completes; UI-shim may save-on-cancel via library |
| G-04 | Fine-grained progress only for the NTFS walk; all other engines/stages emit stage-boundary ticks only (progress passed as nullptr) | declared-not-wired | Coarse progress bars + indeterminate sub-stage spinner |
| G-05 | `ScanStats.bytes_scanned` = carve bytes only; no per-stage bytes/throughput; ETA not computable | declared-not-wired | ETA only during carve; otherwise elapsed-time only |
| G-06 | ReadErrorMap merged into stats only after the whole run — no live bad-sector map | declared-not-wired | Bad-sector list shown after scan (from session ERROR_MAP) |
| G-07 | No pause/resume; no scan-resume from session (`stage_done_mask` stored, never consumed) | absent | Stop = cancel; "resume" = rescan or load partial session (results only) |
| G-08 | No destination directory creation (DestIo has no mkdir); nested paths fail ERR_DEST_UNWRITABLE; no empty-folder preservation | absent | UI pre-creates folder tree itself; empty folders not reconstructable |
| G-09 | No recovery report generation (no report file/format) | absent | UI renders its own report from WriterStats/queue |
| G-10 | No preview/content API: no bounded extent reader, thumbnails, EXIF/media metadata, Office text extraction, hex view | absent | UI implements preview in managed code over extents + read_at |
| G-11 | No `.rsf` (EaseUS) session import — CRSF only | absent | No legacy-session loading |
| G-12 | No core-side filter/sort/paging/query APIs (all client-side) | absent | UI filters in C# over tree snapshot (fine at ≤ millions with virtualization) |
| G-13 | No auto-checkpointing — session written only on explicit save | absent | UI-shim saves periodically via `save_session` (requires quiesced tree — coordinate with G-02) |
| G-14 | Session `VolumeInfo` sparsely populated (label/geometry/offset/elapsed zero; `fs_kind` stores FsFamily not FsKindCode) | declared-not-wired | Session metadata pane limited; recover requires re-selecting the source disk |
| G-15 | TRIM toggle not implemented (report-only) | by-design | Recommendation text only |
| G-16 | SMART over USB bridges may be unavailable (ERR_HEALTH_UNAVAILABLE); admin handle required | win-incomplete | Health pane degrades to NVMe/geometry/TRIM facts |
| G-17 | Windows layer CI-verified only (no Linux runtime verification) | win-incomplete | UI contract tests must run on real Windows hosts |
| G-18 | ZFS: single-vdev pools only; multi-vdev detected+reported unsupported; BE pools refused ERR_UNSUPPORTED; gang blocks counted damaged | by-design | ZFS support statement must be scoped honestly |
| G-19 | LDM multi-disk members unresolved in single-disk scans; RAID-5 reconstruction of one missing column only; Broken volumes surfaced but not scannable | by-design | Dynamic-volume recovery scoped to present disks |
| G-20 | CLI `list` exits 0 even when enumeration fails; file-top usage comment drift (missing bench `info`, `--iterations/--seed/--no-rda`, scan `--csv/--filter/--deleted-only`) | win-incomplete | Subprocess mode parses stdout, never trusts list's exit code |
| G-21 | CLI `recover` has no `--volume` source form (extents are absolute; `--disk` mandatory) | by-design | UI always resolves volume→disk before recover |
| G-22 | Structured (non-carved) files carry no stored hash → writer verification is hash-less for them | declared-not-wired | "Verified" badge only on carved items |
| G-23 | `load_session` ignores VOLUME_INFO/PARTITION_CONTEXT; lost candidates not restored from sessions | declared-not-wired | Session reload shows the tree, not candidate metadata |

Header/doc discrepancies found (headers win; flagged for core):

- **D-1** `cr_records.h:47` VolumeInfo.fs_kind comment (factory codes, `carved 0xFF`) vs
  impl writing `FsFamily` (`cr_scan.cpp:573`) and Raw=6 (row 45) — comment is stale and
  the impl uses the *other* enum namespace than the comment implies.
- **D-2** `cr_runlist.h:3-6` header comment still teaches the inverted runlist nibble
  order (AUDIT row 2: impl corrected, comment stale).
- **D-3** `docs/architecture.md` §4 "supports pause/resume through session persistence"
  vs no resume API (G-07).
- **D-4** `requirements.md` R6.7 "node graph carries preview metadata slots" vs no such
  fields in `FileProperties` (G-10).
- **D-5** CMake `VERSION 0.1.0` vs roadmap "v0.2 shipped".
- **D-6** `rrecov.cpp` file-top comment omits bench `info` mode and several flags the
  usage string carries (CLI doc drift, G-20).
- **D-7** `ScanConfig.threads` header comment (auto clamp 1..8) vs impl also accepting
  explicit values up to 64 (`resolve_threads`).

---

## 15. Traceability appendix (function → header → lines)

| Function/type | Header | Lines |
|---|---|---|
| `err_text`, `Err` codes | `cr_error.h` | 18–63 |
| `FileTimes`, `FsKindCode` | `cr_types.h` | 45–81 |
| `ThreadPool`, `PoolCancelToken`, `parallel_for` | `cr_parallel.h` | 42–102 |
| `Geometry`, `ReadErrorRange/Map`, `RetryPolicy`, `IStorageDevice`, `CancelToken` | `storage/cr_device.h` | 31–100 |
| `PartitionSlice`, `SliceShim` | `storage/cr_slice.h` | 30–69 |
| `CachedReader` | `storage/cr_cached.h` | 20–52 |
| `RetryingDevice` | `storage/cr_retry.h` | 30–65 |
| `Origin/DeletedState/Confidence` | `tree/cr_node.h` | 28–36 |
| `FileProperties` (+`Extent`) | `tree/cr_node.h` | 45–96 |
| `FileNode` | `tree/cr_node.h` | 103–144 |
| `FullNode` (+`Stats`) | `tree/cr_node.h` | 150–164 |
| `ResultTree` | `tree/cr_node.h` | 170–241 |
| `FsFamily`, `StageWeights`, `ScanConfig`, `ScanStats`, `weighted_progress`, `ScanController` | `orchestrator/cr_scan.h` | 26–115 |
| CRSF constants, `Record`, `Writer`, `Reader`, chunk fns | `session/cr_crsf.h` | 28–93 |
| `SessionState/VolumeInfo/PartitionContext` + (de)serializers | `session/cr_records.h` | 38–79 |
| `DestIo`, `ItemState`, `QueueItem`, `WriterConfig`, `WriterStats`, `SafeWriter` | `recovery/cr_safewriter.h` | 27–112 |
| `WinDestIo` | `win/cr_destio.h` | 17–33 |
| `PhysicalDisk`, `VolumeEntry`, `enumerate_disks/volumes`, `open_disk_view` | `win/cr_enumerate.h` | 23–57 |
| `SmartAttribute/Report`, `NvmeHealth`, `read_smart`, `read_nvme_health`, `query_trim_state`, `looks_like_ssd`, `smart_attr_name` | `win/cr_health.h` | 30–90 |
| `open_rda_partition/volume`, `open_physical_drive`, `open_harddisk_volume`, `open_disk`, `open_disk_rw`, `volume_letter_to_disk`, `volume_extent`, `rda_available` | `win/cr_transport.h` | 27–62 |
| RDA wire constants, `DriverInfo`, `OpenTargetIn`, `OpenSession`, `StartBackupIn`, `GetNextBlock`, `RdaClient`, `compute_pid_token`, `control_device_path` | `win/cr_rda.h` | 38–293 |
| `is_fve_volume`, `Status`, `unlock`, `try_auto_unlock`, `status_text` | `win/cr_bitlocker.h` | 25–40 |
| `CandidateFs`, `Evidence`, `Candidate`, `DiscoveryResult`, `discover`, `Overlap`, `classify_overlap`, `materialize` | `lost/cr_lostpart.h` | 25–66 |
| `CarveStats`, `CarveFlags`, `NameCallback`, `default_namer`, `carve_slice` | `carve/cr_carve.h` | 24–60 |
| `FormatId` (30), `Signature`, `CarveResult`, `Validator`, `registry`, `validate_registry` | `carve/cr_signatures.h` | 21–92 |
| `RawSign*`, `SignIndex` | `carve/cr_rawsigns.h` | 36–96 |
| `IWriteDevice`, `ActionKind`, `RepairAction`, `RepairPlan`, `UndoLog`, `analyze`, `execute`, `undo` | `repair/cr_rawfixer.h` | 54–127 |
| `RefsInfo`, `detect_refs` | `refs/cr_refs_detect.h` | 27–36 |
| `RefsScanStats/Flags`, `RefsBoot`, `validate_boot`, `scan_refs`, `REFS_FS_KIND` | `refs/cr_refs_engine.h` | 56–96 |
| `table::FsKind`, `TableLayout`, `PartitionInfo`, `DiskTable`, `parse_disk_table`, `parse_ebr_chain`, `mbr_type_to_kind` | `table/cr_table_parse.h` | 23–81 |
| MBR/GPT wire structs, LDM GUIDs, `crc32` | `table/cr_table_wire.h` | 24–119 |
| LDM structs, `parse_ldm_database`, `LdmVolumeDevice`, `ldm_parse_guid_text`, `ldm_volume_kind_name` | `table/cr_ldm.h` | 62–319 |
| `NtfsVolume`, `VolumeParams`, `validate_boot_sector` | `ntfs/cr_bootstrap.h` | 27–98 |
| `WalkerFlags`, `WalkStats`, `WalkProgress`, `walk_mft`, `scan_mft_remnants` | `ntfs/cr_mftwalk.h` | 30–88 |
| `VolumeBitmap` | `ntfs/cr_bitmap.h` | 20–51 |
| `DataRun`, `RunList` | `ntfs/cr_runlist.h` | 22–61 |
| `fixup_validate/apply` | `ntfs/cr_fixup.h` | 24–34 |
| `AttrView`, `parse_*` | `ntfs/cr_attributes.h` | 25–98 |
| NTFS wire structs + helpers | `ntfs/cr_ntfs_wire.h` | 25–247 |
| `FatParams`, `FatTable`, `ChainResult`, `follow_chain`, `reconstruct_deleted_chain`, LFN fns, `scan_fat` | `fat/cr_fat_engine.h` | 29–141 |
| FAT wire | `fat/cr_fat_wire.h` | 23–149 |
| `ExfatParams`, checksums, `validate_boot_region`, `entry_set_checksum`, `scan_exfat` | `exfat/cr_exfat_engine.h` | 28–77 |
| exFAT wire | `exfat/cr_exfat_wire.h` | 21–160 |
| `SuperBlock`, `validate_superblock`, `scan_ext4`, `unix_to_filetime` | `ext4/cr_ext4_engine.h` | 31–97 |
| `VolumeHeader`, `validate_volume_header`, `scan_hfsplus`, `mac_to_filetime` | `hfsplus/cr_hfsplus_engine.h` | 31–76 |
| `SuperBlock`, `ChunkMap`, `validate_superblock`, `scan_btrfs`, `timespec_to_filetime` | `btrfs/cr_btrfs_engine.h` | 36–80 |
| `LabelConfig`, `scan_zfs`, `unix_to_filetime` | `zfs/cr_zfs_engine.h` | 39–75 |
| `Codec`, `decompress_block`, `codec_available`, `codec_from_zfs` | `compress/cr_decomp.h` | 25–44 |
| `Sha256`, `to_hex` | `crypto/cr_sha256.h` | 18–50 |
| CLI commands | `src/app/rrecov.cpp` | list 225–248 · scan 358–493 · tree 500–543 · recover 549–680 · health 685–766 · bench 768–965 · fix 967–1150 · wmain 1154–1190 |
| Orchestrator impl | `src/core/orchestrator/cr_scan.cpp` | probe 83–132 · run 134–184 · engine pass 200–343 · deep 345–452 · candidates 454–532 · carve 534–556 · save 558–601 · load 603–634 |
| Writer impl | `src/core/recovery/cr_safewriter.cpp` | enqueue 71–111 · item 113–291 · process 293–315 |
| Win32 dest IO | `src/win/cr_destio.cpp` | 13–78 |
| Enumeration impl | `src/win/cr_enumerate.cpp` | geometry/descriptor 14–60 |
| Contract-test fixtures | `tests/unit/test_fixtures.hpp` (`LdmFixture`, `PartSpec/CompSpec/VolSpec`, `StripeDisk`), `tests/unit/test_*` (25 files, `cr_tests` binary) | — |

*End of CORE-CONTRACT.md — generated by Integration Research Agent C (Task 2-c) from
core_recovery @ 183dfcd.*
