namespace Infra.Persistance.Entities;

/// <summary>
/// 結果を表すクラス
/// </summary>
/// <typeparam name="T"></typeparam>
public class Result<T>
{
    /// <summary>
    /// 成功したか
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// データ
    /// </summary>
    public T? Data { get; }

    /// <summary>
    /// エラーメッセージ
    /// </summary>
    public string? ErrorMessage { get; }

    private Result(bool isSuccess, T? data, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Data = data;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// 成功した結果を返す
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static Result<T> Success(T data) => new(true, data, null);

    /// <summary>
    /// 失敗した結果を返す
    /// </summary>
    /// <param name="errorMessage"></param>
    /// <returns></returns>
    public static Result<T> Failure(string errorMessage) => new(false, default, errorMessage);
}
