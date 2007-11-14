using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Exortech.NetReflector;

namespace WeSay.Foundation
{
    /// <summary>
    /// Used to refer to this option from a field
    /// </summary>
    public class OptionRefCollection: IParentable,
                                      INotifyPropertyChanged,
                                     // ICollection<string>,
                                        IReportEmptiness
    {
       // private readonly List<string> _keys;
        private BindingList<OptionRef> _members;

        /// <summary>
        /// This "backreference" is used to notify the parent of changes. 
        /// IParentable gives access to this during explicit construction.
        /// </summary>
        private IReceivePropertyChangeNotifications _whomToNotify;

        private BindingList<OptionRef> _optionRefProxyList = new BindingList<OptionRef>();

        public OptionRefCollection(): this(null)
        {
        }
        public OptionRefCollection(IReceivePropertyChangeNotifications whomToNotify)
        {
            _whomToNotify = whomToNotify;
            //_keys = new List<string>();
            _members = new BindingList<OptionRef>();
        }

        public bool IsEmpty
        {
            get { return _members.Count == 0; }
        }

        #region ICollection<string> Members

//        void ICollection<string>.Add(string key)
//        {
//            if (Keys.Contains(key))
//            {
//                throw new ArgumentOutOfRangeException("key", key,
//                        "OptionRefCollection already contains that key");
//            }
//
//            Add(key);
//        }

        private OptionRef FindByKey(string key)
        {
            foreach (OptionRef _member in _members)
            {
                if(_member.Key == key)
                {
                    return _member;
                }

            }
            return null;
        }

        /// <summary>
        /// Removes a key from the OptionRefCollection
        /// </summary>
        /// <param name="key">The OptionRef key to be removed</param>
        /// <returns>true when removed, false when doesn't already exists in collection</returns>
        public bool Remove(string key)
        {
            OptionRef or = FindByKey(key);
            if (or!=null)
            {
                this._members.Remove(or);
                NotifyPropertyChanged();
                return true;
            }
            return false;
        }

        public bool Contains(string key)
        {
            foreach (OptionRef _member in _members)
            {
                if(_member.Key == key)
                    return true;
            }
            return false;
            //return Keys.Contains(key);
        }

        public int Count
        {
            get { return _members.Count; }
        }

        public void Clear()
        {
            _members.Clear();
            NotifyPropertyChanged();
        }

//        public void CopyTo(string[] array, int arrayIndex)
//        {
//            Keys.CopyTo(array, arrayIndex);
//        }
//
//        public bool IsReadOnly
//        {
//            get { return false; }
//        }
//
//        public IEnumerator<string> GetEnumerator()
//        {
//            return Keys.GetEnumerator();
//        }
//
//        IEnumerator IEnumerable.GetEnumerator()
//        {
//            return Keys.GetEnumerator();
//        }

        #endregion

        #region INotifyPropertyChanged Members

        /// <summary>
        /// For INotifyPropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region IParentable Members

        public WeSayDataObject Parent
        {
            set { _whomToNotify = value; }
        }

        #endregion

        protected void NotifyPropertyChanged()
        {
            //tell any data binding
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("option"));
                        //todo
            }

            //tell our parent
            _whomToNotify.NotifyPropertyChanged("option");
        }

        /// <summary>
        /// Adds a key to the OptionRefCollection
        /// </summary>
        /// <param name="key">The OptionRef key to be added</param>
        /// <returns>true when added, false when already exists in collection</returns>
        public bool Add(string key)
        {
            if (Contains(key))
            {
                return false;
            }

            _members.Add(new OptionRef(key));
            NotifyPropertyChanged();
            return true;
        }

        /// <summary>
        /// Adds a set of keys to the OptionRefCollection
        /// </summary>
        /// <param name="keys">A set of keys to be added</param>
        public void AddRange(IEnumerable<string> keys)
        {
            bool changed = false;
            foreach (string key in keys)
            {
                if ( this.Contains(key))
                {
                    continue;
                }

                Add(key);
                changed = true;
            }

            if (changed)
            {
                NotifyPropertyChanged();
            }
        }

        #region IReportEmptiness Members

        public bool ShouldHoldUpDeletionOfParentObject
        {
            get
            {
                //this one is a conundrum.  Semantic domain gathering involves making senses
                //and adding to their semantic domain collection, without (necessarily) adding
                //a gloss.  We don't want this info lost just because some eager-beaver decides
                //to clean up.
                // OTOH, we would like to have this *not* prevent deletion, if it looks like
                //the user is trying to delete the sense.
                //It will take more code to have both of these desiderata at the same time. For
                //now, we'll choose the first one, in interest of not loosing data.  It will just
                //be impossible to delete such a sense until we have SD editing.
                return ShouldBeRemovedFromParentDueToEmptiness;
            }
        }

        public bool ShouldCountAsFilledForPurposesOfConditionalDisplay
        {
            get { return !(IsEmpty); }
        }

        public bool ShouldBeRemovedFromParentDueToEmptiness
        {
            get
            {
                foreach (string s in Keys)
                {
                    if (!string.IsNullOrEmpty(s))
                    {
                        return false;   // one non-empty is enough to keep us around
                    }
                }
                return true;
            }
        }

        public IEnumerable<string>Keys
        {
            get
            {
                foreach (OptionRef _member in _members)
                {
                    yield return _member.Key;
                }
            }
        }

        public IBindingList Members
        {
            get { return _members; }
        }

        public string KeyAtIndex(int index)
        {
            if (index < 0)
                throw new ArgumentException("index");
            if (index >= _members.Count)
                throw new ArgumentException("index");
            return _members[index].Key;
        }

//        public IEnumerable<OptionRef> AsEnumeratorOfOptionRefs
//        {
//            get
//            {
//                foreach (string key in _keys)
//                {
//                    OptionRef or = new OptionRef();
//                    or.Value = key;
//                   yield return or;
//                }
//            }
//        }

//        public IBindingList GetConnectedBindingListOfOptionRefs()
//        {
//            foreach (string key in _keys)
//                {
//                    OptionRef or = new OptionRef();
//                    or.Key = key;
//                    or.Parent = (WeSayDataObject) _whomToNotify ;
//                
//                    _optionRefProxyList.Add(or);
//                }
//            
//        }

        public void RemoveEmptyStuff()
        {
            List<string> condemened=new List<string>();
            foreach (string s in Keys)
            {
                if(string.IsNullOrEmpty(s))
                {
                    condemened.Add(s);
                }
            }
            foreach (string s in condemened)
            {
                this.Remove(s);
            }
        }

        #endregion
    }

    /// <summary>
    /// Used to refer to this option from a field. 
    /// This class just wraps the key, which is a string, with various methods to make it fit in
    /// with the system.
    /// </summary>
    public class OptionRef: Annotatable, IParentable, IValueHolder<string>, IReportEmptiness, IReferenceContainer
    {
        private string _humanReadableKey;

        /// <summary>
        /// This "backreference" is used to notify the parent of changes. 
        /// IParentable gives access to this during explicit construction.
        /// </summary>
        private IReceivePropertyChangeNotifications _parent;

        public OptionRef() : this(string.Empty)
        {
          
        }
        public OptionRef(string key) //WeSay.Foundation.WeSayDataObject parent)
        {
            _humanReadableKey = key;
        }
        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(Value); }
        }

        #region IParentable Members

        public WeSayDataObject Parent
        {
            set { _parent = value; }
        }

        #endregion

        #region IValueHolder<string> Members

        /// <summary>
        /// For INotifyPropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public string Key
        {
            get{ return Value;}
            set{ Value = value;}
        }
        public string Value
        {
            get { return _humanReadableKey; }
            set
            {
                if(value !=null)
                {
                    _humanReadableKey = value.Trim();
                }
                else
                {
                    _humanReadableKey = null;
                }
                // this.Guid = value.Guid;
                NotifyPropertyChanged();
            }
        }

        // IReferenceContainer
        public object Target
        {
            get
            {
               // return Lexicon.FindFirstLexEntryMatchingId(_targetId);
               // OptionsList pretend = null;
                //return pretend.GetOptionFromKey(_humanReadableKey);
                throw new NotImplementedException();
                
            }
            set
            {
                if(value == null && String.IsNullOrEmpty(_humanReadableKey))
                {
                    return;
                }

                Option o = value as Option;
                if (o.Key == _humanReadableKey)
                {
                    return;
                }

                if (value == null)
                {
                    _humanReadableKey = string.Empty;
                }
                else
                {
                    _humanReadableKey = o.Key;
                }
                NotifyPropertyChanged();
            }
        }


        #endregion

        private void NotifyPropertyChanged()
        {
            //tell any data binding
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("option"));
                        //todo
            }

            //tell our parent

            if (_parent != null)
            {
                _parent.NotifyPropertyChanged("option");
            }
        }

        #region IReportEmptiness Members

        public bool ShouldHoldUpDeletionOfParentObject
        {
            get { return false; }
        }

        public bool ShouldCountAsFilledForPurposesOfConditionalDisplay
        {
            get { return !IsEmpty; }
        }

        public bool ShouldBeRemovedFromParentDueToEmptiness
        {
            get { return IsEmpty; }
        }


        public void RemoveEmptyStuff()
        {
            if(Value == string.Empty)
            {
                Value = null; // better for matching 'missing' for purposes of missing info task
            }

        }

        #endregion
    }

    /// <summary>
    /// Just makes the xml serialization work right
    /// </summary>
    public class OptionsListWrapper
    {
        [XmlElement(typeof (Option), ElementName = "option")]
        public List<Option> _options;
    }

    /// <summary>
    /// This is like a PossibilityList in FieldWorks, or RangeSet in Toolbox
    /// </summary>
    [XmlRoot("optionsList")]
    public class OptionsList 
    {
        private List<Option> _options;

        public OptionsList()
        {
            _options = new List<Option>();
        }

        /// <summary>
        /// just to get the old xml format (which includes a <options> element around the options) read in
        /// </summary>
        [XmlElement("options")]
        public OptionsListWrapper options
        {
            set { _options = value._options; }
            get
            {
                //                OptionsListWrapper w = new OptionsListWrapper();
                //                w._options = _options;
                //                return w;
                return null;
            }
        }

        [XmlElement(typeof (Option), ElementName = "option")]
        public List<Option> Options
        {
            get { return _options; }
            set { _options = value; }
        }

        public static OptionsList LoadFromFile(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof (OptionsList));
            using (XmlReader reader = XmlReader.Create(path))
            {
                OptionsList list = (OptionsList) serializer.Deserialize(reader);
                reader.Close();

#if DEBUG
                foreach (Option option in list.Options)
                {
                    Debug.Assert(option.Name.Forms != null);
                }
#endif
                return list;
            }
        }

        public void SaveToFile(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof (OptionsList));
            XmlAttributeOverrides overrides = new XmlAttributeOverrides();
            XmlAttributes ignoreAttr = new XmlAttributes();
            ignoreAttr.XmlIgnore = true;
            overrides.Add(typeof (Annotatable), "IsStarred", ignoreAttr);

            using (StreamWriter writer = File.CreateText(path))
            {
                serializer.Serialize(writer, this);
                writer.Close();
            }
        }

        public Option GetOptionFromKey(string value)
        {
            foreach (Option option in Options)
            {
                if (option.Key == value)
                    return option;
            }
            return null;
        }

    }

    [XmlRoot("option")]
    public class Option : IChoice
    {
        private MultiText _abbreviation;
        private MultiText _description;
        private string _humanReadableKey;
        private MultiText _name;

        public Option()
                : this(string.Empty, new MultiText()) {}

        public Option(string humanReadableKey, MultiText name) //, Guid guid)
        {
            _humanReadableKey = humanReadableKey;
            _name = name;
        }

        #region IChoice Members

        public string Label
        {
            get { return this._name.GetFirstAlternative(); }
        }

        #endregion

        [ReflectorProperty("key", Required = true)]
        [XmlElement("key")]
        public string Key
        {
            get
            {
                if (String.IsNullOrEmpty(_humanReadableKey))
                {
                    return GetDefaultKey(); //don't actually save it yet
                }

                else
                {
                    return _humanReadableKey;
                }
            }

            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    _humanReadableKey = GetDefaultKey();
                }
                        //the idea here is, we're delaying setting the key in concrete for as long as possible
                        //this allows the ui to continue to auto-create the key during a ui session.
                else if (value != GetDefaultKey())
                {
                    _humanReadableKey = value;
                }
            }
        }

        [ReflectorProperty("name", typeof (MultiTextSerializorFactory),
                Required = true)]
        [XmlElement("name")]
        public MultiText Name
        {
            get { return _name; }
            set { _name = value; }
        }

        [ReflectorProperty("abbreviation", typeof (MultiTextSerializorFactory),
                Required = false)]
        [XmlElement("abbreviation")]
        public MultiText Abbreviation
        {
            get
            {
                if (_abbreviation == null)
                {
                    return Name;
                }
                return _abbreviation;
            }
            set { _abbreviation = value; }
        }

        [ReflectorProperty("description", typeof (MultiTextSerializorFactory),
                Required = false)]
        [XmlElement("description")]
        public MultiText Description
        {
            get
            {
                if (_description == null)
                {
                    _description = new MultiText();
                }
                return _description;
            }
            set { _description = value; }
        }

        private string GetDefaultKey()
        {
            string name = Name.GetFirstAlternative();
            if (!String.IsNullOrEmpty(name))
            {
                return name;
            }
            return Guid.NewGuid().ToString();
        }

   

        //        [ReflectorProperty("guid", Required = false)]
        //        public Guid Guid
        //        {
        //            get
        //            {
        //                if (_guid == null || _guid == Guid.Empty)
        //                {
        //                    return Guid.NewGuid();
        //                }
        //                return _guid;
        //            }
        //            set { _guid = value; }
        //        }

        public override string ToString()
        {
            return _name.GetFirstAlternative();
        }

        public object GetDisplayProxy(string writingSystemId)
        {
            return new OptionDisplayProxy(this, writingSystemId);
        }

        #region Nested type: OptionDisplayProxy

        /// <summary>
        /// Gives a monolingual representation of the object for use by a combo-box
        /// </summary>
        public class OptionDisplayProxy
        {
            private readonly string _writingSystemId;
            private Option _option;

            public OptionDisplayProxy(Option option, string writingSystemId)
            {
                _writingSystemId = writingSystemId;
                _option = option;
            }

            public string Key
            {
                get { return _option.Key; }
            }

            public Option UnderlyingOption
            {
                get { return _option; }
                set { _option = value; }
            }

            public override string ToString()
            {
                return _option.Name.GetBestAlternative(_writingSystemId, "*");
            }
        }

        #endregion


    }

    public class OptionDisplayAdaptor : IChoiceSystemAdaptor<Option, String, OptionRef>
        {
            private readonly OptionsList _allOptions;
            private readonly string _preferredWritingSystemId;
        private IDisplayStringAdaptor _toolTipAdaptor;

        public OptionDisplayAdaptor(OptionsList allOptions,  string preferredWritingSystemId)
            {
                this._allOptions = allOptions;
                this._preferredWritingSystemId = preferredWritingSystemId;
            }

            #region IDisplayStringAdaptor Members

            public string GetDisplayLabel(object item)
            {
                if (item == null)
                    return string.Empty;

                Option option = item as Option;// _allOptions.GetOptionFromKey((string)item);

                if (option == null)
                {
                    return (string)item;   // no matching object, just show the key
                }

                return option.Name.GetBestAlternative(_preferredWritingSystemId);
            }

            #endregion

            //other delegates

            public string GetKeyFromOption(Option t)
            {
                return t.Key;
            }

            /// <summary>
            /// GetValueFromKeyValueDelegate
            /// </summary>
            /// <returns></returns>
            public Option GetOptionFromKey(string s)//review: is this the key?
            {
                Option result = _allOptions.Options.Find(delegate(Option opt)
                                                       {
                                                           return opt.Key == s;
                                                       });

               // string name = " no match";
//                if (result != null)
//                    name = result.Name.GetFirstAlternative();
                //Debug.WriteLine("GetOptionFromKey(" + s + ") = " + name);
                return result;
            }

            public Option GetOptionFromOptionRef(OptionRef oref)
            {
                return GetOptionFromKey(oref.Key);                
            }

            public void UpdateKeyContainerFromKeyValue(Option kv, OptionRef oRef)
            {
                oRef.Key = kv.Key;
            }

        public IDisplayStringAdaptor ToolTipAdaptor
        {
            get
            {
                if (_toolTipAdaptor == null)
                {
                    //_toolTipAdaptor = new 
                }
                return _toolTipAdaptor;
                
            }
        }

        public string GetValueFromKeyValue(Option kv)
        {
            return GetKeyFromOption(kv);
        }

        public Option GetKeyValueFromKey_Container(OptionRef kc)
        {
            return GetOptionFromOptionRef(kc);
        }

        #region IChoiceSystemAdaptor<Option,string,OptionRef> Members

        public Option GetKeyValueFromValue(string t)
        {
            return GetOptionFromKey(t);
        }

        public string GetValueFromForm(string form)
        {
            foreach (Option item in this._allOptions.Options)
            {
                if (GetDisplayLabel(item) == form)
                {
                    return item.Key;
                }
            }
            return null;
        }

        #endregion
        }
}