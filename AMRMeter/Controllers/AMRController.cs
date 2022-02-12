using AMRMeter.DB;
using AMRMeter.Filters;
using AMRMeter.Models;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Mvc;

namespace AMRMeter.Controllers
{
    [BasicAuthentication]
    //[RequireHttps]
    public class AMRController : ApiController
    {
        ILog log = log4net.LogManager.GetLogger(typeof(AMRController));
        DBOperations db = new DBOperations();
        Login UserLogin = new Login();
        SMSSerice.mainservice SMS = new SMSSerice.mainservice();
        string saveResult;

        [System.Web.Http.HttpGet]
        public HttpResponseMessage AppVersion()
        {
            string AppVersion = db.getAppVersion();
            if (!string.IsNullOrEmpty(AppVersion))
            {
                var resp = new HttpResponseMessage()
                {
                    Content = new StringContent("[{\"AppVersion\":\"" + AppVersion + "\"}]")
                };
                return resp;
            }
            else
            {
                var resp = new HttpResponseMessage()
                {
                    Content = new StringContent("[{\"OTP\":\"Invalid App Version\"}]")
                };
                return resp;
            }
        }

        [System.Web.Http.HttpGet]
        public HttpResponseMessage Login(string MobileNo)
        {
            try
            {
                log.DebugFormat("MobileNo {0}", MobileNo);
                if (db.CheckMobileNo(MobileNo))
                {
                    string OTP;
                    if (MobileNo.Equals("8686863442"))
                        OTP = "1234";
                    else
                        OTP = db.generate_random();
                    if (db.SaveOTP(MobileNo, OTP))
                    {
                        //login.Add(new Login { AgencyName = db.AgencyName, AgencyMobileNo = db.AgencyMobileNo, OTP = OTP });
                        //SMS.SendOTP(MobileNo, "Your Login OTP is : " + OTP + ". HMSSB", "1307161174773856452");
                        var objLogin = new
                        {
                            Result = new
                            {
                                StatusCode = 1,
                                Success = true,
                                StatusMessage = "OTP Sent Successfully"
                            },
                            Details = new List<Login>
                            {
                                new Login {AgencyName = db.AgencyName,AgencyMobileNo = db.AgencyMobileNo,OTP = OTP }
                            }
                        };
                        //SMS.SendOTP(MobileNo, "Your Login OTP is : " + OTP + ". HMSSB", "1307161174773856452");
                        //SMS.SendOTP(MobileNo, "[#] Dear Registered User,\nyour Passcode: " + OTP + "\nregards,\nHMWSSB,\n1ba03SGGQKr", "1307161174121979772"); /* Live hash key */
                        SMS.SendOTP(MobileNo, "[#] " + OTP + " Use this OTP for AMR Preventive Maintanance App Login, regards, HMWSSB, ZKhMUURR6l9", "1307164015556618224"); /* Live hash key */
                        //SMS.SendOTP(MobileNo, "[#] " + OTP + " Use this OTP for AMR Preventive Maintanance App Login,\nregards,\nHMWSSB,\nZKhMUURR6l9", "1307164015556618224"); /* Live hash key */
                        return Request.CreateResponse(HttpStatusCode.OK, objLogin);
                    }
                    else
                    {
                        var objLogin = new
                        {
                            Result = new
                            {
                                StatusCode = -1,
                                Success = false,
                                StatusMessage = "OTP Sending Failed.Please try again"
                            },
                            Details = new
                            {

                            }
                        };
                        return Request.CreateResponse(HttpStatusCode.OK, objLogin);
                    }
                }
                else
                {
                    var objLogin = new
                    {
                        Result = new
                        {
                            StatusCode = -1,
                            Success = false,
                            StatusMessage = "Mobile Number Not Registered"
                        },
                        Details = new { }
                    };
                    return Request.CreateResponse(HttpStatusCode.NotFound, objLogin);
                }
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Exception {0}", ex.ToString());
                throw ex;
            }
        }

        [System.Web.Http.HttpGet]
        public HttpResponseMessage GetCanDetailsByAgency(string MobileNo)
        {
            try
            {
                if (!string.IsNullOrEmpty(MobileNo))
                {
                    var Cans = db.GetCanDetailsByAgency(MobileNo);
                    if (Cans != null)
                        return Request.CreateResponse(HttpStatusCode.OK, Cans);
                    else
                        return Request.CreateResponse(HttpStatusCode.OK, "No Cans Found");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Invalid Mobile Number");
                }
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Exception {0}", ex.ToString());
                throw ex;
            }
        }

        [System.Web.Http.HttpGet]
        public HttpResponseMessage GetDashboardSummary(string MobileNo)
        {
            try
            {
                if (!string.IsNullOrEmpty(MobileNo))
                {
                    var Counts = db.GetDashboardSummary(MobileNo);
                    if (Counts != null)
                        return Request.CreateResponse(HttpStatusCode.OK, Counts);
                    else
                        return Request.CreateResponse(HttpStatusCode.OK, "No Cans Found");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Invalid Mobile Number");
                }
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Exception {0}", ex.ToString());
                throw ex;
            }
        }

        [System.Web.Http.HttpGet]
        public HttpResponseMessage GetCanStatusDetails(string CAN, string MobileNo)
        {
            try
            {
                if (!string.IsNullOrEmpty(CAN) && !string.IsNullOrEmpty(MobileNo))
                {
                    var Counts = db.GetCanDetailswithStatus(CAN, MobileNo);
                    if (Counts != null)
                        return Request.CreateResponse(HttpStatusCode.OK, Counts);
                    else
                        return Request.CreateResponse(HttpStatusCode.OK, "No Cans Found");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Invalid CAN/Mobile Number");
                }
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Exception {0}", ex.ToString());
                throw ex;
            }
        }

        [System.Web.Http.HttpGet]
        public HttpResponseMessage GetCanDetailsByStatus(string InspectionStatus, string MobileNo, int StatusFlag)
        {
            try
            {
                if (!string.IsNullOrEmpty(InspectionStatus) && !string.IsNullOrEmpty(MobileNo))
                {
                    var Cans = db.GetCanDetailsByStatus(InspectionStatus, MobileNo, StatusFlag);
                    if (Cans != null)
                        return Request.CreateResponse(HttpStatusCode.OK, Cans);
                    else
                        return Request.CreateResponse(HttpStatusCode.OK, "No Cans Found");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Invalid CAN/Mobile Number");
                }
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Exception {0}", ex.ToString());
                throw ex;
            }
        }

        //[System.Web.Http.HttpGet]
        //public HttpResponseMessage GetCanDetailsByStatus(string InspectionStatus, string MobileNo)
        //{
        //    try
        //    {
        //        if (!string.IsNullOrEmpty(InspectionStatus) && !string.IsNullOrEmpty(MobileNo))
        //        {
        //            var Cans = db.GetCanDetailsByStatus(InspectionStatus, MobileNo);
        //            if (Cans != null)
        //            {
        //                var objLogin = new
        //                {
        //                    Result = new
        //                    {
        //                        StatusCode = 1,
        //                        Success = true,
        //                        StatusMessage = InspectionStatus + " Records"
        //                    },
        //                    Details = new
        //                    {
        //                        Cans
        //                    }
        //                };
        //                return Request.CreateResponse(HttpStatusCode.OK, objLogin);
        //            }
        //            //return Request.CreateResponse(HttpStatusCode.OK, Cans);

        //            else
        //                return Request.CreateResponse(HttpStatusCode.OK, "No Cans Found");
        //        }
        //        else
        //        {
        //            return Request.CreateResponse(HttpStatusCode.NotFound, "Invalid CAN/Mobile Number");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        log.ErrorFormat("Exception {0}", ex.ToString());
        //        throw ex;
        //    }
        //}

        [System.Web.Http.HttpPost]
        public HttpResponseMessage CapturePreventiveMaintanance(PreventiveMaintanance JsonRequest)
        {
            try
            {
                log.InfoFormat("CAN {0}", JsonRequest.CAN);
                log.InfoFormat("Submitted By {0}", JsonRequest.INSPECTIONBY);
                log.InfoFormat("JsonRequest {0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}" +
                    "{12} {13} {14} {15} {16}", "CAN: " + JsonRequest.CAN, ", ISDISPLAYWORKING: " + JsonRequest.ISDISPLAYWORKING, ", ISFOGPRESENTINSIDEMETERE: " + JsonRequest.ISFOGPRESENTINSIDEMETERE
                  , ", ISANYMETERREPAIRWORKS: " + JsonRequest.ISANYMETERREPAIRWORKS, ", ISMETERPRESENT: " + JsonRequest.ISMETERPRESENT, ", ISMETERSUBMERGED: " + JsonRequest.ISMETERSUBMERGED
                  , ", ISMETERCHAMBERPRESENT: " + JsonRequest.ISMETERCHAMBERPRESENT, ", ISANYLEAKAGESPRESENT: " + JsonRequest.ISANYLEAKAGESPRESENT, ", ISPIPESIZESAME: " + JsonRequest.ISPIPESIZESAME
                  , ", CANLIVESTATUS: " + JsonRequest.CANLIVESTATUS, ", REMARKS: " + JsonRequest.REMARKS, ", METERIMGLATITUDE: " + JsonRequest.METERIMGLATITUDE
                  , ", METERIMGLONGITUDE: " + JsonRequest.METERIMGLONGITUDE, ", INSPECTIONBY: " + JsonRequest.INSPECTIONBY, ", INSPECTIONSTATUS: " + JsonRequest.INSPECTIONSTATUS
                  , ", APPVERSION: " + JsonRequest.APPVERSION, ", LASTBREAKPOINT: " + JsonRequest.LASTBREAKPOINT, ", QUARTERTYPE: " + JsonRequest.QUARTERTYPE);

                saveResult = string.Empty;

                bool response = db.CapturePreventiveMaintananceDetails(JsonRequest);
                log.InfoFormat("Save Response {0}", response);
                if (response)
                {
                    var objSuccess = new
                    {
                        Result = new
                        {
                            StatusCode = 1,
                            Success = true,
                            StatusMessage = "Details Captured Successfully"
                        }
                    };
                    return Request.CreateResponse(HttpStatusCode.OK, objSuccess);
                }
                else
                {
                    var objFailed = new
                    {
                        Result = new
                        {
                            StatusCode = -1,
                            Success = false,
                            StatusMessage = "Failed"
                        }
                    };
                    return Request.CreateResponse(HttpStatusCode.OK, objFailed);
                }

            }
            catch (Exception ex)
            {
                log.ErrorFormat("Exception {0} {1} {2}", ex.ToString(), JsonRequest.CAN, JsonRequest.INSPECTIONBY);
                throw ex;
            }
        }

        [System.Web.Http.HttpGet]
        //public HttpResponseMessage GetNewMeterInstallationCansList(string MobileNo)
        public HttpResponseMessage GetReplacementMeterCansListfromPreventiveMaintenance(string MobileNo)
        {
            try
            {
                if (!string.IsNullOrEmpty(MobileNo))
                {
                    //var Cans = db.GetNewMeterInstallationCansList(MobileNo);
                    var Cans = db.GetReplacementMeterCansListfromPreventiveMaintenance(MobileNo);
                    if (Cans != null)
                        return Request.CreateResponse(HttpStatusCode.OK, Cans);
                    else
                        return Request.CreateResponse(HttpStatusCode.OK, "No Cans Found");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Invalid Mobile Number");
                }
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Exception {0}", ex.ToString());
                throw ex;
            }
        }

        [System.Web.Http.HttpGet]
        public HttpResponseMessage GetNewMeterInstallationDashboardSummary(string MobileNo)
        {
            try
            {
                if (!string.IsNullOrEmpty(MobileNo))
                {
                    var Cans = db.GetNewMeterInstallationDashboardSummary(MobileNo);
                    if (Cans != null)
                        return Request.CreateResponse(HttpStatusCode.OK, Cans);
                    else
                        return Request.CreateResponse(HttpStatusCode.OK, "No Cans Found");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Invalid Mobile Number");
                }
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Exception {0}", ex.ToString());
                throw ex;
            }
        }

        [System.Web.Http.HttpGet]
        public HttpResponseMessage GetNewMeterInstallationCans(string MobileNo, int ListType)
        {
            try
            {
                if (!string.IsNullOrEmpty(MobileNo))
                {
                    var Cans = db.GetNewMeterInstallationCans(MobileNo, ListType);
                    if (Cans != null)
                        return Request.CreateResponse(HttpStatusCode.OK, Cans);
                    else
                        return Request.CreateResponse(HttpStatusCode.OK, "No Cans Found");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Invalid Mobile Number");
                }
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Exception {0}", ex.ToString());
                throw ex;
            }
        }

        [System.Web.Http.HttpPost]
        public HttpResponseMessage CaptureNewMeterInstallationDetails(NewMeterInstallation JsonRequest)
        {
            try
            {
                log.InfoFormat("CAN {0}", JsonRequest.CAN);
                log.InfoFormat("Submitted By {0}", JsonRequest.CAPTUREDBY);
                log.InfoFormat("JsonRequest {0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}" +
                    "{12} {13} {14} {15} {16} ", "CAN: " + JsonRequest.CAN, ", NEWMETERNUMBER: " + JsonRequest.NEWMETERNUMBER, ", INITIALMETERREADING: " + JsonRequest.INITIALMETERREADING
                  , ", OLDMETERREADING: " + JsonRequest.OLDMETERREADING, ", PIPESIZE: " + JsonRequest.PIPESIZE, ", METERFIXATIONDATE: " + JsonRequest.METERFIXATIONDATE
                  , ", METERREMOVALDATE: " + JsonRequest.METERREMOVALDATE, ", METERTYPE: " + JsonRequest.METERTYPE, ", METERMAKE: " + JsonRequest.METERMAKE
                  , ", EXISTINGMETERCONDITION: " + JsonRequest.EXISTINGMETERCONDITION, ", ISCHAMBEREXISTS: " + JsonRequest.ISCHAMBEREXISTS
                  , ", LATITUDE: " + JsonRequest.LATITUDE, ", LONGITUDE: " + JsonRequest.LONGITUDE, ", CAPTUREDBY: " + JsonRequest.CAPTUREDBY
                  , ", QUARTERTYPE: " + JsonRequest.QUARTERTYPE, ", APPVERSION: " + JsonRequest.APPVERSION, ", ISOLDMETEREXISTS: " + JsonRequest.ISOLDMETEREXISTS
                  , ", METERSTATUSFLAG: " + JsonRequest.METERSTATUSFLAG, ", CAPTUREDAGENCYNAME: " + JsonRequest.CAPTUREDAGENCYNAME);

                saveResult = string.Empty;

                bool response = db.CaptureNewMeterInstallation(JsonRequest);
                log.InfoFormat("Save Response {0}", response);
                if (response)
                {
                    //db.ManagerMobileNo
                    string smsResponse = SMS.CDAC_SMS_New(db.ManagerMobileNo, "Can '" + JsonRequest.CAN + "' has been fixed with a new AMR meter by agency " + JsonRequest.CAPTUREDAGENCYNAME + " on " + JsonRequest.METERFIXATIONDATE + " , Reading " + JsonRequest.INITIALMETERREADING + ". Please do the inspection and approve the installed meter. --HMWSSB", "1307164008795107632");
                    if (!smsResponse.Contains("402"))
                        SMS.CDAC_SMS(db.ManagerMobileNo, "Can '" + JsonRequest.CAN + "' has been fixed with a new AMR meter by agency " + JsonRequest.CAPTUREDAGENCYNAME + " on " + JsonRequest.METERFIXATIONDATE + " , Reading " + JsonRequest.INITIALMETERREADING + ". Please do the inspection and approve the installed meter. --HMWSSB", "1307164008795107632");
                    //string smsResponse = SMS.CDAC_SMS_New(db.ManagerMobileNo, "Your Login OTP is : 123456. HMWSSB", "1307163619370512492");
                    //if (!smsResponse.Contains("402"))
                    //    SMS.CDAC_SMS(db.ManagerMobileNo, "Your Login OTP is : 123456. HMWSSB", "1307163619370512492");
                    var objSuccess = new
                    {
                        Result = new
                        {
                            StatusCode = 1,
                            Success = true,
                            StatusMessage = "New Meter Installed Successfully"
                        }
                    };
                    return Request.CreateResponse(HttpStatusCode.OK, objSuccess);
                }
                else
                {
                    var objFailed = new
                    {
                        Result = new
                        {
                            StatusCode = -1,
                            Success = false,
                            StatusMessage = "Failed"
                        }
                    };
                    return Request.CreateResponse(HttpStatusCode.OK, objFailed);
                }

            }
            catch (Exception ex)
            {
                log.ErrorFormat("Exception {0} {1} {2}", ex.ToString(), JsonRequest.CAN, JsonRequest.CAPTUREDBY);
                throw ex;
            }
        }

        [System.Web.Http.HttpGet]
        public HttpResponseMessage GetCansListforCaptureReading(string MobileNo)
        {
            try
            {
                if (!string.IsNullOrEmpty(MobileNo))
                {
                    var Cans = db.GetCansListforCaptureReading(MobileNo);
                    if (Cans != null)
                        return Request.CreateResponse(HttpStatusCode.OK, Cans);
                    else
                        return Request.CreateResponse(HttpStatusCode.OK, "No Cans Found");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Invalid Mobile Number");
                }
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Exception {0}", ex.ToString());
                throw ex;
            }
        }

        [System.Web.Http.HttpGet]
        public HttpResponseMessage GetReviewCansListforCaptureReading(string MobileNo)
        {
            try
            {
                if (!string.IsNullOrEmpty(MobileNo))
                {
                    var Cans = db.GetReviewCansListforCaptureReading(MobileNo);
                    if (Cans != null)
                        return Request.CreateResponse(HttpStatusCode.OK, Cans);
                    else
                        return Request.CreateResponse(HttpStatusCode.OK, "No Cans Found");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Invalid Mobile Number");
                }
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Exception {0}", ex.ToString());
                throw ex;
            }
        }

        [System.Web.Http.HttpPost]
        public HttpResponseMessage CaptureMeterReadingDetails(CaptureMeterReading JsonRequest)
        {
            try
            {
                log.InfoFormat("CAN {0}", JsonRequest.CAN);
                log.InfoFormat("Submitted By {0}", JsonRequest.CAPTUREDBY);
                log.InfoFormat("JsonRequest {0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}" +
                    "{12} {13}", "CAN: " + JsonRequest.CAN, ", CATEGORY: " + JsonRequest.CATEGORY, ", PREVIOUSMETERREADING: " + JsonRequest.PREVIOUSMETERREADING
                  , ", PREVIOUSMETERREADINGDATE: " + JsonRequest.PREVIOUSMETERREADINGDATE, ", PREVIOUSCONSUMPTIONUNITS: " + JsonRequest.PREVIOUSCONSUMPTIONUNITS, ", PRESENTMETERREADING: " + JsonRequest.PRESENTMETERREADING
                  , ", PRESENTCONSUMPTIONUNITS: " + JsonRequest.PRESENTCONSUMPTIONUNITS, ", MTRIMGLATITUDE: " + JsonRequest.MTRIMGLATITUDE
                  , ", MTRIMGLONGITUDE: " + JsonRequest.MTRIMGLONGITUDE, ", QUARTERTYPE: " + JsonRequest.QUARTERTYPE, ", CAPTUREDBY: " + JsonRequest.CAPTUREDBY
                  , ", REMARKS: " + JsonRequest.REMARKS, ", APPVERSION: " + JsonRequest.APPVERSION, ", METERSTATUS: " + JsonRequest.METERSTATUS);

                saveResult = string.Empty;
                if (!JsonRequest.MTRIMGLATITUDE.Equals("0") && !JsonRequest.MTRIMGLONGITUDE.Equals("0"))
                {
                    bool response = db.CaptureMeterReadingDetails(JsonRequest);
                    log.InfoFormat("Save Response {0}", response);
                    if (response)
                    {
                        var objSuccess = new
                        {
                            Result = new
                            {
                                StatusCode = 1,
                                Success = true,
                                StatusMessage = "Meter Reading Captured Successfully"
                            }
                        };
                        return Request.CreateResponse(HttpStatusCode.OK, objSuccess);
                    }
                    else
                    {
                        var objFailed = new
                        {
                            Result = new
                            {
                                StatusCode = -1,
                                Success = false,
                                StatusMessage = "Failed"
                            }
                        };
                        return Request.CreateResponse(HttpStatusCode.OK, objFailed);
                    }
                }
                else
                {
                    var objFailed = new
                    {
                        Result = new
                        {
                            StatusCode = -1,
                            Success = false,
                            StatusMessage = "GPS Co-ordinates not found"
                        }
                    };
                    return Request.CreateResponse(HttpStatusCode.OK, objFailed);
                }
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Exception {0} {1} {2}", ex.ToString(), JsonRequest.CAN, JsonRequest.CAPTUREDBY);
                throw ex;
            }
        }

    }
}
