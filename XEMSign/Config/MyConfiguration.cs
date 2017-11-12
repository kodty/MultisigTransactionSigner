using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Configuration;
namespace XEMSign
{
    public class MyConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("", IsRequired = true, IsDefaultCollection = true)]
        public MyConfigInstanceCollection Instances
        {
            get { return (MyConfigInstanceCollection)this[""]; }
            set { this[""] = value; }
        }
    }
    public class MyConfigInstanceCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new MyConfigInstanceElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            //set to whatever Element Property you want to use for a key
            return ((MyConfigInstanceElement)element).Name;
        }
    }

    public class MyConfigInstanceElement : ConfigurationElement
    {
        //Make sure to set IsKey=true for property exposed as the GetElementKey above
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name
        {
            get { return (string)base["name"]; }
            set { base["name"] = value; }
        }

        [ConfigurationProperty("code", IsRequired = true)]
        public string Code
        {
            get { return (string)base["code"]; }
            set { base["code"] = value; }
        }
    }

    public class MyMosaicConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("", IsRequired = true, IsDefaultCollection = true)]
        public MosaicInstanceCollection Mosaics
        {
            get { return (MosaicInstanceCollection)this[""]; }
            set { this[""] = value; }
        }

    }
    public class MosaicInstanceCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new MosaicConfigElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            //set to whatever Element Property you want to use for a key
            return ((MosaicConfigElement)element).Name;
        }
    }
    public class MosaicConfigElement : ConfigurationElement
    {
        //Make sure to set IsKey=true for property exposed as the GetElementKey above
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name
        {
            get { return (string)base["name"]; }
            set { base["name"] = value; }
        }
        [ConfigurationProperty("mosaicID", IsRequired = true)]
        public string MosaicID
        {
            get { return (string)base["mosaicID"]; }
            set { base["mosaicID"] = value; }
        }
        [ConfigurationProperty("mosaicNameSpace", IsRequired = true)]
        public string MosaicNameSpace
        {
            get { return (string)base["mosaicNameSpace"]; }
            set { base["mosaicNameSpace"] = value; }
        }
        [ConfigurationProperty("mosaicCost", IsRequired = true)]
        public string MosaicCost
        {
            get { return (string)base["mosaicCost"]; }
            set { base["mosaicCost"] = value; }
        }
        [ConfigurationProperty("costDenomination", IsRequired = true)]
        public string CostDenomination
        {
            get { return (string)base["costDenomination"]; }
            set { base["costDenomination"] = value; }
        }
    }

    public class MyBonusConfigSection : ConfigurationSection
    {

        [ConfigurationProperty("", IsRequired = true, IsDefaultCollection = true)]
        public BonusInstanceCollection Bonuses
        {
            get { return (BonusInstanceCollection)this[""]; }
            set { this[""] = value; }
        }
    }
    public class BonusInstanceCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new MosaicBonusConfigElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            //set to whatever Element Property you want to use for a key
            return ((MosaicBonusConfigElement)element).Name;
        }
    }

    public class MosaicBonusConfigElement : ConfigurationElement
    {
        //Make sure to set IsKey=true for property exposed as the GetElementKey above
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name
        {
            get { return (string)base["name"]; }
            set { base["name"] = value; }
        }

        [ConfigurationProperty("tokenAssignedTo", IsKey = true, IsRequired = true)]
        public string TokenAssignedTo
        {
            get { return (string)base["tokenAssignedTo"]; }
            set { base["tokenAssignedTo"] = value; }
        }

        [ConfigurationProperty("startDateTime", IsKey = true, IsRequired = true)]
        public string StartDateTime
        {
            get { return (string)base["startDateTime"]; }
            set { base["startDateTime"] = value; }
        }

        [ConfigurationProperty("endDateTime", IsKey = true, IsRequired = true)]
        public string EndDateTime
        {
            get { return (string)base["endDateTime"]; }
            set { base["endDateTime"] = value; }
        }

        [ConfigurationProperty("bonusPercent", IsKey = true, IsRequired = true)]
        public string BonusPercent
        {
            get { return (string)base["bonusPercent"]; }
            set { base["bonusPercent"] = value; }
        }

    }
}
