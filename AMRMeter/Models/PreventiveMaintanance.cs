using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AMRMeter.Models
{
    public class PreventiveMaintanance
    {
        public string CAN { get; set; }
        public string ISDISPLAYWORKING { get; set; }
        public string ISFOGPRESENTINSIDEMETERE { get; set; }
        public string ISANYMETERREPAIRWORKS { get; set; }
        public string ISMETERPRESENT { get; set; }
        public string ISMETERSUBMERGED { get; set; }
        public string ISMETERCHAMBERPRESENT { get; set; }
        public string ISANYLEAKAGESPRESENT { get; set; }
        public string ISPIPESIZESAME { get; set; }
        public string CANLIVESTATUS { get; set; }
        public string REMARKS { get; set; }
        public string METERIMGPATH { get; set; }
        public string METERIMGLATITUDE { get; set; }
        public string METERIMGLONGITUDE { get; set; }
        public string INSPECTIONBY { get; set; }
        public string INSPECTIONSTATUS { get; set; }
        public string APPVERSION { get; set; }
        public string LASTBREAKPOINT { get; set; }
        public string QUARTERTYPE { get; set; }
        public string ISMETERREPAIREDORREPLACED { get; set; }
    }
}