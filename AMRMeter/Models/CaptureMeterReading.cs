using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AMRMeter.Models
{
    public class CaptureMeterReading
    {
        public string CAN { get; set; }
        public string CATEGORY { get; set; }
        public Int32 PREVIOUSMETERREADING { get; set; }
        public DateTime PREVIOUSMETERREADINGDATE { get; set; }
        public Int32 PREVIOUSCONSUMPTIONUNITS { get; set; }
        public Int32 PRESENTMETERREADING { get; set; }
        public Int32 PRESENTCONSUMPTIONUNITS { get; set; }
        public string MTRIMGPATH { get; set; }
        public string MTRIMGLATITUDE { get; set; }
        public string MTRIMGLONGITUDE { get; set; }
        public string REFERENCENO { get; set; }
        public string QUARTERTYPE { get; set; }
        public string CAPTUREDBY { get; set; }
        public string REMARKS { get; set; }
        public string APPVERSION { get; set; }
        public string METERSTATUS { get; set; }
    }
}