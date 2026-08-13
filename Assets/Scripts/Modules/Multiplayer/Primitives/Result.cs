using System;

namespace Modules.Multiplayer.Primitives
{
    public readonly struct Result<TValue, TError>
    {
        private readonly TValue _value;
        private readonly TError _error;

        private Result(bool isSuccess, TValue value, TError error)
        {
            IsSuccess = isSuccess;
            _value = value;
            _error = error;
        }

        public bool IsSuccess { get; }

        public bool IsFailure => !IsSuccess;

        public TValue Value => IsSuccess
            ? _value
            : throw new InvalidOperationException("Failure has no value.");

        public TError Error => IsFailure
            ? _error
            : throw new InvalidOperationException("Success has no error.");

        public static Result<TValue, TError> Success(TValue value)
        {
            return new Result<TValue, TError>(true, value, default);
        }

        public static Result<TValue, TError> Failure(TError error)
        {
            return new Result<TValue, TError>(false, default, error);
        }
    }

    public readonly struct Unit : IEquatable<Unit>
    {
        public static readonly Unit Value = new Unit();

        public bool Equals(Unit other)
        {
            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is Unit;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override string ToString()
        {
            return "()";
        }

        public static bool operator ==(Unit left, Unit right)
        {
            return true;
        }

        public static bool operator !=(Unit left, Unit right)
        {
            return false;
        }
    }
}
