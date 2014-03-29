contextlocal
============

ContextLocal for flowing ambient state across threads and tasks

If you are doing parallel operations on immutable data you may run into a problem. A lot of us are used to using `[ThreadStatic]` static fields to hold *ambient* state that exceeds the built-in ThreadPrincipal or CurrentCulture settings. If you are running in a web context you certainly have access to HttpContext, but otherwise you appear to be out of luck. 

Today I want to present a solution to that problem: `ContextLocal<T>`. A `ContextLocal<T>` static variable is local to the thread that sets its value *plus any child threads, tasks, async, or thread pool work items that it spawns*. The value is copy-on-write, so if a child thread changes the value that change is only visible to that child thread and *its* child threads. Furthermore, when all threads using a value are finished the value is cleaned up automatically. You don't have to worry about values modified on a child thread living forever because the thread is in a pool and never gets destroyed. 

What this means is we have a handy way to do the same thing that Thread.CurrentCulture does. *One caveat to note: The copy-on-write behavior is only on .Net 4.5 and later.*

How does this work exactly?
===

We need to back up slightly and look at what the framework provides. It has an `ExecutionContext` that itself holds various contexts and values. This object is *flowed* to child tasks/threads automatically, unless you use the thread pool's `QueueUnsafeUserWorkItem` or a similar operation to prohibit it. 

Within an `ExecutionContext` is a `CallContext`. That has `LogicalGetData` and `LogicalSetData`. Those are the magic methods we need with the copy-on-write behavior. Don't be fooled by the namespace or documentation, this class works perfectly well outside any WCF or remoting scenarios. Only values that implement the `ILogicalCallData` interface will actually be serialized and transmitted over the wire.

Best Practices
===

First, use a `ConcurrentDictionary` or similar class if you want to allow child threads to insert data into a shared data structure. That way none of them are modifying the original reference so they can all share the same copy.

Think of your program as a stream of clear water. Think of starting a `Task<T>` or launching another thread as the stream splitting into multiple branches. Now imagine `ContextLocal<T>` is a big barrel of red dye. If you drop it in at the head of the stream, all the water turns red. If you drop it on a branch, only that branch and its sub-branches turn red. 

The key is that you set the value just before the branch takes place and that value will be visible to all the branches and sub-branches as ambient state, but the value is completely isolated from any other unrelated branches. **That's the key difference between `ContextLocal<T>`, `static`, and `[ThreadStatic]`.**

`static` values are globally shared state.

`[ThreadStatic]` values are isolated to one thread

`ContextLocal<T>` values are shared among child threads, but only among child threads.


As far as I am aware, outside of a web server or the built-in properties like CurrentCulture, this is the *only* way to flow ambient state across threads cleanly. It sure beats adding context parameters to every single method call!
