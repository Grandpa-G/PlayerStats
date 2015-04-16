using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerStats
{
    class Prefixs
    {
        public Prefixs()
        {

        }
        public Prefixs(string name, int prefix)
        {
            this.Name = name;
            this.Prefix = prefix;
        }

        private string name;
        private int prefix;

        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }
        public int Prefix
        {
            get { return this.prefix; }
            set { this.prefix = value; }
        }

    }
    class Item
    {
        public Item()
        {
            this.Name = "";
            this.NetId = 0;
            this.StackSize = 0;
            this.Prefix = 0;
            this.ItemCount = 0;
            this.ItemQuantity = 0;
            this.PrefixCount = 0;
        }

        public Item(string name)
        {
            this.Name = name;
        }

        public Item(string name, int netId, int stackSize, int prefix, int itemcount, int itemquantity, int prefixcount)
        {
            this.Name = name;
            this.NetId = netId;
            this.StackSize = stackSize;
            this.Prefix = prefix;
            this.ItemCount = itemcount;
            this.ItemQuantity = itemquantity;
            this.PrefixCount = prefixcount;
        }

        private string name;
        private int stackSize;
        private int netId;
        private int prefix;
        private int itemcount;
        private int itemquantity;
        private int prefixcount;

        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        public int StackSize
        {
            get { return this.stackSize; }
            set { this.stackSize = value; }
        }
        public int NetId
        {
            get { return this.netId; }
            set { this.netId = value; }
        }
        public int Prefix
        {
            get { return this.prefix; }
            set { this.prefix = value; }
        }
        public int ItemCount
        {
            get { return this.itemcount; }
            set { this.itemcount = value; }
        }
        public int ItemQuantity
        {
            get { return this.itemquantity; }
            set { this.itemquantity = value; }
        }
        public int PrefixCount
        {
            get { return this.prefixcount; }
            set { this.prefixcount = value; }
        }
    }
}
