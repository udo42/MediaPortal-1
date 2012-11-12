using System.Collections.Generic;
using System.Data.Objects;

namespace Mediaportal.TV.Server.TVDatabase.EntityModel.ObjContext
{
  /// <summary>
  /// Stores object context
  /// </summary>
  public interface IObjectContextStorage
  {
    /// <summary>
    /// Gets the object context for key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    ObjectContext GetObjectContextForKey(string key);

    /// <summary>
    /// Sets the object context for key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="objectContext">The object context.</param>
    void SetObjectContextForKey(string key, ObjectContext objectContext);

    /// <summary>
    /// Gets all object contexts.
    /// </summary>
    /// <returns></returns>
    IEnumerable<ObjectContext> GetAllObjectContexts();
  }
}