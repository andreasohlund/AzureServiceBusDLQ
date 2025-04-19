public class NServiceBus
{
    public static class Headers
    {
        public const string FailedQ = "NServiceBus.FailedQ";
        public const string ExceptionType = $"{ExceptionInfoPrefix}ExceptionType";
        public const string Message = $"{ExceptionInfoPrefix}Message";
        public const string Source = $"{ExceptionInfoPrefix}Source";
        public const string StackTrace = $"{ExceptionInfoPrefix}StackTrace";
        public const string TimeOfFailure = "NServiceBus.TimeOfFailure";
        public const string MessageId = "NServiceBus.MessageId";

        const string ExceptionInfoPrefix = "NServiceBus.ExceptionInfo.";
    }
}