using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Lazy8.Core
{
  /// <summary>
  /// A descendent of HashSet<T> that requires it be constructed with
  /// a Predicate<T> callback.The callback is executed before an item T
  /// is added with the Add(T) method.  If the predicate returns false, the
  /// item is not added and Add(T) returns false. This prevents any unwanted
  /// items from being added to the hashset.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class GuardedHashSet<T> : HashSet<T>
  {
    private readonly Predicate<T> _predicate;

    public GuardedHashSet(Predicate<T> predicate) : base() => this._predicate = predicate;

    public GuardedHashSet(IEnumerable<T> collection, Predicate<T> predicate) : base(collection) => this._predicate = predicate;

    public GuardedHashSet(IEqualityComparer<T>? comparer, Predicate<T> predicate) : base(comparer) => this._predicate = predicate;

    public GuardedHashSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer, Predicate<T> predicate) : base(collection, comparer) => this._predicate = predicate;

    public GuardedHashSet(Int32 capacity, Predicate<T> predicate) : base(capacity) => this._predicate = predicate;

    public GuardedHashSet(Int32 capacity, IEqualityComparer<T>? comparer, Predicate<T> predicate) : base(capacity, comparer) => this._predicate = predicate;

    /* GuardedHashSet<T>(SerializationInfo, StreamingContext, predicate) constructor is not implemented.
       
       BinaryFormatter, SerializationInfo, and related classes will be marked as obsolete in .Net 7,
       and removed entirely in .Net 8.
    
       https://github.com/dotnet/designs/blob/main/accepted/2020/better-obsoletion/binaryformatter-obsoletion.md
    */

    /// <summary>
    /// Adds the specified element to a set, only if the predicate specified in the constructor returns true.
    /// <para>If the element is already in the set, or the predicate returns false, then this method returns false.</para>
    /// </summary>
    /// <param name="item">An item of type T</param>
    /// <returns>True if the predicate returns true, and the element is not already present in the set.  False otherwise.</returns>
    public new Boolean Add(T item) => this._predicate(item) && base.Add(item);
  }
}
