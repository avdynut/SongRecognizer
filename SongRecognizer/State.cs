namespace SongRecognizer
{
    public enum State
    {
        Connected,
        Recording,
        SendingRecord,
        WaitingForResponse,
        Identifying,
        Completed,
        Failed
    }
}
