
using System.Diagnostics.Contracts;

namespace ContainerHive.Core.Common.Helper.Result {

    public enum ResultState : byte {
        Faulted,
        Success
    }

    public readonly struct Result<A> : IEquatable<Result<A>>, IComparable<Result<A>> {

        internal readonly ResultState State;
        internal readonly A Value;
        readonly Exception? exception;

        public Result(A value) {
            State = ResultState.Success;
            this.Value = value;
            exception = null;
        }

        public Result(Exception ex) {
            State = ResultState.Faulted;
            this.exception = ex;
            Value = default;
        }

        public static implicit operator Result<A>(A value) => new(value);

        public static implicit operator Result<A>(Exception ex) => new(ex);


        public bool IsFaulted => State == ResultState.Faulted;

        public bool IsSuccess => State == ResultState.Success;

        public override string ToString() =>
            IsFaulted ?
                exception?.ToString() ?? "Undefined Exception" :
                Value?.ToString() ?? "(null)";

        public bool Equals(Result<A> b) {
            if (IsFaulted && !b.IsFaulted || !IsFaulted && b.IsFaulted) return false;
            if (IsFaulted && b.IsFaulted) {
                if (exception == null && b.exception == null) 
                    return true;
                return exception?.Equals(b.exception) ?? false;
            }
            if(Value == null && b.Value == null) 
                return true;
            return Value?.Equals(b.Value) ?? false;
        }

        public static bool operator==(Result<A> a, Result<A> b) =>
            Equals(a, b);

        public static bool operator !=(Result<A> a, Result<A> b) =>
            !(a==b);

        public A IfFail(A defaultValue) =>
            IsFaulted ? defaultValue : Value;

        public A IfFail(Func<Exception, A> f) =>
            IsFaulted ? f(exception) : Value;

        public void IfSucc(Action<A> f) {
            if(IsSuccess)
                f(Value);
        }

        public R Match<R>(Func<A, R> Succ, Func<Exception, R> Fail) =>
            IsFaulted ? Fail(exception) : Succ(Value);

        public Result<B> Map<B>(Func<A, B> map) =>
            IsFaulted ? new Result<B>(exception) : new Result<B>(map(Value));

        public async Task<Result<B>> MapAsync<B>(Func<A, Task<B>> map) =>
            IsFaulted ? new Result<B>(exception) : new Result<B>(await map(Value));

        public int CompareTo(Result<A> b) {
            if (IsFaulted && b.IsFaulted) return 0;
            if (IsFaulted && !b.IsFaulted) return -1;
            if (!IsFaulted && b.IsFaulted) return 1;
            return Comparer<A>.Default.Compare(Value, b.Value);
        }

        public static bool operator <(Result<A> a, Result<A> b) =>
            a.CompareTo(b) < 0;
        public static bool operator <=(Result<A> a, Result<A> b) =>
            a.CompareTo(b) <= 0;
        public static bool operator >(Result<A> a, Result<A> b) =>
            a.CompareTo(b) > 0;
        public static bool operator >=(Result<A> a, Result<A> b) =>
            a.CompareTo(b) >= 0;
    }
}
