// Created by Russ Bishop
// v1.0
// 2014/03/28
//
// http://russbishop.net
// github.com/xenadu
//
// MIT License. No warranty, use at your own risk. 

namespace ContextLocal
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Remoting.Messaging;


    /// <summary>
    /// Holds a value that is flowed to child Task/Thread contexts.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ContextLocal<T> where T : class
    {
        private string _name = Guid.NewGuid().ToString("N");
        private Func<T> _lazyInitializer;
        private static ConcurrentBag<WeakReference<T>> _createdInstances;

        /// <summary>
        /// Creates a new ContextLocal
        /// </summary>
        public ContextLocal()
        {
        }

        /// <summary>
        /// Creates a new ContextLocal
        /// </summary>
        /// <param name="lazyInitializer">If a thread asks for a value and no value was set by a parent thread, this function will be invoked to create a new value.
        /// The new value will be inherited by all child threads.</param>
        /// <param name="trackCreatedInstances">If true, created instances are tracked via weak references.</param>
        public ContextLocal(Func<T> lazyInitializer, bool trackCreatedInstances = false)
        {
            _lazyInitializer = lazyInitializer;
            if (trackCreatedInstances)
                _createdInstances = new ConcurrentBag<WeakReference<T>>();
        }

        /// <summary>
        /// The value, or default(T) if the value has not been set
        /// </summary>
        public T Value
        {
            get
            {
                var value = (T)CallContext.LogicalGetData(_name);
                if (value == null && _lazyInitializer != null)
                {
                    value = _lazyInitializer();
                    CallContext.LogicalSetData(_name, value);
                    if (_createdInstances != null)
                    {
                        _createdInstances.Add(new WeakReference<T>(value));
                    }
                }
                return value;
            }
            set
            {
                CallContext.LogicalSetData(_name, value);
            }
        }

        /// <summary>
        /// The instances that were created by the lazy initializer, or empty if lazy tracking not enabled.
        /// </summary>
        public IEnumerable<T> Instances
        {
            get 
            {
                if (_createdInstances == null)
                    yield break;

                var weakRefs = _createdInstances.ToArray();
                T value;
                foreach (var wr in weakRefs)
                {
                    if (wr.TryGetTarget(out value))
                        yield return value;
                }
            }
        }
    }
}
