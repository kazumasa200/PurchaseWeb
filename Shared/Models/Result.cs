namespace PurchaseWeb.Client.Models;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static Result<T> Success(T value) => new() { IsSuccess = true, Data = value };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}
