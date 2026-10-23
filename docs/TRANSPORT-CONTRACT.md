# TRANSPORT-CONTRACT — rda-wizard UI ⇄ rda-driver ⇄ core `cr_transport`

> Task 2-d research output. Every claim is traceable to file/line (see §13).
> Driver under contract: **rda-driver v0.2.0, commit `2b1db5f`** (tag `v0.2.0`).
> Core under contract: **core_recovery @ `183dfcd`** (`src/win/cr_transport.cpp`).

---

## 1. Overview

`rda.sys` is one WDM kernel driver (x64, Windows 7 SP1 → Windows 11, single binary) that
merges the roles of three legacy EaseUS reference drivers:

| Reference binary | Role | Where in RDA |
|---|---|---|
| `eprdtdrv.sys` (EPMNtDrv) | raw disk/partition/volume gate | open grammar + sector forwarding |
| `EUEDKEPM.sys` (EuDskAcs) | 2-segment raw gate | merged into the open grammar |
| `EUDCPEPM.sys` (EuBackup) | disk-class filter + copy-session engine | `Parameters\FilterMode` + 18-command session protocol |

Position in the rda-wizard stack:

```
UI (rda-wizard)  ──links──►  core_recovery (cr_transport.cpp = the ONLY transport impl)
                                   │
                                   ├── RDA path:  \\.\RDA\<pid-token>\<disk>\<part>  (rda.sys)
                                   └── direct path: \\.\PhysicalDriveN / \\.\HarddiskVolumeN
```

**The UI must NOT reimplement transport.** It links the core's transport through
`core_recovery/include/cr/win/cr_transport.h` + `cr_rda.h` (see §8).

Security posture (deviation F2 from the references): the control device is created with
`IoCreateDeviceSecure`, default SDDL **`D:P(A;;GA;;;BA)(A;;GA;;;SY)`** — Administrators and
SYSTEM full access only, overridable via the service's `Parameters\Sddl` (registry string).
Class GUID `{22503FD1-90D4-4322-B62D-9C8FFE186E33}` lets admins tighten security via the
device class key. The legacy PID-token handshake is kept for wire compatibility but is
explicitly **not** a security boundary; the DACL is.

**Two version numbers — do not confuse them:**

| Version | Value | Where reported |
|---|---|---|
| Repo/release version | `v0.2.0` (tag at `2b1db5f`) | git tag, release artifact name; NOT queryable at runtime |
| Wire/protocol version | `1.0` (major=1, minor=0) | `IOCTL_RDA_GET_INFO` (MajorVersion/MinorVersion) and `IOCTL_RDA_GET_VERSION` (`(1<<32)|0`) |

The wire version 1.0 is frozen in `src/shared/inc/rda_ioctl.h:106-107` and
`src/driver/rda_version.h` (`RDA_VERSION_FILE_STRING "1.0.0.0"`; INF `DriverVer
01/01/2026,1.0.0.0`). The UI displays the wire version from GET_INFO/GET_VERSION and may
show the file version of the deployed `rda.sys`/service binary for the release version.

---

## 2. Device interface

### 2.1 Names

The control device name is **derived from the service name at load time**
(`rda_control.c:27-101`): base = UPPERCASE(last component of the DriverEntry registry
path), so:

```
NT device:   \Device\<BASE>
Dos symlink: \DosDevices\<BASE>        → user-mode path \\.\<BASE>
fallback base: EPMNTDRV                (unusable registry path)
Parameters\DeviceName (REG_SZ) overrides the base (EUEDKEPM parity)
```

Canonical names in this stack:

| Service name | User-mode control path | Used by |
|---|---|---|
| `RDA` | `\\.\RDA` | **core default** (`cr_rda.h` default `L"RDA"`; `rrecov.cpp` uses `L"RDA"`) — the UI's canonical service |
| `eprdtdrv` | `\\.\EPRDTDRV` | reference Device.mo drop-in; `rdacli.c` hardcodes this |
| `rdatest` | `\\.\RDATEST` | CI benchmark alias |

Sub-device grammar (all on the same symbolic link; the part after `\\.\RDA` is the
FILE_OBJECT name classified by `RdaClassifyOpenPath`, `rda_compat.h:161-279`):

```
\\.\RDA                              bare/global handle (identity + extended IOCTLs, sessions)
\\.\RDA\<PID-TOKEN>\<disk>\<part>    EPMNtDrv 3-segment disk branch   (RDA_TARGET_KIND_DISK_PART)
\\.\RDA\<PID-TOKEN>\<disk>           EUEDKEPM 2-segment whole-disk    (RDA_TARGET_KIND_DISK)
\\.\RDA\<PID-TOKEN>\Volume\<n>       volume branch                    (RDA_TARGET_KIND_VOLUME)
```

PID token (`rda_compat.h:49-57`, identical transform on both sides):

```
b0..b3 = LE bytes of the 32-bit PID
token  = b0 | (b0+b1)<<8 | (b1+b2)<<16 | (b2+b3)<<24   (8-bit wrapping adds)
```

The driver re-derives it at IRP_MJ_CREATE from `PsGetCurrentProcessId()`; mismatch fails
the open with `STATUS_INVALID_PARAMETER` (`rda_create.c:71-77`). A 1-wchar name or a
malformed path also REJECTs; only the EMPTY name (Length < 2 bytes) completes with
STATUS_SUCCESS and no target ("global mode").

### 2.2 Presence detection

There is no dedicated "ping" — presence == the control-device open succeeding:

```cpp
// cr_transport.cpp:576-588  (exposed as win::rda_available)
HANDLE h = CreateFileW(L"\\\\.\\RDA", GENERIC_READ,
                       FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr,
                       OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, nullptr);
// INVALID_HANDLE_VALUE → absent; *last_error receives GetLastError()
```

Interpretation of `GetLastError()` on failure (CreateFile on a DosDevices symlink):

| GLE | Meaning |
|---|---|
| `ERROR_FILE_NOT_FOUND` (2) | no `\DosDevices\RDA` → service not installed **or** installed-but-not-started (symlink is created at DriverEntry and deleted at unload) |
| `ERROR_ACCESS_DENIED` (5) | device EXISTS and is loaded, but the caller is blocked by the SDDL (non-admin) — driver present, elevation required |
| `ERROR_PATH_NOT_FOUND` (3) | malformed path (not expected for the bare path) |

`rda_available()` returns the GLE precisely so callers can distinguish these (the header
comment documents it as "the diagnostic for the runner mystery"). Distinguishing
"installed-not-started" from "not installed" additionally requires an SCM query
(`OpenSCManager`/`OpenService`/`QueryServiceStatus`) — **the core does not do this today**
(no helper exists); the UI must add it itself (see §9, §12).

### 2.3 Version reporting

Both queries run on a bare/global handle (rdacli `info`, `rdacli.c:73-99`):

- `IOCTL_RDA_GET_INFO` (0x60002440) → `RDA_DRIVER_INFO` (24 bytes):
  `StructSize=24, MajorVersion=1, MinorVersion=0, Flags, MaxDisks (default 200),
  IoctlBase (default 0; WinPE profile sets 0x10000)`. Flags:
  `RDA_INFO_FLAG_FILTER_MODE 0x1` (PnP filter role active), `RDA_INFO_FLAG_SESSIONS 0x2`
  (copy-session engine served; default on).
- `IOCTL_RDA_GET_VERSION` (0x60002448) → `ULONG64` = `(Major<<32)|Minor` = `0x0000000100000000`.

Session-protocol side: session code `0x800C2AC8` (GET_VERSION) answers proto version
`0x00010100` + a ≤0x2E-byte product string `"RDA Disk Copy Driver (c) RDA contributors"`.

### 2.4 Access rules

- Default SDDL grants Administrators + SYSTEM only → a standard-user process cannot even
  open `\\.\RDA` (CreateFile fails, GLE 5). All transport operations therefore require an
  elevated process. The direct fallback path (`\\.\PhysicalDriveN` raw) also requires
  elevation — so the wizard effectively always runs elevated.
- Admins can replace the DACL via `Parameters\Sddl` (e.g. to widen access); the class GUID
  allows class-key overrides.
- Identity IOCTLs (GetProcName/GetFileObjName) disclose privileged info and are reachable
  only through the (admin-only) device — fix F3.
- When blocked by policy: opens fail with ACCESS_DENIED; the UI must present "run as
  administrator" guidance, not a driver-missing state.

---

## 3. IOCTL contract

### 3.1 Code construction

Extended RDA IOCTLs (`rda_ioctl.h:72-76`):

```c
#define CTL_CODE_RDA(Func) ((RDA_DEVICE_TYPE << 16) | ((Func) << 2) | 0 /* METHOD_BUFFERED */)
/* RDA_DEVICE_TYPE = 0x600; no access bits => FILE_ANY_ACCESS */
IOCTL_RDA_GET_INFO     = CTL_CODE_RDA(0x910)  /* 0x60002440 */
IOCTL_RDA_OPEN_TARGET  = CTL_CODE_RDA(0x911)  /* 0x60002444 */
IOCTL_RDA_GET_VERSION  = CTL_CODE_RDA(0x912)  /* 0x60002448 */
```

Reference identity IOCTLs are frozen as raw constants (`rda_ioctl.h:60-62`):
`0x60002400` GET_PROC_NAME (fn 0x900), `0x60002404` GET_FILE_OBJ_NAME (fn 0x901),
`0x60002408` GET_DISK_CONTEXT_MODE (fn 0x902) — all METHOD_BUFFERED, FILE_ANY_ACCESS.

Session codes are **raw EUDCPEPM wire constants** (`0x800C2AC0`…`0x800C2B42`), not
CTL_CODE-built. The driver matches the full code after subtracting the registry
`IoctlBase` (`rda_ioctl.c:38-72`); clients must send `constant + IoctlBase` where
`IoctlBase` is learned from GET_INFO (the core does exactly this — `cr_rda.h:103-104`,
`cr_transport.cpp:407` etc.). Low 2 bits: direct-family codes (`B0A, B12, B2A, B2E, B3E,
B42`) are METHOD_OUT_DIRECT (bit pattern …A/…E); all others are METHOD_BUFFERED.

### 3.2 Dispatch order (kernel side, `rda_ioctl.c:247-286`)

1. If `SessionsEnabled != 0` and `(code - IoctlBase)` is one of the 18 session codes →
   session engine (`rda_session.c:1365`). (If `SessionsEnabled == 0`, session codes are
   not intercepted and fall through to the rules below.)
2. Target-bound handle (`FsContext2` non-NULL): `0x60002408` → GetDiskContextMode;
   **everything else → pass-through** to the target's top-of-stack (honoring transfer
   method: buffered = SystemBuffer in/out; direct = in SystemBuffer / out UserBuffer;
   METHOD_NEITHER → `STATUS_NOT_SUPPORTED`).
3. Global handle (bare open): `0x60002400`, `0x60002404`, `0x60002440`, `0x60002444`,
   `0x60002448` as below; anything else → `STATUS_INVALID_PARAMETER`.

### 3.3 IOCTL table (control device)

| Code | Name | Dir | In | Out | Semantics |
|---|---|---|---|---|---|
| `0x60002400` | GET_PROC_NAME | BW | u32 PID, exactly 4 bytes | ≥ 0x208 bytes: image path WCHAR + NUL | `PsLookupProcessByProcessId` + `SeLocateProcessImageName`; Information = path bytes incl. NUL (len+2); wrong lengths → `STATUS_INVALID_PARAMETER` (`rda_ident.c:46-82`) |
| `0x60002404` | GET_FILE_OBJ_NAME | BW | u32 HANDLE, exactly 4 bytes | ≥ 0x208 bytes: full NT path | `ObReferenceObjectByHandle`→`ObQueryNameString(DeviceObject)` + FileName append; Information = bytes incl. NUL (`rda_ident.c:84-161`) |
| `0x60002440` | GET_INFO | BW | NULL, 0 | `RDA_DRIVER_INFO` (24) | out < 24 → `STATUS_BUFFER_TOO_SMALL`; Information = 24 (`rda_ioctl.c:75-98`) |
| `0x60002444` | OPEN_TARGET | BW | `RDA_OPEN_TARGET_IN` (24) | none (out must be 0) | explicit structured open, no PID token; binds the handle (`rda_ioctl.c:115-184`) |
| `0x60002448` | GET_VERSION | BW | NULL, 0 | u64 (24 → 8) | out < 8 → `STATUS_BUFFER_TOO_SMALL`; `(Major<<32)|Minor`; Information = 8 (`rda_ioctl.c:100-110`) |
| `0x60002408` | GET_DISK_CONTEXT_MODE | BW | NULL | exactly 4 bytes (LONG) | target-bound handles only; 0 = target layer driven by `\Driver\Disk`, 1 otherwise; Information = 4 (`rda_ident.c:163-174`) |
| other, bound handle | pass-through | per-method | per-method | per-method | forwarded to target top-of-stack; Information = forwarded iosb.Information (`rda_ioctl.c:186-245`) |
| other, global handle | — | — | — | — | `STATUS_INVALID_PARAMETER` |

READ/WRITE are **not** IOCTLs: plain `ReadFile`/`WriteFile` on grammar or OPEN_TARGET
handles (§4).

### 3.4 Struct layouts (wire-exact; compile-time frozen by C_ASSERTs)

**`RDA_DRIVER_INFO`** (`rda_ioctl.h:79-86`) — natural alignment, 24 bytes:

| Offset | Type | Field |
|---|---|---|
| 0x00 | UINT32 | StructSize (= 24) |
| 0x04 | UINT32 | MajorVersion (1) |
| 0x08 | UINT32 | MinorVersion (0) |
| 0x0C | UINT32 | Flags (`RDA_INFO_FLAG_FILTER_MODE` 0x1, `RDA_INFO_FLAG_SESSIONS` 0x2) |
| 0x10 | UINT32 | MaxDisks (default 200) |
| 0x14 | UINT32 | IoctlBase (default 0; PE profile 0x10000) |

**`RDA_OPEN_TARGET_IN`** (`rda_ioctl.h:92-99`) — natural alignment, 24 bytes:

| Offset | Type | Field |
|---|---|---|
| 0x00 | UINT32 | StructSize (= 24) |
| 0x04 | UINT32 | Kind: VOLUME=1, DISK=2, DISK_PART=3 |
| 0x08 | UINT32 | DiskNumber (Kinds DISK, DISK_PART) |
| 0x0C | UINT32 | PartNumber (Kind DISK_PART; DR-fallback selector) |
| 0x10 | UINT32 | VolumeNumber (Kind VOLUME) |
| 0x14 | UINT32 | Reserved |

Validation (`rda_ioctl.c:115-184`): in ≥ 24, out == 0, StructSize ≥ 24, handle not already
bound (else `STATUS_INVALID_DEVICE_STATE`); DISK → DiskNumber < 100 (EUEDKEPM limit);
DISK_PART → DiskNumber < MaxDisks (200); VOLUME → must be issued on a global (bare) handle.
Resolution: VOLUME → `\Device\HarddiskVolume{n}` top-of-stack; DISK → `\Device\Harddisk{d}\Partition0`
walk-to-`\Driver\Disk` (EUEDKEPM); DISK_PART → `\Device\Harddisk{d}\Partition0` with
`\Device\Harddisk{d}\DR{d}` fallback (same disk number twice — G4), stack walk-down to the
first `\Device\Harddisk`-named layer (`rda_target.c`). No PID token required — "the device
DACL is the security boundary".

**`RDA_OPEN_SESSION`** (0x840 bytes, `#pragma pack(4)`; `rda_ioctl.h:309-342`) — overlaid
in/out buffer, reference-exact offsets:

| Offset | Type | Field | In/Out |
|---|---|---|---|
| 0x000 | UINT32 | DiskId | in |
| 0x004 | UINT64 | DataFirstSec (copy range start LBA) | in |
| 0x00C | UINT64 | TotalSecs (≠0 required) | in |
| 0x014 | UINT64 | ExtentParam1 (block-map builder) | in |
| 0x01C | UINT64 | ExtentParam2 (block-map builder) | in |
| 0x024 | UINT64 | BitmapSize (≠0 required; capped to 0x800) | in |
| 0x02C | UINT32 | BitmapCount (≠0 required) | in |
| 0x030 | UINT32 | SpbInOut — in: 0 = auto (TotalSecs<0x20→1 else 0x10, tail=TotalSecs&0xF); **out: effective spb** | in/out |
| 0x034 | UINT8[0x800] | BlockBitmap (bit per copy block; NUL pad @0x7FE) | in |
| 0x834 | UINT64 | SessionHandle (opaque ID; **not** a kernel pointer) | out |
| 0x83C | UINT32 | SectorSize (probed) | out |

**`RDA_STARTBACKUP_IN`** (0x28 bytes, pack(4); `rda_ioctl.h:261-278`):

| Offset | Type | Field |
|---|---|---|
| 0x00 | UINT64 | Handle (session) |
| 0x08 | PVOID | PumpBuffer (user VA; driver MDL-locks it) |
| 0x10 | UINT32 | PumpBytes (must be spb*sectorSize-aligned) |
| 0x14 | PVOID | UserBitmap (optional exclusion bitmap; 4-mod-8 unaligned on wire) |
| 0x1C | UINT32 | StartSec |
| 0x20 | UINT32 | BlockCount |
| 0x24 | UINT32 | Spb (must equal session spb when a bitmap is supplied) |

**`RDA_GETNEXTBLOCK`** (0x1E bytes, pack(1); `rda_ioctl.h:234-251`) — overlaid in/out:

| Offset | Type | Field | In/Out |
|---|---|---|---|
| 0x00 | UINT64 | Handle | in |
| 0x08 | UINT64 | CursorSec (LBA cursor) | in |
| 0x10 | UINT32 | MaxSec (max sectors this round) | in |
| 0x14 | UINT32 | DiskId (source disk) | out |
| 0x18 | UINT32 | Progress (copy %) | out |
| 0x1C | UINT8 | MapProgress (block-map build %) | out |
| 0x1D | UINT8 | MapOk | out |

**`RDA_FILEIO_IN`** (0x1C bytes, pack(4); `rda_ioctl.h:284-295`): `+0x00 HANDLE
FileHandle, +0x08 PVOID Buffer, +0x10 UINT64 ByteOffset, +0x18 UINT32 Length` (MODE3 read /
MODE4 write as paging IO with the caller's file object).

### 3.5 Status codes to expect

| Status codes | Gates: `STATUS_INVALID_PARAMETER` (0xC000000D, malformed grammar/token/wrong IOCTL class), `STATUS_BUFFER_TOO_SMALL` (0xC0000023, length gates), `STATUS_INVALID_DEVICE_STATE` (0xC0000184, OPEN_TARGET on bound handle), `STATUS_INVALID_DEVICE_REQUEST` (0xC000000C, OPEN_SESSION zero fields), `STATUS_SHARING_VIOLATION` (0xC0000043, session overlap), `STATUS_INVALID_HANDLE` (0xC0000008, CLOSE/GET_PROGRESS handle) / `STATUS_INVALID_PARAMETER` (other session cmds), `STATUS_INSUFFICIENT_RESOURCES` (0xC0000017), `STATUS_NO_MEMORY` (pump-queue exhaustion), `STATUS_NOT_SUPPORTED` (0xC00000BB, METHOD_NEITHER pass-through), `STATUS_CANCELLED` (0xC0000120, cancelled IO + drained pump). Forwarded storage-stack NTSTATUSes surface verbatim on reads/writes/pass-through (e.g. CRC errors on bad media — client maps via GetLastError). |

---

## 4. `Information÷512` semantics — the legacy contract (F11)

**Kernel side** (`rda_readwrite.c:185-278`, dispatch 280-367): READ/WRITE on a bound handle
forward exactly `Length` BYTES (buffer and `Parameters.Read/Write.Length` are bytes;
`ByteOffset` is the absolute target byte offset; negative offsets →
`STATUS_INVALID_PARAMETER`). On success:

```c
*Information = (ULONG_PTR)Length * RDA_LEGACY_INFORMATION_SCALE;   /* Length * 512 */
```

i.e. **IRP Information = the REQUESTED byte length × 512** (not the target-reported
count), and `Information = 0` on any failure. This is byte-for-byte the reference
convention (ground-truth CORRECTION-2: "read length is expressed in sectors of 512 bytes"
was the WRONG prior artifact reading — the length is BYTES; only the *reported count* is
scaled).

**Client evidence:**

- `rdacli.c:192-200` (CmdRead): after `ReadFile`, checks
  `got != len * RDA_LEGACY_INFORMATION_SCALE` and prints a NOTE on mismatch — the client
  *asserts* the scale; the true byte count is the requested `len` (buffer holds exactly
  `len` bytes on success).
- **Commit `c692ca6`** (`fix(bench): honor the legacy Information contract (Length<<9) in
  rdabench`, 2026-10-22): the benchmark harness originally compared the raw
  `lpNumberOfBytesRead`; every data mode reported "first read failed" on BOTH the original
  `eprdtdrv.sys` and `rda.sys` — proving the harness, not the drivers, was wrong. Fix:
  "unscale lpNumberOfBytesRead by 512 everywhere; explicit SetFilePointerEx seeks; the
  info probe now ASSERTS the scaling (512 requested → 512*512 reported = contract proof)".
- **Commit `2b1db5f`** (HEAD, tag v0.2.0) keeps the contract unchanged; worklog Session 2
  records "Information scaling must be divided by 512" as a live lesson from round-1 data.

**Core side** (`cr_transport.cpp`, F11): `RdaTargetDevice::read_at` (lines 172-198) and
`RwDevice::read_at`/`write_at` (lines 641-708):

```cpp
if (got == len * rda::kLegacyInformationScale) return OK;   /* legacy scaled count */
if (got == len)                     return OK;   /* future true-count contract */
return ERR_READ_FAILED;
```

i.e. the core accepts **either** the scaled count (`len*512`) **or** a true count (`len`)
as success and treats everything else as failure; it never uses `got` as a byte count for
data purposes.

**THE RULE FOR THE UI:** the UI never sees raw RDA handles (it goes through
`IStorageDevice`), but if it ever instruments raw handles: *never trust
lpNumberOfBytesRead as the byte count; the authoritative count is the requested length;
accept `got == len*512` (legacy) or `got == len` (future) as success; divide by 512 only
to compare.* Session IOCTLs do NOT use this scale (their Information values are constants:
GETNEXTBLOCK = 0x1E, DIRECT_* = outLength, others = 0).

---

## 5. Session engine IOCTLs (EUDCPEPM copy-session protocol)

Codes below are the normalized constants; the wire code is `constant + IoctlBase`.
All on the control device (any handle class) while `SessionsEnabled` (default 1).
Gates and semantics from `rda_session.c` dispatch (1365-1508) + handlers:

| Normalized code | Name | Gates (in/out) | Semantics |
|---|---|---|---|
| 0x800C2AC0 | DISCOVERY | in ≥ 4 | **filter stacks only**; on the control device completes SUCCESS with nothing (1413-1419) |
| 0x800C2AC8 | GET_VERSION | in ≥ 0x84 | writes `0x00010100` + product string; Information = 0 |
| 0x800C2AD0 | OPEN_SESSION | in ≥ 0x840 | creates session (see below); Information = 0 |
| 0x800C2AD8 | SESSION_OP (flush) | in ≥ 8 | flushes the protected file's volume (path = WCHAR string at bitmap head; IRP_MJ_FLUSH_BUFFERS ≤ 20 retries); always returns SUCCESS (reference maps failures to 0) |
| 0x800C2AE0 | SESSION_OP2 (StartBackup) | in ≥ 0x28 | requires state 3 (standby); MDL-locks pump buffer; validates spb match + pump alignment; merges optional user exclusion bitmap; state → 4 (copying); kicks worker |
| 0x800C2AE8 | GETNEXTBLOCK | in ≥ 0x1E, out ≥ 0x1E | async pump: IRP queued (cap 2000 → `STATUS_NO_MEMORY`); worker copies `min(MaxSec, pump capacity, plan remaining)` sectors into the REGISTERED pump buffer, completes the IRP with the 0x1E metadata record; **Information = 0x1E constant**; queued IRPs are cancel-safe |
| 0x800C2AF0 | SESSION_STATUS | in ≥ 8 | returns the block-map status AS the NTSTATUS |
| 0x800C2AF8 | GET_PROGRESS (FINALIZE) | in ≥ 0x0C | state → 5 (done); frees plan + pump MDL + COW; drains queued pumps with `0xC0000120`; percent u32 at out+8; Information = 0 |
| 0x800C2B00 | CLOSE_SESSION | in ≥ 8 | destroys session; Information = 0; invalid handle → `STATUS_INVALID_HANDLE` |
| 0x800C2B0A / 0x800C2B12 | DIRECT_READ / DIRECT_WRITE | out > 0x10 | METHOD_OUT_DIRECT; MDL header `{u32 diskId, u32 sectorCount, u64 startLBA}` + data in the output buffer; LBA×probed sector size; **Information = outLength always**; session-FREE |
| 0x800C2B18 / 0x800C2B20 | MODE4 / MODE3 | in ≥ 0x1C, out ≥ 4 | raw paging WRITE / READ by file handle (`RDA_FILEIO_IN`); Information = 8 |
| 0x800C2B2A / 0x800C2B2E | SIZED_XFER / SIZED_XFER2 | out > 0x10 | direct read / write with per-disk sector-size cache |
| 0x800C2B38 | CANCEL | in ≥ 8 | state := 3 (standby); Information = 0 |
| 0x800C2B3E / B40 / B42 | RRWR_WRITE / _VERSION / _READ | — | filter-only ("RRWR" magic 0x52455752; 0x604 @+8 for version); control device: SUCCESS no-op |
| unknown code | — | — | reference fall-through: completes SUCCESS on the control device |

**Session identity & lifetime:**

- Handle = `(slotIndex << 16) | generation` over a 64-slot table (`rda_session.c:145-167`)
  — opaque to clients; generation guards stale handles.
- OPEN_SESSION claims `{DiskId, DataFirstSec..+TotalSecs}` exclusively; an overlapping
  active session on the same disk fails with `STATUS_SHARING_VIOLATION`.
- Sessions are **owned by the creating process** (referenced `PEPROCESS`).
- **Crash cleanup:** the control device's IRP_MJ_CLEANUP runs
  `RdaSessionSweepByProcess(PsGetCurrentProcess())` (`rda_create.c:159-162`) — a dying
  client cannot leak pump workers or MDL-locked pump buffers. Filter REMOVE destroys
  sessions bound to a removed disk. DriverUnload tears the whole engine down.
- State machine: 0 created → 1 flushing → 3 standby → 4 copying → 5 done; CANCEL forces 3
  from any state; StartBackup requires 3; worker kick budget 301 (`0x12D`).
- The core uses sessions only for imaging/verification
  (`RdaClient::session_open/start_backup/get_next_block/finalize/cancel/close`,
  `cr_transport.cpp:398-507`); `session_get_next_block` treats a failed pump IOCTL
  (0xC0000120 drain signal) as `stream_done=true` and estimates the granted-sector count
  client-side (clamped to the plan bounds — audit row 41a).

---

## 6. Retry & error-map surfaces

**There is no retry inside the driver for plain handle IO** — a failing READ/WRITE returns
the forwarded storage-stack NTSTATUS immediately (Information = 0). Cancellation is
supported (fix F4): `CancelIoEx` from user mode propagates into the storage stack; the
dispatcher completes the caller IRP with `STATUS_CANCELLED`.

**The retry policy lives in the core** (`cr_retry.cpp`, policy in `cr_device.h:68-77`):

- `RetryPolicy { u32 max_attempts = 3; RetryMode mode = Fail|Skip; }` — defaults 3 attempts,
  Fail; the scan orchestration sets **Skip** ("surface + zero-fill, never abort",
  `cr_scan.cpp:141-151`).
- Only `ERR_READ_FAILED`/`ERR_RETRY_EXHAUSTED` are retried — deterministic errors
  (out-of-bounds, bad parameter) fail immediately (`try_read`, 40-52).
- Persistent failures are **bisected** down to sector granularity
  (`read_isolated`, 62-99): halve while a half ≥ 1 logical sector; depth budget 24.
  Unbisectable bad ranges land in the `ReadErrorMap` (`{offset,length,attempts}` runs),
  surfaced into scan stats (`cr_scan.cpp:176-178`) — this is the bad-sector map the UI
  displays; it is a **client-side construct**, not a driver API (§12).
- Transport error surface (`cr_error.h`): `ERR_OPEN_FAILED 0x50020001`,
  `ERR_READ_FAILED 0x50020002`, `ERR_WRITE_FAILED 0x50020003`,
  `ERR_RANGE_OUT_OF_BOUNDS 0x50020004`, `ERR_CANCELLED 0x50020005`,
  `ERR_RETRY_EXHAUSTED 0x50020006`. Driver NTSTATUS → these codes happens inside
  `RdaTargetDevice`/`RwDevice` (ReadFile/WriteFile outcome → OK/ERR_READ_FAILED etc.).
- Timeout-ish bounds: the driver's session pump uses a 10000 ms guard
  (`RDA_SESSION_PUMP_GUARD_MS`); plain reads have no driver timeout — the UI should rely
  on CancelIoEx for hung media.

---

## 7. Reference client walk-through (`rdacli.c`, 443 lines)

`rdacli` is the executable contract harness (run elevated, driver installed). Subcommands:

1. **`info`** (73-99): `OpenGlobal(GENERIC_READ)` = `CreateFileW(L"\\\\.\\EPRDTDRV", …)`
   (42-47) → `DeviceIoControl(IOCTL_RDA_GET_INFO, NULL, 0, &info, 24, &bytes)` and checks
   `bytes >= 24` → `DeviceIoControl(IOCTL_RDA_GET_VERSION, NULL, 0, &u64, 8)` →
   CloseHandle. Prints flags/maxDisks/ioctlBase/version.
2. **`grammar <disk>`** (103-167): opens `\\.\EPRDTDRV\<token>\<disk>\0` (3-segment),
   queries `IOCTL_RDA_GET_DISK_CONTEXT_MODE` (out exactly 4 bytes); opens
   `\\.\EPRDTDRV\<token>\Volume\0` (failure tolerated when volume absent); opens
   `\\.\EPRDTDRV\<token>\<disk>` (2-segment); negative test: a corrupted token
   (`token ^ 0x5A5A`) MUST fail to open.
3. **`read <disk> <off> <len>`** (171-210): open 3-segment handle →
   `SetFilePointer(FILE_BEGIN, off)` → `ReadFile(h, buf, len, &got)` → asserts the F11
   scale (`got == len*512`, NOTE otherwise) → dump first 16 bytes.
4. **`ident <pid>`** (214-229): global handle → `IOCTL_RDA_GET_PROC_NAME` with a 0x208-byte
   out buffer.
5. **`session <disk> <start> <secs>`** (233-328): global RW handle → fill
   `RDA_OPEN_SESSION` (BitmapSize 0x800, BitmapCount 0x100, SpbInOut 0, bitmap 0xFF×0x100)
   → `DeviceIoControl(RDA_SESSION_CODE_OPEN_SESSION + 0 /*IoctlBase*/, in, 0x840, in,
   0x840)` → read back handle@+0x834, spb@+0x30, sectorSize@+0x83C → up to 8 ×
   `GETNEXTBLOCK` pump rounds (0x1E in, 64 KiB out buffer) → `GET_PROGRESS` (percent @+8)
   → `CLOSE_SESSION` (8-byte handle in, no out).
6. **`bench read <disk> <secs> [chunkSectors]`** (339-401): RDA path throughput vs a
   `\\.\PhysicalDriveN` baseline, chunk = sectors×512.

Exact call sequence for the UI's mental model:
**CreateFile(bare) → GET_INFO → GET_VERSION → [OPEN_TARGET or grammar CreateFile] →
SetFilePointer/ReadFile/WriteFile (÷512 rule) → [session ops on the bare handle] →
CloseHandle.**

---

## 8. Core's `cr_transport` mapping — and how the UI obtains transport

`core_recovery/src/win/cr_transport.cpp` (807 lines) is THE transport implementation:

- **`rda::RdaClient`** (declared `cr_rda.h:224-290`): one bare control handle
  (`\\.\{service}`, default `RDA`) opened `GENERIC_READ|GENERIC_WRITE` (falls back to
  read-only if RW open fails); all control IOCTLs serialized on one CRITICAL_SECTION;
  caches `DriverInfo` + `IoctlBase` from GET_INFO at open time (248-273). Methods:
  `query_info`, `query_version`, `open_target` (structured OPEN_TARGET on a FRESH bare
  handle so the control handle stays global-mode, 309-343), `open_disk_grammar` /
  `open_volume_grammar` (legacy PID-token paths, 375-385), `get_proc_name`, and the full
  session family (398-507).
- **`RdaTargetDevice` / `DirectDevice` / `RwDevice`**: `IStorageDevice` /
  `cr::repair::IWriteDevice` wrappers. RDA variants apply the F11 unscale (§4). The write
  path stages through a sector-aligned buffer ONLY on direct handles (RDA MDL-locks any
  alignment) (671-690).
- **Free functions** (`cr_transport.h`): `open_rda_partition` / `open_rda_volume`
  (grammar opens), `open_physical_drive` / `open_harddisk_volume` (direct),
  `open_disk(disk, prefer_rda, service, err)` (RDA-first + direct fallback, 616-626),
  `open_disk_rw` (write-capable, 721-759), `rda_available`, `volume_letter_to_disk`,
  `volume_extent`.
- Geometry probing (`probe_geometry`, 48-97) uses IOCTL_DISK_GET_LENGTH_INFO /
  GET_DRIVE_GEOMETRY_EX / STORAGE_QUERY_PROPERTY (access-alignment descriptor at +0x10
  logical / +0x14 physical — the cache-line fields at +8/+0xC were a real VHD bug) —
  these go through the RDA handle's **pass-through** path, so geometry works on both
  transports.

**How the UI obtains transport (binding decision):**

1. **Recommended (and required by the wizard prompt): link the core's transport.** Consume
   `core_recovery/include/cr/win/cr_transport.h` + `cr_rda.h`; build `cr_transport.cpp`
   (and `cr_enumerate.cpp`, `cr_destio.cpp`) into the UI process or link the core library.
   The UI then: (a) calls `win::rda_available(L"RDA", &gle)` for status; (b) opens devices
   via `open_disk` / `open_disk_view` / `RdaClient`; (c) never touches raw handles, IOCTL
   codes, or the ÷512 rule directly.
2. **Reimplementing transport is forbidden** for the UI (prompt rule) — every subtlety
   (F11 unscale, IoctlBase normalization, session cursor math, aligned write staging,
   geometry descriptor offsets) is already encoded and regression-tested in the core.
3. If the UI must display session/protocol internals, it reads them out of `RdaClient`'s
   public surface (`info()`, `ioctl_base()`, `session_*`) — not by issuing IOCTLs itself.

---

## 9. Driver presence/version detection for the UI

**Startup + preflight sequence (mirrors `rrecov.cpp:130-157`):**

```
1. CreateFile("\\\\..\RDA", GENERIC_READ, …)          → rda_available(L"RDA", &gle)
2. If opened:  GET_INFO  → wire version, Flags, MaxDisks, IoctlBase
               GET_VERSION → (Major<<32)|Minor
               (optional) grammar open + GET_DISK_CONTEXT_MODE as a liveness probe
3. If not opened: gle tells you which state (see §2.2); optionally SCM query to
   distinguish "installed, stopped" from "not installed"
4. Transport choice per scan: open_disk_view() — RDA grammar open when available,
   else direct \\.\PhysicalDriveN
```

**Version comparison rules (from code):**

- Wire version from GET_VERSION: expect `(1 << 32) | 0`. GET_INFO: `MajorVersion==1 &&
  MinorVersion>=0` (minor is currently the only moveable part; a future minor bump is
  compatible, a major bump is not).
- Forward compatibility: `DriverInfo.struct_size == 0 || > sizeof(DriverInfo)*4` → reject
  as "driver too old / IOCTL unsupported" (`cr_transport.cpp:289-298`).
- GET_INFO failure on a successfully-opened device = wrong/ancient driver under that
  service name (the extended IOCTLs postdate the reference binaries) → treat as
  "wrong driver".
- Session users must additionally require `Flags & RDA_INFO_FLAG_SESSIONS`.
- The release version (v0.2.0) is NOT queryable through the wire (§12 NEEDS-ISSUE #1).

**UI states to render:**

| State | Detection | UI message |
|---|---|---|
| not installed | CreateFile → GLE 2 (FILE_NOT_FOUND), SCM says no service | "RDA driver not installed — direct-access mode" |
| installed-not-loaded | CreateFile → GLE 2, SCM says service exists, status ≠ RUNNING | "RDA driver installed but not started — direct-access mode" (+ offer elevation to start, §10) |
| loaded, blocked (policy/non-admin) | CreateFile → GLE 5 (ACCESS_DENIED) | "Driver present — administrator rights required" |
| wrong version / wrong driver | open OK, GET_INFO/GET_VERSION fail or struct_size invalid or major ≠ 1 | "Incompatible RDA driver (wire vX.Y) — expected 1.0" |
| loaded-ok | open + GET_INFO + GET_VERSION all succeed | show wire 1.0, flags (sessions/filter), MaxDisks, IoctlBase; transport = "RDA driver path (\\\\.\\RDA)" |

**Driver-absent behavior — THE VERDICT (code-verified): the core HAS a direct-access
fallback and uses it TODAY.** Evidence, exactly as implemented:

- `rrecov.cpp:130-157 open_disk_view()`: probes `win::rda_available(L"RDA", &rda_gle)`;
  when unavailable prints `"[transport] RDA not available (\\\\.\\RDA CreateFile error
  %lu) - direct path"` and opens `win::open_physical_drive(n)`; when RDA is present but
  the partition open fails, prints `"RDA open failed (…) - direct path"` and falls back;
  only when BOTH fail does it print `"cannot open disk %u: … (elevate?)"` and refuse.
- `cr_transport.cpp:616-626 open_disk()`: `if (prefer_rda) { try RDA; } return
  open_physical_drive(disk, err);` — unconditional direct fallback.
- `cr_transport.cpp:721-759 open_disk_rw()`: same duality for the write path.
- `cr_enumerate.cpp:98-112 enumerate_disks()`: disk discovery ALWAYS uses direct
  `\\.\PhysicalDriveN` handles — enumeration never needs the driver.
- This mirrors the reference product's Device.mo duality (driver path for locked/damaged
  volumes + direct fallback), per the `cr_transport.h:1-9` header comment.

Therefore: **the UI must NOT refuse scans when the driver is absent** — it follows the
core: enumerate + scan via the direct path, and truthfully label the transport ("direct
path" vs "RDA driver path"), warning that the direct path is weaker for locked volumes and
damaged media (that is RDA's purpose) and that both paths require elevation. The UI refuses
a scan only when the direct open also fails (not elevated / device gone).

---

## 10. Install / elevation model

**Installation is service-based (SCM), not PnP/INF-driven for the live-OS profile:**

- `scripts/install.ps1` (84 lines): **requires an elevated PowerShell** (explicit guard).
  Copies `<source>.sys` → `%SystemRoot%\System32\drivers\<ServiceName>.sys`, then
  `sc.exe create <ServiceName> binPath= … type= kernel start= demand error= normal group=
  "Boot File System"`, writes `Parameters\{IoctlBase=0, MaxDisks=200, DebugLevel=1,
  SessionsEnabled=1}` (DWORDs), then `sc.exe start` (unless `-NoStart`). Prints the
  expected device name and the user-mode path template. Service-name choice defines the
  device identity: `-ServiceName eprdtdrv` → `\Device\EPRDTDRV` (DRW drop-in);
  `-ServiceName rda`/`RDA` → `\Device\RDA` (the core's canonical name).
- `scripts/uninstall.ps1`: elevated; `sc stop` + `sc delete` + remove the .sys file.
- `src/driver/rda.inf`: a DefaultInstall service INF (SERVICE_KERNEL_DRIVER,
  SERVICE_DEMAND_START, same Parameters) — shipped verbatim; the repo deliberately has no
  INF build tooling; requires a `rda.cat` (i.e., a signed package) for PnP/INF install, so
  in practice `install.ps1` is the deployment path today.
- **WinPE profile**: `scripts/winpe/regdrv-rda.reg` registers `rda` as a demand-start
  service with `IoctlBase=0x10000`, `FilterMode=1`, and the disk-class `UpperFilters`
  value (filter/copy-engine role).
- **Signing gates when the UI requests an install**: Win10/11 retail requires
  Microsoft attestation/HLK signing (Hardware Dev Center + EV cert); test-signing
  (`bcdedit /set testsigning on` + reboot) or a provisioned test cert is required for
  locally built drivers; Win7 accepts HCK or grandfathered SHA-1 cross-signing; WinPE is
  relaxed (docs/signing.md). The release pipeline ships UNSIGNED artifacts with an
  assertion; the OSSign pipeline exists but is disabled pending certificate onboarding.
- **When the UI needs elevation**: always, in practice — the SDDL blocks non-admin device
  opens, and `\\.\PhysicalDriveN` raw reads need admin too. Install/start/uninstall of the
  service additionally needs an elevated helper (run `install.ps1`/`uninstall.ps1` or
  issue the SCM calls) — recommend the UI's bootstrapper performs: SCM query (no admin
  needed) → if missing/stopped, offer "install/start driver" via an elevated child process
  that runs install.ps1 with `-ServiceName RDA`.
- CI-installed example (benchmark.yml): `sc.exe create rdatest type= kernel start= demand
  binPath= "System32\drivers\rdatest.sys" group= "FSFilter Boot File System"` + `sc.exe
  start rdatest` — the same minimal contract the UI's installer should reproduce.

---

## 11. CI style reference (rda-driver workflows)

The rda-wizard repo's workflows must follow this house style.

**`ci.yml` (195 lines) — the shape:**

- `name: ci` (lowercase); `on: push/PR → branches: [main]` + `workflow_dispatch`;
  `concurrency: group ${{ github.workflow }}-${{ github.ref }}, cancel-in-progress: true`;
  `permissions: contents: read` (minimal, top-level).
- Jobs: `lint` (ubuntu: clang-format `--dry-run --Werror` over the C/H sources; markdown sanity; portable unit tests), `fuzz` (ubuntu: bounded libFuzzer round + invariant selftest + ASan/UBSan units), `unit` (matrix `os: [windows-2022]`, `ilammy/msvc-dev-cmd@v1`, MSVC `/W4 /WX` build+run of the portable tests), `build` (matrix `runner/wdk/tag/configuration: [windows-2022]/[26100.6584]/[wdk26100]/[Debug, Release]`; steps: checkout → `nuget/setup-nuget@v2` → `nuget restore packages.config` → `microsoft/setup-msbuild@v2` → `msbuild rda.sln -m -v:minimal -p:Configuration=… -p:Platform=x64` → post-build GATES (stamp Win7 OS header, PE/import allowlist verify, unsigned assertion) → `actions/upload-artifact@v4` with `name: rda-${{ matrix.tag }}-${{ matrix.configuration }}`, `if-no-files-found: error`), `codeql` (windows-2022, `permissions: security-events: write`, init/analyze).
- Style notes: WHY-comments inside steps (the wdm.h include-order landmine, the windows-2022 pinning rationale), pinned toolchains, gates BEFORE artifacts, `fail-fast: false` on matrices.

**`benchmark.yml` (259 lines) — the pattern the user referenced specifically:**

- `name: benchmark`; comment block explains the whole pipeline up front (what is measured,
  how reference binaries are fetched at RUN time — never committed — and how test-signing
  works on SecureBoot-off runners).
- `on: workflow_dispatch` + `push: branches: [main]` **with `paths:` filter**
  (`src/**`, the workflow file itself) — expensive jobs only re-run when relevant inputs
  change.
- Top-level job `env:` block pins shared constants (`WDK_VERSION`,
  `RESEARCH_RAW` base URL, `VHD_PATH`, `SEED_MB`).
- Single `bench` job, `runs-on: windows-2022`, `timeout-minutes: 75`.
- Step sequence: checkout → NuGet/MSBuild setup → build (Release) → stamp + PE gate → fetch reference binaries (SHA-256 logged) → **test-sign with a job-local self-signed cert** (`New-SelfSignedCertificate -Type CodeSigningCert`, export, import into LocalMachine Root + TrustedPublisher, signtool sign/verify) → **create + seed a 4 GiB fixed VHDX via diskpart** (GPT, NTFS + FAT32 volumes, realistic JPEG/PDF/MP4/ZIP/text/1000-small-file corpus, `Write-VolumeCache` best-effort flush) → install and start BOTH drivers under distinct service names (`sc.exe create …; sc.exe start …`, recording failure reasons but continuing) → run the matrix with one JSON-emitting binary (`rdabench --device X --disk N --mode info|open|seq|rand|verify|fuzz …` appending to `bench-results\results.jsonl`) → build a markdown comparison + **cross-driver SHA-256 byte-equality verdict that FAILS the job on mismatch** → `if: always()` + `continue-on-error: true` cleanup (stop/delete services, detach VHD, remove files and the test cert) → `if: always()` artifact upload (`benchmark-results`, `retention-days: 30`).
- Reusable ideas for the wizard: workflow-level env pins, paths-filtered triggers,
  seeded-fixture creation as a step, JSONL results + step-summary markdown, verdict step
  that hard-fails, unconditional cleanup, bounded retention.

**`release.yml` (95 lines):**

- `on: push: tags: ['v*']` + `workflow_dispatch`; `permissions: contents: write`.
- One job: build Release → stamp → PE gate → assert unsigned → stage a `dist/` bundle
  (binaries + INF + scripts + LICENSE + README + selected docs) → compute
  `dist/SHA256SUMS` → upload artifact `rda-release-${{ github.ref_name }}`
  (`if-no-files-found: error`) → on tags, attach to the GitHub release via
  `softprops/action-gh-release@v2` with `generate_release_notes: true` and a body that
  states the signing posture honestly.
- Wizard analogue: `wizard-release-<tag>` artifacts, SHA256SUMS, release notes.

---

## 12. Gap list (UI-relevant; NEEDS-ISSUE)

1. **NEEDS-ISSUE (driver):** no runtime build/release identity. GET_INFO reports only
   wire 1.0; v0.1.0 and v0.2.0 artifacts are indistinguishable at runtime. The UI cannot
   display "v0.2.0" from the driver — it must read the service binary's file version
   (`GetFileVersionInfo` on `system32\drivers\rda.sys`) or trust GET_INFO's wire version.
   Proposed driver-side fix: extend `RDA_DRIVER_INFO` (bump StructSize) with a build id.
2. **NEEDS-ISSUE (driver):** no per-sector bad-sector/error-map API. Bad-sector knowledge
   is built client-side by retry+bisection (`cr_retry.cpp`); the driver surfaces only the
   failing NTSTATUS. If the UI wants a driver-authoritative error map, this needs a new
   IOCTL. Today: use the core's `ReadErrorMap`.
3. **NEEDS-ISSUE (driver):** no live throughput/statistics IOCTL. Progress exists only
   in the session protocol (GETNEXTBLOCK progress fields, GET_PROGRESS percent). Plain
   handle reads expose nothing; UI progress/throughput must be measured client-side.
4. **NEEDS-ISSUE (driver/UI):** no disk-enumeration IOCTL. DISCOVERY (0x800C2AC0) is
   filter-only and returns only session-0's diskId. The UI enumerates disks via
   `\\.\PhysicalDriveN` (as `cr_enumerate.cpp` does) — fine, but the transport-status
   panel cannot claim "driver sees N disks".
5. **NEEDS-ISSUE (driver):** no arrival/removal notifications (no IOCTL events). UI must
   use `WM_DEVICECHANGE`/polling plus re-running `rda_available`.
6. **NEEDS-ISSUE (driver):** GETNEXTBLOCK does not report the number of sectors actually
   pumped this round; the core estimates (clamped to plan bounds — `cr_transport.cpp:465-476`,
   audit row 41a). Fragile for exact progress rendering.
7. **NEEDS-ISSUE (driver):** session occupancy is unqueryable (64-slot table, 2000-node
   pump queue caps exist but are not exposed). UI should cap concurrent imaging sessions
   conservatively (1 per disk — the overlap guard enforces that anyway).
8. **NEEDS-ISSUE (UI):** no SCM helper in the core (installed-vs-stopped distinction);
   the UI implements `OpenSCManager`/`QueryServiceStatus` itself (documented in §9).
9. **NEEDS-ISSUE (driver/UI):** `IoctlBase` must be learned via GET_INFO before any
   session call; drivers predating GET_INFO cannot be session-addressed safely (rdacli
   just assumes base 0). UI: require GET_INFO success before enabling session features.
10. Note (not a gap): storage IOCTLs (geometry, extents, SMART passthrough) DO work
    through RDA target-bound handles via the pass-through path (`rda_ioctl.c:186-245`);
    the core currently probes geometry through whatever handle it holds — both transports
    work.

---

## 13. Traceability appendix

| Claim | Evidence |
|---|---|
| Device naming from service name; fallback EPMNTDRV; SDDL + class GUID; DO_DIRECT_IO | rda_control.c:27-168; rda_p.h:46-68 |
| Grammar + PID token transform; token mismatch → STATUS_INVALID_PARAMETER; disk limits (DISK_PART < MaxDisks 200, DISK < 100) | rda_compat.h:49-57, 161-279; rda_create.c:43-121; rda_p.h:55-56 |
| Identity IOCTL codes/gates | rda_ioctl.h:60-66; rda_ident.c:46-82, 150-174 |
| Extended IOCTL codes CTL_CODE_RDA(0x910/0x911/0x912); RDA_DRIVER_INFO / GET_INFO handler; GET_VERSION `(Major<<32)|Minor` | rda_ioctl.h:72-107; rda_ioctl.c:75-110 |
| OPEN_TARGET input + validation + resolution (DR retry passes disk# twice) | rda_ioctl.h:91-103; rda_ioctl.c:115-184; rda_target.c:135-189 |
| Pass-through method handling; dispatch order (session → bound → global) | rda_ioctl.c:186-286 |
| READ/WRITE Information = Length×512 | rda_readwrite.c:185-278 (esp. 267-273), 349-366; rda_ioctl.h:128-143; ground-truth-contracts.md §3 (CORRECTION-2) |
| rdacli asserts the scale; rdabench ÷512 fix | rdacli.c:192-200; commit c692ca6; worklog Session 2 (cont.) |
| v0.2.0 = 2b1db5f; wire 1.0 | git tag v0.2.0 → 2b1db5f; rda_ioctl.h:106-107; rda_version.h:3-5; rda.inf:22 |
| Session codes, gates, structs | rda_ioctl.h:146-342; rda_session.c:731-838 (open), 1057-1085 (finalize), 1091-1139 (direct), 1143-1191+ (file IO), 1365-1508 (dispatch) |
| Overlap → STATUS_SHARING_VIOLATION; handle = (index<<16)\|generation, 64 slots; kick cap 301; queue cap 2000; flush retries 20; pump guard 10 s | rda_ioctl.h:351-356; rda_session.c:65-73, 145-210, 751-754 |
| Crash cleanup sweep at CLEANUP | rda_session.c:52-55; rda_create.c:159-162 |
| Global-handle WRITE-len-4 / READ≥0x208 modes | rda_ioctl.h:358-365; rda_readwrite.c:317-340 |
| F11 unscale in core; RdaClient API surface; IoctlBase learned via GET_INFO | cr_transport.cpp:192-197, 661-707, 212-507; cr_rda.h:54-56, 103-104, 224-290 |
| rda_available + GLE diagnostic | cr_transport.cpp:576-588; cr_transport.h:58-62 |
| Direct fallback verdict | rrecov.cpp:130-157; cr_transport.cpp:616-626, 721-759; cr_enumerate.cpp:98-112, 156-164; cr_transport.h:1-9 |
| Retry policy (3 attempts, Skip/Fail, bisection); error codes 0x5002xxxx | cr_device.h:68-77; cr_retry.cpp:16, 40-99; cr_scan.cpp:141-151; cr_error.h:18-60 |
| Install/uninstall scripts + elevation guard; INF parameters; WinPE reg profile (IoctlBase 0x10000, FilterMode) | scripts/install.ps1:37-76; scripts/uninstall.ps1:15-36; rda.inf:45-66; scripts/winpe/regdrv-rda.reg:13-31 |
| Signing requirements per OS | docs/signing.md §1-2 |
| CI style (triggers/matrix/gates/artifacts) | .github/workflows/ci.yml, benchmark.yml, release.yml (whole files) |
| Session GETNEXTBLOCK Information = 0x1E; direct-family Information = outLength | rda_ioctl.h:227-251, 165-178; rda_session.c:613, 1137, 1472-1500 |
| Win7→Win11 single binary, import gate | docs/win7-compat.md; tools/verify_pe.py; ci.yml build steps |
