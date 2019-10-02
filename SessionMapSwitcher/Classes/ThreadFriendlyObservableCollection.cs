using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SessionMapSwitcher.Classes
{

    /// <summary>
    /// Overrides the OnCollectionChanged event to dispatch event to the UI thread to avoid any exceptions when accessing the collection from a Task.
    /// </summary>
    /// <remarks>
    /// reference: https://stackoverflow.com/questions/23108045/how-to-make-observablecollection-thread-safe
    /// </remarks>
    public class ThreadFriendlyObservableCollection<T> : ObservableCollection<T>
    {
        public override event NotifyCollectionChangedEventHandler CollectionChanged;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler CollectionChanged = this.CollectionChanged;

            if (CollectionChanged == null)
                return;

            foreach (NotifyCollectionChangedEventHandler handler in CollectionChanged.GetInvocationList())
            {
                DispatcherObject dispObj = handler.Target as DispatcherObject;
                if (dispObj != null)
                {
                    Dispatcher dispatcher = dispObj.Dispatcher;
                    if (dispatcher != null && !dispatcher.CheckAccess())
                    {
                        dispatcher.BeginInvoke(
                            (Action)(() => handler.Invoke(this,
                                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset))),
                            DispatcherPriority.DataBind);
                        continue;
                    }
                }
                handler.Invoke(this, e);
            }
        }

        public ThreadFriendlyObservableCollection() : base()
        {
        }

        public ThreadFriendlyObservableCollection(IEnumerable<T> items) : base(items)
        {
        }
    }
}
