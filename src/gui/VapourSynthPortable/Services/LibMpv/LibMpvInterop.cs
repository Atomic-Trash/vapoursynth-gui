using System.Runtime.InteropServices;

namespace VapourSynthPortable.Services.LibMpv;

/// <summary>
/// P/Invoke declarations for libmpv
/// </summary>
internal static class LibMpvInterop
{
    private const string LibMpv = "libmpv-2.dll";

    // mpv_create - Create a new mpv handle
    [DllImport(LibMpv, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr mpv_create();

    // mpv_initialize - Initialize the mpv handle
    [DllImport(LibMpv, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_initialize(IntPtr ctx);

    // mpv_destroy - Destroy the mpv handle
    [DllImport(LibMpv, CallingConvention = CallingConvention.Cdecl)]
    public static extern void mpv_destroy(IntPtr ctx);

    // mpv_terminate_destroy - Stop playback and destroy
    [DllImport(LibMpv, CallingConvention = CallingConvention.Cdecl)]
    public static extern void mpv_terminate_destroy(IntPtr ctx);

    // mpv_command - Execute a command
    [DllImport(LibMpv, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_command(IntPtr ctx, IntPtr[] args);

    // mpv_command_string - Execute a command string
    [DllImport(LibMpv, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_command_string(IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)] string args);

    // mpv_set_property_string - Set a property as string
    [DllImport(LibMpv, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_set_property_string(IntPtr ctx,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string data);

    // mpv_set_property - Set a property
    [DllImport(LibMpv, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_set_property(IntPtr ctx,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        int format,
        ref long data);

    // mpv_set_property double
    [DllImport(LibMpv, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_set_property(IntPtr ctx,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        int format,
        ref double data);

    // mpv_get_property_string - Get a property as string
    [DllImport(LibMpv, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr mpv_get_property_string(IntPtr ctx,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    // mpv_get_property - Get a property
    [DllImport(LibMpv, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_get_property(IntPtr ctx,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        int format,
        out double data);

    [DllImport(LibMpv, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_get_property(IntPtr ctx,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        int format,
        out long data);

    [DllImport(LibMpv, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_get_property(IntPtr ctx,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        int format,
        out int data);

    // mpv_free - Free memory allocated by mpv
    [DllImport(LibMpv, CallingConvention = CallingConvention.Cdecl)]
    public static extern void mpv_free(IntPtr data);

    // mpv_set_option - Set an option
    [DllImport(LibMpv, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_set_option(IntPtr ctx,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        int format,
        ref long data);

    // mpv_set_option_string - Set an option as string
    [DllImport(LibMpv, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_set_option_string(IntPtr ctx,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string data);

    // mpv_observe_property - Observe property changes
    [DllImport(LibMpv, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_observe_property(IntPtr ctx, ulong reply_userdata,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name, int format);

    // mpv_wait_event - Wait for an event
    [DllImport(LibMpv, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr mpv_wait_event(IntPtr ctx, double timeout);

    // mpv_request_log_messages - Request log messages
    [DllImport(LibMpv, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_request_log_messages(IntPtr ctx,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string min_level);

    // mpv_error_string - Get error string
    [DllImport(LibMpv, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr mpv_error_string(int error);

    // mpv_client_api_version
    [DllImport(LibMpv, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint mpv_client_api_version();

    // Format constants
    public const int MPV_FORMAT_NONE = 0;
    public const int MPV_FORMAT_STRING = 1;
    public const int MPV_FORMAT_OSD_STRING = 2;
    public const int MPV_FORMAT_FLAG = 3;
    public const int MPV_FORMAT_INT64 = 4;
    public const int MPV_FORMAT_DOUBLE = 5;
    public const int MPV_FORMAT_NODE = 6;

    // Event IDs
    public const int MPV_EVENT_NONE = 0;
    public const int MPV_EVENT_SHUTDOWN = 1;
    public const int MPV_EVENT_LOG_MESSAGE = 2;
    public const int MPV_EVENT_GET_PROPERTY_REPLY = 3;
    public const int MPV_EVENT_SET_PROPERTY_REPLY = 4;
    public const int MPV_EVENT_COMMAND_REPLY = 5;
    public const int MPV_EVENT_START_FILE = 6;
    public const int MPV_EVENT_END_FILE = 7;
    public const int MPV_EVENT_FILE_LOADED = 8;
    public const int MPV_EVENT_IDLE = 11;
    public const int MPV_EVENT_TICK = 14;
    public const int MPV_EVENT_CLIENT_MESSAGE = 16;
    public const int MPV_EVENT_VIDEO_RECONFIG = 17;
    public const int MPV_EVENT_AUDIO_RECONFIG = 18;
    public const int MPV_EVENT_SEEK = 20;
    public const int MPV_EVENT_PLAYBACK_RESTART = 21;
    public const int MPV_EVENT_PROPERTY_CHANGE = 22;
    public const int MPV_EVENT_QUEUE_OVERFLOW = 24;

    // Error codes
    public const int MPV_ERROR_SUCCESS = 0;
    public const int MPV_ERROR_EVENT_QUEUE_FULL = -1;
    public const int MPV_ERROR_NOMEM = -2;
    public const int MPV_ERROR_UNINITIALIZED = -3;
    public const int MPV_ERROR_INVALID_PARAMETER = -4;
    public const int MPV_ERROR_OPTION_NOT_FOUND = -5;
    public const int MPV_ERROR_OPTION_FORMAT = -6;
    public const int MPV_ERROR_OPTION_ERROR = -7;
    public const int MPV_ERROR_PROPERTY_NOT_FOUND = -8;
    public const int MPV_ERROR_PROPERTY_FORMAT = -9;
    public const int MPV_ERROR_PROPERTY_UNAVAILABLE = -10;
    public const int MPV_ERROR_PROPERTY_ERROR = -11;
    public const int MPV_ERROR_COMMAND = -12;
    public const int MPV_ERROR_LOADING_FAILED = -13;

    [StructLayout(LayoutKind.Sequential)]
    public struct MpvEvent
    {
        public int event_id;
        public int error;
        public ulong reply_userdata;
        public IntPtr data;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MpvEventProperty
    {
        public IntPtr name;
        public int format;
        public IntPtr data;
    }

    public static string? GetErrorString(int error)
    {
        var ptr = mpv_error_string(error);
        return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : null;
    }
}
