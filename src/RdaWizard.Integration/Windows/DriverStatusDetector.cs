using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using RdaWizard.Core.Models;
using RdaWizard.Core.Services;

namespace RdaWizard.Integration.Windows;

/// <summary>
/// Driver presence/version detection — the UI-side probe sequence from
/// TRANSPORT-CONTRACT §9, exactly as the core's own transport chooses paths:
/// CreateFile("\\.\RDA") presence probe → GLE classification → SCM
/// installed/stopped distinction. This NEVER issues IOCTLs (transport is
/// the core's job; we only detect + label truthfully). All Windows calls
/// are runtime-guarded so the type constructs on any host — probes report
/// Unknown elsewhere.
/// </summary>
public sealed partial class DriverStatusDetector
{
    private const uint GenericRead = 0x80000000;
    private const uint OpenExisting = 3;
    private const uint ShareRead = 1;
    private const uint ShareWrite = 2;
    private const int InvalidHandleValue = -1;

    private readonly ILogger<DriverStatusDetector> _logger;

    public DriverStatusDetector(ILogger<DriverStatusDetector> logger) => _logger = logger;

    public Task<DriverStatus> ProbeAsync(CancellationToken cancellationToken = default)
        => Task.Run(() =>
        {
            if (!OperatingSystem.IsWindows())
            {
                return new DriverStatus { State = DriverState.Unknown, LastError = null };
            }

            return ProbeWindows();
        }, cancellationToken);

    private DriverStatus ProbeWindows()
    {
        var handle = CreateFileW(@"\\.\RDA", GenericRead, ShareRead | ShareWrite, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
        if (handle.ToInt64() != InvalidHandleValue)
        {
            // Device reachable: GET_INFO/GET_VERSION happen through the engine at scan
            // time; for status labeling, reachability + handle semantics are enough.
            CloseHandle(handle);
            _logger.LogInformation("Driver probe: \\\\.\\RDA reachable (loaded)");
            return new DriverStatus
            {
                State = DriverState.LoadedOk,
                WireVersion = null, // resolved by the engine at scan time ([transport] line)
                LastError = 0,
            };
        }

        var gle = Marshal.GetLastWin32Error();
        var state = gle switch
        {
            2 => ServiceExists() ? DriverState.InstalledNotLoaded : DriverState.NotInstalled, // ERROR_FILE_NOT_FOUND
            5 => DriverState.Blocked, // ERROR_ACCESS_DENIED (SDDL: admins/system only)
            _ => DriverState.WrongVersion,
        };

        _logger.LogInformation("Driver probe: not reachable (GLE {Gle}) -> {State}", gle, state);
        return new DriverStatus { State = state, WireVersion = null, LastError = gle };
    }

    private static bool ServiceExists()
    {
        var scm = IntPtr.Zero;
        var service = IntPtr.Zero;
        try
        {
            scm = OpenSCManagerW(IntPtr.Zero, IntPtr.Zero, ScManagerEnumerateService);
            if (scm == IntPtr.Zero)
            {
                return false;
            }

            service = OpenServiceW(scm, "RDA", ServiceQueryStatus);
            return service != IntPtr.Zero;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (service != IntPtr.Zero) CloseHandle(service);
            if (scm != IntPtr.Zero) CloseHandle(scm);
        }
    }

    private const uint ScManagerEnumerateService = 0x0004;
    private const uint ServiceQueryStatus = 0x0004;

    [LibraryImport("kernel32", EntryPoint = "CreateFileW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr CreateFileW(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [LibraryImport("kernel32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(IntPtr hObject);

    [LibraryImport("advapi32", EntryPoint = "OpenSCManagerW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr OpenSCManagerW(
        IntPtr lpMachineName,
        IntPtr lpDatabaseName,
        uint dwDesiredAccess);

    [LibraryImport("advapi32", EntryPoint = "OpenServiceW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr OpenServiceW(
        IntPtr hScManager,
        string lpServiceName,
        uint dwDesiredAccess);
}

/// <summary>Driver status service implementation (delegates to the detector; non-Windows → Unknown).</summary>
public sealed class DriverStatusService : IDriverStatusService
{
    private readonly DriverStatusDetector _detector;

    public DriverStatusService(DriverStatusDetector detector) => _detector = detector;

    public Task<DriverStatus> ProbeAsync(CancellationToken cancellationToken = default)
        => !OperatingSystem.IsWindows()
            ? Task.FromResult(new DriverStatus { State = DriverState.Unknown })
            : _detector.ProbeAsync(cancellationToken);
}
