namespace SafePhrase;

// Guards asynchronous countdown/composer continuations after cancellation or navigation.
internal sealed class SosFlowGuard
{
    public bool IsEnded { get; private set; }
    public bool HasPrepared { get; private set; }
    public bool IsPreparing { get; private set; }
    public bool CanAct(bool pageVisible, bool appVisible) => !IsEnded && pageVisible && appVisible;

    public bool TryPrepare(bool pageVisible, bool appVisible)
    {
        if (HasPrepared || IsPreparing || !CanAct(pageVisible, appVisible))
            return false;
        HasPrepared = true;
        IsPreparing = true;
        return true;
    }

    public bool TryNext(bool pageVisible, bool appVisible)
    {
        if (!HasPrepared || IsPreparing || !CanAct(pageVisible, appVisible))
            return false;
        IsPreparing = true;
        return true;
    }

    public void CompletePreparation() => IsPreparing = false;
    public void End() => IsEnded = true;
}
