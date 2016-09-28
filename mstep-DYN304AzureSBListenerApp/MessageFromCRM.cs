using Microsoft.ServiceBus.Messaging;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace mstep_DYN304AzureSBListenerApp
{
    public class MessageFromCRM
    {
        private string _messageId;
        public string MessageId
        {
            get
            {
                return _messageId;
            }
            set
            {
                if (_messageId == value) return;
                _messageId = value;
                OnPropertyChanged("MessageId");
            }
        }
        private DateTime _enqueuedTimeUtc;
        public DateTime EnqueuedTimeUtc
        {
            get
            {
                return _enqueuedTimeUtc;
            }
            set
            {
                if (_enqueuedTimeUtc == value) return;
                _enqueuedTimeUtc = value;
                OnPropertyChanged("EnqueuedTimeUtc");
            }
        }
        private long _size;
        public long Size
        {
            get
            {
                return _size;
            }
            set
            {
                if (_size == value) return;
                _size = value;
                OnPropertyChanged("Size");
            }
        }
        private IDictionary<string, object> _properties;
        public IDictionary<string, object> Properties
        {
            get
            {
                return _properties;
            }
            set
            {
                if (_properties == value) return;
                _properties = value;
                OnPropertyChanged("Properties");
            }
        }
        private Entity _inputEntity;
        public Entity InputEntity
        {
            get
            {
                return _inputEntity;
            }
            set
            {
                if (_inputEntity == value) return;
                _inputEntity = value;
                OnPropertyChanged("InputEntity");
            }
        }
        private Entity _preEntity;
        public Entity PreEntity
        {
            get
            {
                return _preEntity;
            }
            set
            {
                if (_preEntity == value) return;
                _preEntity = value;
                OnPropertyChanged("PreEntity");
            }
        }
        private Entity _postEntity;
        public Entity PostEntity
        {
            get
            {
                return _postEntity;
            }
            set
            {
                if (_postEntity == value) return;
                _postEntity = value;
                OnPropertyChanged("PostEntity");
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class MessagesFromCRM: ObservableCollection<MessageFromCRM>
    {
        public MessagesFromCRM()
        {
        }
    }
}
