using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Threading;
using NLog;

namespace RoleRewardBot.Objects
{
    public sealed class MyList<T> : ObservableCollection<T>
    {
        private Dispatcher m_dispatcher => RoleRewardBot.Instance.MainDispatcher;
        private readonly object _lock = new object();
        private bool m_notificationSuspended = false;
        private Logger Log = LogManager.GetCurrentClassLogger();

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!m_notificationSuspended)
            {
                base.OnCollectionChanged(e);
            }
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (!m_notificationSuspended)
                base.OnPropertyChanged(e);
        }

        public new void Add(T item)
        {
            lock (_lock)
            {
                // Config is loaded before plugin ui, so no dispatcher will be set yet...
                if (m_dispatcher is null)
                {
                    Items.Add(item);
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    return;
                }
                
                m_dispatcher.Invoke(() =>
                {
                    Items.Add(item);
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                });
                
            }
        }
        
        public new void Remove(T item)
        {
            lock (_lock)
            {
                m_dispatcher.Invoke(() =>
                {
                    var result = Items.Remove(item);
                    if (result)
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    return result;

                });
            }
        }
        
        public new void RemoveAt(int index)
        {
            lock (_lock)
            {
                m_dispatcher.Invoke(() =>
                {
                    Items.RemoveAt(index);
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                });
                
            }
        }
        
        public new void Clear()
        {
            lock (_lock)
            {
                m_dispatcher.Invoke(() =>
                {
                    Items.Clear();
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                });
            }
        }
        
        public void AddRange(IEnumerable<T> items)
        {
            try
            {
                lock (_lock)
                {
                    // Config is loaded before plugin ui, so no dispatcher will be set yet...
                    if (m_dispatcher is null)
                    {
                        m_notificationSuspended = true;

                        foreach (var item in items)
                        {
                            Items.Add(item);
                        }

                        m_notificationSuspended = false;
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                        return;
                    }
                    
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
            catch (Exception ex)
            {
                Log.Error("Error adding range: " + ex);
            }
        }
    }
}