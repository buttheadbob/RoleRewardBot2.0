using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;

namespace RoleRewardBot.Objects
{
    public sealed class MyList<T> : ObservableCollection<T>
    {
        private Dispatcher m_dispatcher => RoleRewardBot.Instance.MainDispatcher;
        private readonly object _lock = new object();
        private bool m_notificationSuspended = false;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!m_notificationSuspended)
            {
                base.OnCollectionChanged(e);
            }
        }
        
        public new void Add(T item)
        {
            lock (_lock)
            {
                m_dispatcher.Invoke(() => Items.Add(item));
            }
        }
        
        public new void Remove(T item)
        {
            lock (_lock)
            {
                m_dispatcher.Invoke(() => Items.Remove(item));
            }
        }
        
        public new void RemoveAt(int index)
        {
            lock (_lock)
            {
                m_dispatcher.Invoke(() => Items.RemoveAt(index));
            }
        }
        
        public new void Clear()
        {
            lock (_lock)
            {
                m_dispatcher.Invoke(() => Items.Clear());
            }
        }
        
        public void AddRange(IEnumerable<T> items)
        {
            lock (_lock)
            {
                m_dispatcher.Invoke(() =>
                {
                    m_notificationSuspended = true;
                    foreach (var item in items)
                    {
                        Items.Add(item);
                    }
                    m_notificationSuspended = false;
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                });
            }
        }
    }
}