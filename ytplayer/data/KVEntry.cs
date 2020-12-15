using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer.data {
    [Table(Name = "t_map")]
    public class KVEntry : IEntry {
        [Column(Name = "name", CanBeNull = false, IsPrimaryKey = true)]
        public string KEY { get;  private set; }

        [Column(Name = "ivalue", CanBeNull = true)]
        public int iValue;

        [Column(Name = "svalue", CanBeNull = true)]
        public string sValue;
    }


}
