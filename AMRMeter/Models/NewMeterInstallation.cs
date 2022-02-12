using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AMRMeter.Models
{
    public class NewMeterInstallation
    {
        public string CAN { get; set; }
        public string NEWMETERNUMBER { get; set; }
        public Int32 INITIALMETERREADING { get; set; }
        public Int32 OLDMETERREADING { get; set; }
        public int PIPESIZE { get; set; }
        public DateTime METERFIXATIONDATE { get; set; }
        public DateTime METERREMOVALDATE { get; set; }
        public string METERTYPE { get; set; }
        public string METERMAKE { get; set; }
        public string EXISTINGMETERCONDITION { get; set; }
        public string ISCHAMBEREXISTS { get; set; }
        public string METERIMAGPATH { get; set; }
        public string LATITUDE { get; set; }
        public string LONGITUDE { get; set; }
        public string CAPTUREDBY { get; set; }
        public string QUARTERTYPE { get; set; }
        public string REMARKS { get; set; }
        public string APPVERSION { get; set; }

        public string ISOLDMETEREXISTS { get; set; }
        public string METERSTATUSFLAG { get; set; }
        public string OLDMETERIMAGPATH { get; set; }
        public string CAPTUREDAGENCYNAME { get; set; }
    }
}