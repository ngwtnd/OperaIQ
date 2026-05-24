using System;

namespace OperaIQ.Application.Common
{
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public string? Error { get; }

        protected Result(bool isSuccess, string? error)
        {
            if (isSuccess && error != null)
                throw new InvalidOperationException("Success result cannot contain an error message.");
            if (!isSuccess && error == null)
                throw new InvalidOperationException("Failure result must contain an error message.");

            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new Result(true, null);
        public static Result Failure(string error) => new Result(false, error);

        public static Result<T> Success<T>(T value) => Result<T>.Success(value);
        public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
    }

    public class Result<T> : Result
    {
        private readonly T? _value;

        public T Value => IsSuccess 
            ? _value! 
            : throw new InvalidOperationException("Cannot access the value of a failure result.");

        protected Result(bool isSuccess, string? error, T? value) : base(isSuccess, error)
        {
            _value = value;
        }

        public static Result<T> Success(T value) => new Result<T>(true, null, value);
        public new static Result<T> Failure(string error) => new Result<T>(false, error, default);
    }
}
