namespace Starcluster.Infrastructures
{
    public enum SqliteErrorCode
    {
        Unknown = -1,
        Ok = 0,

        GeneralError = 1,
        InternalError = 2,
        PermissionDenied = 3,
        Aborted = 4,
        Busy = 5,
        DatabaseLocked = 6,
        MemoryAcquisitionFailed = 7,
        DatabaseReadOnly = 8,
        Interrupted = 9,
        DeviceIoError = 10,
        DatabaseCorrupted = 11,
        UnknownOperation = 12,
        DatabaseFull = 13,
        DatabaseCantOpen = 14,
        LockProtocolError = 15,
        DatabaseEmpty = 16,
        SchemaMismatched = 17,
        ExceedSchemaSizeLimit = 18,
        ConstraintViolation = 19,
        TypeMismatched = 20,
        Misused = 21,
        LfsNotAvailable = 22,
        AuthorizationDenied = 23,
        DatabaseFormatError = 24,
        OutOfRange = 25,
        FileIsNotDatabase = 26,
        Notice = 27,
        Warning = 28,
        Row = 100,
        Done = 101,

        NonExtendedMask = 0xFF
    }
}