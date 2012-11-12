using System.Linq;

namespace Mediaportal.TV.Server.TVDatabase.EntityModel.Specification
{
    /// <summary>
    /// http://devlicio.us/blogs/jeff_perrin/archive/2006/12/13/the-specification-pattern.aspx
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public abstract class CompositeSpecification<TEntity> : ISpecification<TEntity>
    {
        protected readonly Specification<TEntity> _leftSide;
        protected readonly Specification<TEntity> _rightSide;

        protected CompositeSpecification(Specification<TEntity> leftSide, Specification<TEntity> rightSide)
        {
            _leftSide = leftSide;
            _rightSide = rightSide;
        }

      #region ISpecification<TEntity> Members

      public abstract TEntity SatisfyingEntityFrom(IQueryable<TEntity> query);

        public abstract IQueryable<TEntity> SatisfyingEntitiesFrom(IQueryable<TEntity> query);

      #endregion
    }
}
