using System;

/// <summary>
/// 取得通知・案内・簡易会話をプレイヤーへ表示するための静的イベントハブ。
/// どこからでも Notify() で発行でき、NotificationView が購読して画面に出す。
/// 購読側は OnDestroy で必ず解除すること。
/// </summary>
public static class Notifier
{
    public static event Action<string> OnNotify;

    public static void Notify(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        OnNotify?.Invoke(message);
    }
}
