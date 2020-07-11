
namespace Breeze.NHibernate.Tests.Models
{
    public interface ICompositeEntity
    {
        ICompositeKey GetCompositeKey();
    }

    public abstract class CompositeEntity : ICompositeEntity
    {
        private volatile ICompositeKey _cachedKey;
        private readonly object _lock = new object();

        public virtual ICompositeKey GetCompositeKey()
        {
            if (_cachedKey != null)
            {
                return _cachedKey;
            }

            lock (_lock)
            {
                if (_cachedKey != null)
                {
                    return _cachedKey;
                }

                _cachedKey = CreateCompositeKeyInternal();
            }

            return _cachedKey;
        }

        protected abstract ICompositeKey CreateCompositeKeyInternal();

        public override bool Equals(object obj)
        {
            if (!(obj is CompositeEntity other))
            {
                return false;
            }

            return Equals(other.CreateCompositeKeyInternal(), CreateCompositeKeyInternal());
        }

        public override int GetHashCode()
        {
            return GetCompositeKey().GetHashCode();
        }
    }

    public abstract class CompositeEntity<TType, TCol1, TCol2> : CompositeEntity
    {
        protected abstract CompositeKey<TType, TCol1, TCol2> CreateCompositeKey();

        protected override ICompositeKey CreateCompositeKeyInternal()
        {
            return CreateCompositeKey();
        }
    }

    public abstract class CompositeEntity<TType, TCol1, TCol2, TCol3> : CompositeEntity
    {
        protected abstract CompositeKey<TType, TCol1, TCol2, TCol3> CreateCompositeKey();

        protected override ICompositeKey CreateCompositeKeyInternal()
        {
            return CreateCompositeKey();
        }
    }

    public abstract class CompositeEntity<TType, TCol1, TCol2, TCol3, TCol4> : CompositeEntity
    {
        protected abstract CompositeKey<TType, TCol1, TCol2, TCol3, TCol4> CreateCompositeKey();

        protected override ICompositeKey CreateCompositeKeyInternal()
        {
            return CreateCompositeKey();
        }
    }
}
