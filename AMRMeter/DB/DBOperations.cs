using AMRMeter.Models;
using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OracleClient;
using System.IO;
using System.Linq;
using System.Web;

namespace AMRMeter.DB
{
    public class DBOperations
    {
        OracleConnection con = new OracleConnection(ConfigurationManager.ConnectionStrings["Con"].ConnectionString);
        OracleCommand cmd;
        Login login = new Login();
        int cnt, refSlno, saveResult;
        public string AgencyName, AgencyMobileNo, ReferenceNo, NewMeterReferenceNo, captureMtrReadingReferenceNo;
        string referenceNo, meterImgPath, newMeterImgPath, oldMeterImgPath, captureMtrReadingImgPath;
        DataTable dt;
        string refMonthid = DateTime.Now.ToString("MMM-yyyy");
        ILog log = log4net.LogManager.GetLogger(typeof(DBOperations));
        String path, imageName, imgPath;
        private string whereCondition;
        public string ManagerMobileNo { get; set; }

        public string getAppVersion()
        {
            dt = GetData("SELECT VERSION FROM TBL_AMR_APPVERSION WHERE ISACTIVE='Y'");
            if (dt.Rows.Count > 0)
                return dt.Rows[0]["VERSION"].ToString();
            else
                return string.Empty;
        }

        public bool CheckMobileNo(string pMobileNo)
        {
            try
            {
                con.Open();
                //cmd = new OracleCommand(@"SELECT DISTINCT AGENCY_MOBILENO,AGENCY_NAME,COUNT(*)CANS FROM
                //                        (SELECT * FROM TBL_AMR_AGENCY_USERS USR JOIN VWGETAMRAGENCYFLAGGEDCANS CAN
                //                        ON CAN.AGENCYTYPE=USR.AGENCY_NAME WHERE AGENCY_MOBILENO=:pMOBILENO  AND USR.ISACTIVE='Y') 
                //                        GROUP BY AGENCY_MOBILENO,AGENCY_NAME", con);
                cmd = new OracleCommand(@"SELECT AGENCY_MOBILENO,AGENCY_NAME FROM TBL_AMR_AGENCY_USERS WHERE AGENCY_MOBILENO=:pMOBILENO AND ISACTIVE='Y'", con);
                OracleParameter param1 = new OracleParameter();
                param1.ParameterName = ":pMOBILENO";
                param1.Value = pMobileNo;
                cmd.Parameters.Add(param1);
                OracleDataReader reader = cmd.ExecuteReader();
                cnt = 0;
                while (reader.Read())
                {
                    cnt++;
                    AgencyName = reader["AGENCY_NAME"].ToString();
                    AgencyMobileNo = reader["AGENCY_MOBILENO"].ToString();
                }
                if (cnt != 0)
                    return true;
                else
                    return false;
            }
            catch (Exception)
            {
                throw;
            }
            finally { con.Close(); }

        }

        public bool SaveOTP(string MobileNo, string OTP)
        {
            try
            {
                con.Open();
                cmd = new OracleCommand("UPDATE TBL_AMR_AGENCY_USERS SET OTP=:pOTP,LOGIN_DATE=:pDATE WHERE AGENCY_MOBILENO=:pMOBILENO ", con);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add("pOTP", OracleType.VarChar).Value = OTP;
                cmd.Parameters.Add("pMOBILENO", OracleType.VarChar).Value = MobileNo;
                cmd.Parameters.Add("pDATE", OracleType.Timestamp).Value = DateTime.Now;
                cnt = cmd.ExecuteNonQuery();
                if (cnt != 0)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                con.Close();
            }
        }

        internal object GetCanDetailsByAgency(string mobileNo)
        {
            try
            {
                con.Open();
                cmd = new OracleCommand(@"SELECT AMR.CAN,MSC.FIRSTNAME||MSC.MIDDLENAME||MSC.LASTNAME CONSUMER_NAME,SC.SPGHMCHNO||','||SC.SPSTREET||','||SC.SPAREA||','||SC.SPPINCODE 
                           CONSUMER_ADDRESS,SC.SPMOBILENO MOBILE_NO,AMR.DIVISIONCODE,AMR.SECTIONNAME,SC.RBSTCATEGORYID CATEGORY,WS.DESCRIPTION PIPE_SIZE,AMR.QUARTERTYPE, MCC.LATITUDE,MCC.lONGITUDE
                           FROM TBL_AMR_AGENCY_USERS USR JOIN TBL_AMR_AGENCYFLAGGEDCANS AMR ON AMR.AGENCYTYPE=USR.AGENCY_NAME INNER JOIN CTLHMWSSBTMP.MDM_SHR_SERVICECONN SC ON SC.CAN=AMR.CAN
                           INNER JOIN CTLHMWSSBTMP.MDM_SHR_CONSUMER MSC ON MSC.PKEY=SC.CONSUMERPKEY INNER JOIN CTLHMWSSBTMP.MDM_SHR_WATERSIZE WS ON WS.CODE=SC.WATERSIZEPKEY
                           LEFT JOIN CTLHMWSSBTMP.EIF_MCC_CANS_LAT_LONG MCC ON MCC.CAN=AMR.CAN WHERE AGENCY_MOBILENO=:pMobileNo AND USR.ISACTIVE='Y'
                           AND AMR.QUARTERTYPE='Quarter1' AND AMR.CAN NOT IN (SELECT CAN FROM TBL_AMR_PREVENTIVE_MAINTANANCE 
                           WHERE INSPECTIONSTATUS NOT IN ('inspectiondone') AND QUARTERTYPE='Quarter1')", con);
                OracleParameter param1 = new OracleParameter();
                param1.ParameterName = ":pMobileNo";
                param1.Value = mobileNo;
                cmd.Parameters.Add(param1);
                OracleDataReader reader = cmd.ExecuteReader();
                dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                con.Close();
            }
        }

        internal object GetDashboardSummary(string mobileNo)
        {
            try
            {
                con.Open();
                //cmd = new OracleCommand(@"select t2.target,t3.pending,t1.* from(select sum(case when inspectionstatus='vendorissue' then 1 else 0 end )vendorissue,
                //                        sum(case when inspectionstatus='customerissue' then 1 else 0 end )customerissue,
                //                        sum(case when inspectionstatus='inspectiondone' then 1 else 0 end )inspectiondone,
                //                        --sum(case when inspectionstatus in ('customerissue','vendorissue') then 1 else 0 end )underrepair,
                //                        sum(case when inspectionstatus in ('nometer') then 1 else 0 end )nometer,
                //                        sum(case when inspectionstatus in ('inspectiondone') and trunc(inspectiondate)-trunc(captureddate)<=2
                //                        and can in (select can from tbl_amr_vendor_issues where quartertype='Quarter1' and capturedby=:pMobileNo) then 1 else 0 end) withinsla,
                //                        sum(case when inspectionstatus in ('inspectiondone') and trunc(inspectiondate)-trunc(captureddate)>2
                //                        and can in (select can from tbl_amr_vendor_issues where quartertype='Quarter1' and capturedby=:pMobileNo) then 1 else 0 end) beyondsla
                //                        from tbl_amr_preventive_maintanance where inspectionby=:pMobileNo and quartertype='Quarter1')t1,(select count(*)target from tbl_amr_agencyflaggedcans a 
                //                        join tbl_amr_agency_users b on a.agencytype=b.agency_name where agency_mobileno=:pMobileNo and quartertype='Quarter1')t2,
                //                        (select count(*)pending from tbl_amr_agencyflaggedcans a 
                //                        join tbl_amr_agency_users b on a.agencytype=b.agency_name where agency_mobileno=:pMobileNo and quartertype='Quarter1' and can not in (select can from tbl_amr_preventive_maintanance where inspectionby=:pMobileNo and quartertype='Quarter1'))t3", con);

                cmd = new OracleCommand(@"select t2.target,t3.pending,
                                          --sum(case when t1.vendorissue is not null then 1 else 0 end) vendorissue,
                                          --sum(case when t1.customerissue is not null then 1 else 0 end) customerissue,
                                          --sum(case when t1.inspectiondone is not null then 1 else 0 end) inspectiondone,
                                          --sum(case when t1.nometer is not null then 1 else 0 end) nometer,
                                          --sum(case when t1.withinsla is not null then 1 else 0 end) withinsla,
                                          --sum(case when t1.beyondsla is not null then 1 else 0 end) beyondsla
                                           t1.*
                                         from(select sum(case when inspectionstatus='vendorissue' then 1 else 0 end )vendorissue,
                                         sum(case when inspectionstatus='customerissue' then 1 else 0 end )customerissue,
                                         sum(case when inspectionstatus='inspectiondone' then 1 else 0 end )inspectiondone,
                                        --sum(case when inspectionstatus in ('customerissue','vendorissue') then 1 else 0 end )underrepair,
                                         sum(case when inspectionstatus in ('nometer') then 1 else 0 end )nometer,
                                         sum(case when inspectionstatus in ('inspectiondone') 
                                         and can in (select can from tbl_amr_vendor_issues where quartertype='Quarter1' and capturedby=:pMobileNo 
                                         UNION 
                                         select can from tbl_amr_customer_issues where quartertype='Quarter1' and capturedby=:pMobileNo
                                         UNION 
                                         select can from tbl_amr_nometer where quartertype='Quarter1' and capturedby=:pMobileNo)
                                         and trunc(inspectiondate)-trunc(captureddate)<=2
                                         then 1 else 0 end) withinsla,
                                         sum(case when inspectionstatus in ('inspectiondone')
                                         and can in (select can from tbl_amr_vendor_issues where quartertype='Quarter1' and capturedby=:pMobileNo 
                                         UNION 
                                         select can from tbl_amr_customer_issues where quartertype='Quarter1' and capturedby=:pMobileNo
                                         UNION 
                                         select can from tbl_amr_nometer where quartertype='Quarter1' and capturedby=:pMobileNo)
                                         and trunc(inspectiondate)-trunc(captureddate)>2 then 1 else 0 end) beyondsla
                                         from tbl_amr_preventive_maintanance where inspectionby=:pMobileNo and quartertype='Quarter1')t1,(select count(*)target from tbl_amr_agencyflaggedcans a 
                                         join tbl_amr_agency_users b on a.agencytype=b.agency_name where agency_mobileno=:pMobileNo and quartertype='Quarter1')t2,
                                         (select count(*)pending from tbl_amr_agencyflaggedcans a 
                                         join tbl_amr_agency_users b on a.agencytype=b.agency_name where agency_mobileno=:pMobileNo and quartertype='Quarter1' and can not in (select can from tbl_amr_preventive_maintanance where inspectionby=:pMobileNo and quartertype='Quarter1'))t3", con);

                OracleParameter param1 = new OracleParameter();
                param1.ParameterName = ":pMobileNo";
                param1.Value = mobileNo;
                cmd.Parameters.Add(param1);
                OracleDataReader reader = cmd.ExecuteReader();
                dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                con.Close();
            }
        }

        internal object GetCanDetailswithStatus(string can, string mobileNo)
        {
            try
            {
                con.Open();
                cmd = new OracleCommand(@"SELECT AMR.CAN,MSC.FIRSTNAME||MSC.MIDDLENAME||MSC.LASTNAME CONSUMER_NAME,SC.SPGHMCHNO||','||SC.SPSTREET||','||SC.SPAREA||','||SC.SPPINCODE 
                                          CONSUMER_ADDRESS,SC.SPMOBILENO MOBILE_NO,AMR.DIVISIONCODE,AMR.SECTIONNAME,SC.RBSTCATEGORYID CATEGORY,WS.DESCRIPTION PIPE_SIZE,AMR.QUARTERTYPE,
                                          CASE WHEN APM.INSPECTIONSTATUS IS NOT NULL THEN INSPECTIONSTATUS ELSE 'pending' end INSPECTIONSTATUS,MCC.LATITUDE,MCC.LONGITUDE
                                          FROM TBL_AMR_AGENCY_USERS USR JOIN TBL_AMR_AGENCYFLAGGEDCANS AMR ON AMR.AGENCYTYPE=USR.AGENCY_NAME INNER JOIN CTLHMWSSBTMP.MDM_SHR_SERVICECONN SC ON SC.CAN=AMR.CAN
                                          INNER JOIN CTLHMWSSBTMP.MDM_SHR_CONSUMER MSC ON MSC.PKEY=SC.CONSUMERPKEY INNER JOIN CTLHMWSSBTMP.MDM_SHR_WATERSIZE WS ON WS.CODE=SC.WATERSIZEPKEY
                                          LEFT JOIN TBL_AMR_PREVENTIVE_MAINTANANCE APM ON APM.CAN=AMR.CAN
                                          LEFT JOIN CTLHMWSSBTMP.EIF_MCC_CANS_LAT_LONG MCC ON MCC.CAN=AMR.CAN
                                          WHERE AGENCY_MOBILENO=:pMobileNo AND AMR.CAN=:pCAN
                                          AND USR.ISACTIVE='Y' AND AMR.QUARTERTYPE='Quarter1'", con);
                //AND APM.INSPECTIONSTATUS NOT IN('inspectiondone')", con);
                OracleParameter param1 = new OracleParameter();
                param1.ParameterName = ":pMobileNo";
                param1.Value = mobileNo;
                cmd.Parameters.Add(param1);
                OracleParameter param2 = new OracleParameter();
                param2.ParameterName = ":pCAN";
                param2.Value = can;
                cmd.Parameters.Add(param2);
                OracleDataReader reader = cmd.ExecuteReader();
                dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                con.Close();
            }
        }

        internal object GetCanDetailsByStatus(string inspectionStatus, string mobileNo, int statusFlag)
        {
            try
            {
                con.Open();
                if (inspectionStatus.Equals("pending"))
                    whereCondition = " AND AMR.CAN NOT IN (SELECT CAN FROM TBL_AMR_PREVENTIVE_MAINTANANCE WHERE QUARTERTYPE='Quarter1')";
                //else if (inspectionStatus.Equals("vendorissue") && statusFlag == 1)
                //    whereCondition = " AND APM.INSPECTIONSTATUS =:pSTATUS AND TRUNC(APM.INSPECTIONDATE)-TRUNC(APM.CAPTUREDDATE)<=2";
                //else if (inspectionStatus.Equals("vendorissue") && statusFlag == 2)
                else if (inspectionStatus.Equals("inspectiondone") && statusFlag == 1)
                    whereCondition = " AND APM.INSPECTIONSTATUS =:pSTATUS AND APM.CAN IN (SELECT CAN FROM TBL_AMR_VENDOR_ISSUES WHERE QUARTERTYPE='Quarter1' AND CAPTUREDBY=:pMobileNo UNION SELECT CAN FROM TBL_AMR_CUSTOMER_ISSUES WHERE QUARTERTYPE = 'Quarter1' AND CAPTUREDBY =:pMobileNo UNION SELECT CAN FROM TBL_AMR_NOMETER WHERE QUARTERTYPE = 'Quarter1' AND CAPTUREDBY =:pMobileNo) AND TRUNC(APM.INSPECTIONDATE)-TRUNC(APM.CAPTUREDDATE)<=2";
                else if (inspectionStatus.Equals("inspectiondone") && statusFlag == 2)
                    whereCondition = " AND APM.INSPECTIONSTATUS =:pSTATUS AND APM.CAN IN (SELECT CAN FROM TBL_AMR_VENDOR_ISSUES WHERE QUARTERTYPE='Quarter1' AND CAPTUREDBY=:pMobileNo UNION SELECT CAN FROM TBL_AMR_CUSTOMER_ISSUES WHERE QUARTERTYPE = 'Quarter1' AND CAPTUREDBY =:pMobileNo UNION SELECT CAN FROM TBL_AMR_NOMETER WHERE QUARTERTYPE = 'Quarter1' AND CAPTUREDBY =:pMobileNo) AND TRUNC(APM.INSPECTIONDATE)-TRUNC(APM.CAPTUREDDATE)>2";
                else
                    whereCondition = " AND APM.INSPECTIONSTATUS=:pSTATUS";
                string qry = @"SELECT AMR.CAN,MSC.FIRSTNAME||MSC.MIDDLENAME||MSC.LASTNAME CONSUMER_NAME,SC.SPGHMCHNO||','||SC.SPSTREET||','||SC.SPAREA||','||SC.SPPINCODE 
                                          CONSUMER_ADDRESS,SC.SPMOBILENO MOBILE_NO,AMR.DIVISIONCODE,AMR.SECTIONNAME,SC.RBSTCATEGORYID CATEGORY,WS.DESCRIPTION PIPE_SIZE,AMR.QUARTERTYPE,
                                          CASE WHEN APM.INSPECTIONSTATUS IS NOT NULL THEN INSPECTIONSTATUS ELSE 'pending' end INSPECTIONSTATUS,MCC.LATITUDE,MCC.LONGITUDE
                                          FROM TBL_AMR_AGENCY_USERS USR JOIN TBL_AMR_AGENCYFLAGGEDCANS AMR ON AMR.AGENCYTYPE=USR.AGENCY_NAME INNER JOIN CTLHMWSSBTMP.MDM_SHR_SERVICECONN SC ON SC.CAN=AMR.CAN
                                          INNER JOIN CTLHMWSSBTMP.MDM_SHR_CONSUMER MSC ON MSC.PKEY=SC.CONSUMERPKEY INNER JOIN CTLHMWSSBTMP.MDM_SHR_WATERSIZE WS ON WS.CODE=SC.WATERSIZEPKEY
                                          LEFT JOIN TBL_AMR_PREVENTIVE_MAINTANANCE APM ON APM.CAN=AMR.CAN
                                          LEFT JOIN CTLHMWSSBTMP.EIF_MCC_CANS_LAT_LONG MCC ON MCC.CAN=AMR.CAN
                                          WHERE AGENCY_MOBILENO=:pMobileNo " + whereCondition + " " +
                                          "AND USR.ISACTIVE='Y' AND AMR.QUARTERTYPE='Quarter1'";
                cmd = new OracleCommand(qry, con);
                if (inspectionStatus.Equals("pending"))
                {
                    OracleParameter param1 = new OracleParameter();
                    param1.ParameterName = ":pMobileNo";
                    param1.Value = mobileNo;
                    cmd.Parameters.Add(param1);
                }
                else
                {
                    OracleParameter param1 = new OracleParameter();
                    param1.ParameterName = ":pMobileNo";
                    param1.Value = mobileNo;
                    cmd.Parameters.Add(param1);
                    OracleParameter param2 = new OracleParameter();
                    param2.ParameterName = ":pSTATUS";
                    param2.Value = inspectionStatus;
                    cmd.Parameters.Add(param2);
                }
                OracleDataReader reader = cmd.ExecuteReader();
                dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                con.Close();
            }
        }

        public bool CapturePreventiveMaintananceDetails(PreventiveMaintanance values)
        {
            try
            {
                ReferenceNo = GenerateNewReferenceNo();
                meterImgPath = SaveMeterImagePath(values.METERIMGPATH, values.CAN, values.INSPECTIONSTATUS, values.QUARTERTYPE);
                cmd = new OracleCommand();
                cmd.Connection = con;
                cmd.CommandText = "SP_INSERT_AMR_PREVENTIVEMAINTANANCE";
                //cmd.CommandText = "SP_INSERT_AMR_PREVMNTNCE";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("pCAN", OracleType.VarChar).Value = values.CAN.Trim();
                if (values.ISMETERPRESENT.Trim().Equals("true"))
                    cmd.Parameters.Add("pISMETERPRESENT", OracleType.VarChar).Value = "Yes";
                else if (values.ISMETERPRESENT.Trim().Equals("false"))
                    cmd.Parameters.Add("pISMETERPRESENT", OracleType.VarChar).Value = "No";
                else
                    cmd.Parameters.Add("pISMETERPRESENT", OracleType.VarChar).Value = values.ISMETERPRESENT.Trim();
                if (values.ISMETERPRESENT.Equals("false"))
                {
                    cmd.Parameters.Add("pISANYMETERREPAIRWORKS", OracleType.VarChar).Value =
                    cmd.Parameters.Add("pISDISPLAYWORKING", OracleType.VarChar).Value =
                    cmd.Parameters.Add("pISFOGPRESENTINSIDEMETERE", OracleType.VarChar).Value =
                    cmd.Parameters.Add("pISMETERSUBMERGED", OracleType.VarChar).Value =
                    cmd.Parameters.Add("pISMETERCHAMBERPRESENT", OracleType.VarChar).Value =
                    cmd.Parameters.Add("pISANYLEAKAGESPRESENT", OracleType.VarChar).Value =
                    cmd.Parameters.Add("pISPIPESIZESAME", OracleType.VarChar).Value =
                    cmd.Parameters.Add("pCANLIVESTATUS", OracleType.VarChar).Value = string.Empty;
                }
                else
                {
                    if (values.ISANYMETERREPAIRWORKS.Trim().Equals("true"))
                        cmd.Parameters.Add("pISANYMETERREPAIRWORKS", OracleType.VarChar).Value = "Yes";
                    else if (values.ISANYMETERREPAIRWORKS.Trim().Equals("false"))
                        cmd.Parameters.Add("pISANYMETERREPAIRWORKS", OracleType.VarChar).Value = "No";
                    else
                        cmd.Parameters.Add("pISANYMETERREPAIRWORKS", OracleType.VarChar).Value = values.ISANYMETERREPAIRWORKS.Trim();
                    if (values.ISDISPLAYWORKING.Trim().Equals("true"))
                        cmd.Parameters.Add("pISDISPLAYWORKING", OracleType.VarChar).Value = "Yes";
                    else if (values.ISDISPLAYWORKING.Trim().Equals("false"))
                        cmd.Parameters.Add("pISDISPLAYWORKING", OracleType.VarChar).Value = "No";
                    else
                        cmd.Parameters.Add("pISDISPLAYWORKING", OracleType.VarChar).Value = values.ISDISPLAYWORKING.Trim();
                    if (values.ISFOGPRESENTINSIDEMETERE.Trim().Equals("true"))
                        cmd.Parameters.Add("pISFOGPRESENTINSIDEMETERE", OracleType.VarChar).Value = "Yes";
                    else if (values.ISFOGPRESENTINSIDEMETERE.Trim().Equals("false"))
                        cmd.Parameters.Add("pISFOGPRESENTINSIDEMETERE", OracleType.VarChar).Value = "No";
                    else
                        cmd.Parameters.Add("pISFOGPRESENTINSIDEMETERE", OracleType.VarChar).Value = values.ISFOGPRESENTINSIDEMETERE.Trim();
                    if (values.ISMETERSUBMERGED.Trim().Equals("true"))
                        cmd.Parameters.Add("pISMETERSUBMERGED", OracleType.VarChar).Value = "Yes";
                    else if (values.ISMETERSUBMERGED.Trim().Equals("false"))
                        cmd.Parameters.Add("pISMETERSUBMERGED", OracleType.VarChar).Value = "No";
                    else
                        cmd.Parameters.Add("pISMETERSUBMERGED", OracleType.VarChar).Value = values.ISMETERSUBMERGED.Trim();
                    if (values.ISMETERCHAMBERPRESENT.Trim().Equals("true"))
                        cmd.Parameters.Add("pISMETERCHAMBERPRESENT", OracleType.VarChar).Value = "Yes";
                    else if (values.ISMETERCHAMBERPRESENT.Trim().Equals("false"))
                        cmd.Parameters.Add("pISMETERCHAMBERPRESENT", OracleType.VarChar).Value = "No";
                    else
                        cmd.Parameters.Add("pISMETERCHAMBERPRESENT", OracleType.VarChar).Value = values.ISMETERCHAMBERPRESENT.Trim();
                    if (values.ISANYLEAKAGESPRESENT.Trim().Equals("true"))
                        cmd.Parameters.Add("pISANYLEAKAGESPRESENT", OracleType.VarChar).Value = "Yes";
                    else if (values.ISANYLEAKAGESPRESENT.Trim().Equals("false"))
                        cmd.Parameters.Add("pISANYLEAKAGESPRESENT", OracleType.VarChar).Value = "No";
                    else
                        cmd.Parameters.Add("pISANYLEAKAGESPRESENT", OracleType.VarChar).Value = values.ISANYLEAKAGESPRESENT.Trim();
                    if (values.ISPIPESIZESAME.Trim().Equals("true"))
                        cmd.Parameters.Add("pISPIPESIZESAME", OracleType.VarChar).Value = "Yes";
                    else if (values.ISPIPESIZESAME.Trim().Equals("false"))
                        cmd.Parameters.Add("pISPIPESIZESAME", OracleType.VarChar).Value = "No";
                    else
                        cmd.Parameters.Add("pISPIPESIZESAME", OracleType.VarChar).Value = values.ISPIPESIZESAME.Trim();
                    if (values.CANLIVESTATUS.Trim().Equals("true"))
                        cmd.Parameters.Add("pCANLIVESTATUS", OracleType.VarChar).Value = "Live";
                    else if (values.CANLIVESTATUS.Trim().Equals("false"))
                        cmd.Parameters.Add("pCANLIVESTATUS", OracleType.VarChar).Value = "Disconnected";
                    else
                        cmd.Parameters.Add("pCANLIVESTATUS", OracleType.VarChar).Value = values.CANLIVESTATUS.Trim();
                }
                cmd.Parameters.Add("pREMARKS", OracleType.VarChar).Value = values.REMARKS.Trim();
                cmd.Parameters.Add("pMETERIMGPATH", OracleType.VarChar).Value = meterImgPath;
                cmd.Parameters.Add("pMETERIMGLATITUDE", OracleType.VarChar).Value = values.METERIMGLATITUDE.Trim();
                cmd.Parameters.Add("pMETERIMGLONGITUDE", OracleType.VarChar).Value = values.METERIMGLONGITUDE.Trim();
                cmd.Parameters.Add("pINSPECTIONBY", OracleType.VarChar).Value = values.INSPECTIONBY.Trim();
                cmd.Parameters.Add("pINSPECTIONDATE", OracleType.Timestamp).Value = DateTime.Now;
                cmd.Parameters.Add("pAPPVERSION", OracleType.VarChar).Value = values.APPVERSION.Trim();
                cmd.Parameters.Add("pINSPECTIONSTATUS", OracleType.VarChar).Value = values.INSPECTIONSTATUS.Trim();
                cmd.Parameters.Add("pREFERENCENO", OracleType.VarChar).Value = ReferenceNo;
                cmd.Parameters.Add("pREFSLNO", OracleType.Number).Value = refSlno;
                cmd.Parameters.Add("pREFMONTH", OracleType.VarChar).Value = refMonthid;
                cmd.Parameters.Add("pLASTBREAKPOINT", OracleType.VarChar).Value = values.LASTBREAKPOINT;
                if (values.INSPECTIONSTATUS.Equals("customerissue"))
                    cmd.Parameters.Add("pCUSTOMERISSUEDATE", OracleType.Timestamp).Value = DateTime.Now;
                else
                    cmd.Parameters.Add("pCUSTOMERISSUEDATE", OracleType.Timestamp).Value = DBNull.Value;
                if (values.INSPECTIONSTATUS.Equals("vendorissue"))
                    cmd.Parameters.Add("pVENDORISSUEDATE", OracleType.Timestamp).Value = DateTime.Now;
                else
                    cmd.Parameters.Add("pVENDORISSUEDATE", OracleType.Timestamp).Value = DBNull.Value;
                if (values.INSPECTIONSTATUS.Equals("nometer"))
                    cmd.Parameters.Add("pNEWMETERDATE", OracleType.Timestamp).Value = DateTime.Now;
                else
                    cmd.Parameters.Add("pNEWMETERDATE", OracleType.Timestamp).Value = DBNull.Value;
                cmd.Parameters.Add("pQUARTERTYPE", OracleType.VarChar).Value = values.QUARTERTYPE;
                cmd.Parameters.Add("pCAPTUREDDATE", OracleType.Timestamp).Value = DateTime.Now;
                cmd.Parameters.Add("pISMETERREPAIREDORREPLACED", OracleType.VarChar).Value = values.ISMETERREPAIREDORREPLACED;

                con.Open();
                saveResult = cmd.ExecuteNonQuery();
                if (saveResult > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                log.InfoFormat("Exception {0}", ex.ToString());
                log.Error(ex.ToString());
                throw ex;
            }
            finally { con.Close(); }

        }

        private string GenerateNewReferenceNo()
        {
            try
            {
                string month = DateTime.Now.ToString("yyyy-MM");
                if (con.State == ConnectionState.Closed)
                {
                    con.Open();
                }
                OracleCommand illcmd = new OracleCommand("SELECT MAX(REFSLNO) AS FORMID FROM TBL_AMR_PREVENTIVE_MAINTANANCE WHERE REFMONTH='" + refMonthid + "'", con);
                OracleDataReader illdr;
                illdr = illcmd.ExecuteReader();
                int num;
                int num1;
                if (illdr.Read() == true)
                {
                    if (System.DBNull.Value == illdr["FormID"])
                    {

                        num = 1;
                        refSlno = num;
                        //referenceNo = month + num;
                        referenceNo = month + "-" + num;
                        // FormID = num.ToString();
                    }
                    else
                    {
                        object splobject = illcmd.ExecuteOracleScalar();
                        string insreslut = splobject.ToString();
                        num1 = Convert.ToInt32(insreslut);
                        num1 = num1 + 1;
                        refSlno = num1; ;
                        //referenceNo = month + num1;
                        referenceNo = month + "-" + num1;
                    }
                }

                return referenceNo;
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }

        }

        private string SaveMeterImagePath(string base64, string can, string inspectionStatus, string quarterType)
        {
            if (!string.IsNullOrEmpty(base64))
            {
                string mainPath = "~/" + quarterType + "_Images";
                if (inspectionStatus.Equals("customerissue"))
                {
                    path = HttpContext.Current.Server.MapPath(mainPath + "/CustomerIssueImages");
                    imageName = Images(base64, path, can, inspectionStatus);
                }
                else if (inspectionStatus.Equals("vendorissue"))
                {
                    path = HttpContext.Current.Server.MapPath(mainPath + "/VendorIssueImages");
                    imageName = Images(base64, path, can, inspectionStatus);
                }
                else if (inspectionStatus.Equals("nometer"))
                {
                    path = HttpContext.Current.Server.MapPath(mainPath + "/NoMeterImages");
                    imageName = Images(base64, path, can, inspectionStatus);
                }
                else if (inspectionStatus.Equals("complaint"))
                {
                    path = HttpContext.Current.Server.MapPath(mainPath + "/ComplaintImages");
                    imageName = Images(base64, path, can, inspectionStatus);
                }
                else if (inspectionStatus.Equals("inspectiondone"))
                {
                    path = HttpContext.Current.Server.MapPath(mainPath + "/InspectionDoneImages");
                    imageName = Images(base64, path, can, inspectionStatus);
                }
                path = HttpContext.Current.Server.MapPath(mainPath + "/PreventiveMaintananceImages");
                imageName = Images(base64, path, can, inspectionStatus);
                return imageName;
            }
            else
            {
                return string.Empty;
            }
        }

        private string Images(string base64, string savePath, string can, string picName)
        {
            //Path
            //Check if directory exist
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path); //Create directory if it doesn't exist
            }
            imageName = can + "_" + picName + "_" + System.DateTime.Now.ToString("yyyymmdd") + ".jpg";
            //set the image path
            imgPath = Path.Combine(path, imageName);
            byte[] imageBytes = Convert.FromBase64String(base64);
            File.WriteAllBytes(imgPath, imageBytes);
            return imageName;
        }

        internal object GetReplacementMeterCansListfromPreventiveMaintenance(string mobileNo)
        {
            try
            {
                con.Open();
                cmd = new OracleCommand(@"SELECT AMR.CAN,MSC.FIRSTNAME||MSC.MIDDLENAME||MSC.LASTNAME CONSUMER_NAME,SC.SPGHMCHNO||','||SC.SPSTREET||','||SC.SPAREA||','||SC.SPPINCODE 
                           CONSUMER_ADDRESS,SC.SPMOBILENO MOBILE_NO,AMR.DIVISIONCODE,AMR.SECTIONNAME,SC.RBSTCATEGORYID CATEGORY,WS.DESCRIPTION PIPE_SIZE,AMR.QUARTERTYPE
                           FROM TBL_AMR_AGENCY_USERS USR JOIN TBL_AMR_AGENCYFLAGGEDCANS AMR ON AMR.AGENCYTYPE=USR.AGENCY_NAME INNER JOIN CTLHMWSSBTMP.MDM_SHR_SERVICECONN SC ON SC.CAN=AMR.CAN
                           INNER JOIN CTLHMWSSBTMP.MDM_SHR_CONSUMER MSC ON MSC.PKEY=SC.CONSUMERPKEY INNER JOIN CTLHMWSSBTMP.MDM_SHR_WATERSIZE WS ON WS.CODE=SC.WATERSIZEPKEY
                           INNER JOIN TBL_AMR_PREVENTIVE_MAINTANANCE AMP ON AMP.CAN=AMR.CAN WHERE AGENCY_MOBILENO=:pMobileNo AND USR.ISACTIVE='Y' 
                           AND AMR.QUARTERTYPE='Quarter1' AND AMP.INSPECTIONSTATUS='nometer' AND AMP.CAN NOT IN (SELECT CAN FROM TBL_AMR_NEWMETER_INSTALLATION WHERE QUARTERTYPE='Quarter1')", con);
                OracleParameter param1 = new OracleParameter();
                param1.ParameterName = ":pMobileNo";
                param1.Value = mobileNo;
                cmd.Parameters.Add(param1);
                OracleDataReader reader = cmd.ExecuteReader();
                dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                con.Close();
            }
        }

        public bool CaptureNewMeterInstallation(NewMeterInstallation values)
        {
            try
            {
                NewMeterReferenceNo = GenerateNewMeterReferenceNo();
                //newMeterImgPath = SaveNewMeterImagePath(values.METERIMAGPATH, values.CAN, NewMeterReferenceNo);
                newMeterImgPath = SaveNewMeterImagePath(values.METERIMAGPATH, values.CAN, "NewMeter", values.QUARTERTYPE);
                oldMeterImgPath = SaveNewMeterImagePath(values.METERIMAGPATH, values.CAN, "OldMeter", values.QUARTERTYPE);
                cmd = new OracleCommand();
                cmd.Connection = con;
                cmd.CommandText = "SP_INSERT_AMR_NEWMETERINSTALLATION";
                //cmd.CommandText = "SP_INSERT_AMR_NEWMTRINSTLN";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("pCAN", OracleType.VarChar).Value = values.CAN.Trim();
                cmd.Parameters.Add("pNEWMETERNUMBER", OracleType.VarChar).Value = values.NEWMETERNUMBER.Trim();
                cmd.Parameters.Add("pINITIALMETERREADING", OracleType.Int32).Value = values.INITIALMETERREADING;
                cmd.Parameters.Add("pOLDMETERREADING", OracleType.Int32).Value = values.OLDMETERREADING;
                cmd.Parameters.Add("pPIPESIZE", OracleType.Number).Value = values.PIPESIZE;
                cmd.Parameters.Add("pMETERFIXATIONDATE", OracleType.DateTime).Value = values.METERFIXATIONDATE;
                cmd.Parameters.Add("pMETERREMOVALDATE", OracleType.DateTime).Value = values.METERREMOVALDATE;
                cmd.Parameters.Add("pMETERTYPE", OracleType.VarChar).Value = values.METERTYPE.Trim();
                cmd.Parameters.Add("pMETERMAKE", OracleType.VarChar).Value = values.METERMAKE.Trim();
                cmd.Parameters.Add("pEXISTINGMETERCONDITION", OracleType.VarChar).Value = values.EXISTINGMETERCONDITION.Trim();
                cmd.Parameters.Add("pISCHAMBEREXISTS", OracleType.VarChar).Value = values.ISCHAMBEREXISTS.Trim();
                cmd.Parameters.Add("pMETERIMAGPATH", OracleType.VarChar).Value = newMeterImgPath;
                cmd.Parameters.Add("pLATITUDE", OracleType.VarChar).Value = values.LATITUDE.Trim();
                cmd.Parameters.Add("pLONGITUDE", OracleType.VarChar).Value = values.LONGITUDE.Trim();
                cmd.Parameters.Add("pCAPTUREDBY", OracleType.VarChar).Value = values.CAPTUREDBY.Trim();
                cmd.Parameters.Add("pCAPTUREDDATE", OracleType.Timestamp).Value = DateTime.Now;
                cmd.Parameters.Add("pQUARTERTYPE", OracleType.VarChar).Value = values.QUARTERTYPE;
                cmd.Parameters.Add("pREFERENCENO", OracleType.VarChar).Value = NewMeterReferenceNo;
                cmd.Parameters.Add("pREFSLNO", OracleType.Number).Value = refSlno;
                cmd.Parameters.Add("pREFMONTH", OracleType.VarChar).Value = refMonthid;
                cmd.Parameters.Add("pREMARKS", OracleType.VarChar).Value = values.REMARKS;
                cmd.Parameters.Add("pAPPVERSION", OracleType.VarChar).Value = values.APPVERSION.Trim();
                if (values.ISOLDMETEREXISTS.Trim().Equals("true"))
                    cmd.Parameters.Add("pISOLDMETEREXISTS", OracleType.VarChar).Value = "Yes";
                else if (values.ISOLDMETEREXISTS.Trim().Equals("false"))
                    cmd.Parameters.Add("pISOLDMETEREXISTS", OracleType.VarChar).Value = "No";
                else
                    cmd.Parameters.Add("pISOLDMETEREXISTS", OracleType.VarChar).Value = values.ISOLDMETEREXISTS.Trim();
                cmd.Parameters.Add("pMETERSTATUSFLAG", OracleType.VarChar).Value = values.METERSTATUSFLAG.Trim();
                cmd.Parameters.Add("pOLDMETERIMAGPATH", OracleType.VarChar).Value = oldMeterImgPath;

                cmd.Parameters.Add("pManagerMobileNo", OracleType.VarChar, 200).Direction = ParameterDirection.Output;

                con.Open();
                saveResult = cmd.ExecuteNonQuery();
                ManagerMobileNo = Convert.ToString(cmd.Parameters["pManagerMobileNo"].Value).Trim();
                if (saveResult > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                log.InfoFormat("Exception {0}", ex.ToString());
                log.Error(ex.ToString());
                throw ex;
            }
            finally { con.Close(); }

        }

        private string GenerateNewMeterReferenceNo()
        {
            try
            {
                string month = DateTime.Now.ToString("yyyy-MM");
                if (con.State == ConnectionState.Closed)
                {
                    con.Open();
                }
                OracleCommand illcmd = new OracleCommand("SELECT MAX(REFSLNO) AS FORMID FROM TBL_AMR_NEWMETER_INSTALLATION WHERE REFMONTH='" + refMonthid + "'", con);
                OracleDataReader illdr;
                illdr = illcmd.ExecuteReader();
                int num;
                int num1;
                if (illdr.Read() == true)
                {
                    if (System.DBNull.Value == illdr["FormID"])
                    {

                        num = 1;
                        refSlno = num;
                        //referenceNo = month + num;
                        referenceNo = month + "-" + num;
                        // FormID = num.ToString();
                    }
                    else
                    {
                        object splobject = illcmd.ExecuteOracleScalar();
                        string insreslut = splobject.ToString();
                        num1 = Convert.ToInt32(insreslut);
                        num1 = num1 + 1;
                        refSlno = num1; ;
                        //referenceNo = month + num1;
                        referenceNo = month + "-" + num1;
                    }
                }

                return referenceNo;
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }

        }

        private string SaveNewMeterImagePath(string base64, string can, string meterType, string quarterType)
        {
            if (!string.IsNullOrEmpty(base64))
            {
                string mainPath = "~/" + quarterType + "_Images";
                path = HttpContext.Current.Server.MapPath(mainPath + "/NewMeterInstallationImages");
                imageName = Images(base64, path, can, meterType);
                return imageName;
            }
            else
            {
                return string.Empty;
            }
        }

        internal object GetNewMeterInstallationDashboardSummary(string mobileNo)
        {
            try
            {
                con.Open();
                cmd = new OracleCommand(@"SELECT COUNT(NMC.CAN)TARGET,COUNT(NMI.CAN)COMPLETED,(COUNT(NMC.CAN)-COUNT(NMI.CAN))PENDING  FROM TBL_AMR_NEWMTRINSTALATION_CANS NMC
                                          INNER JOIN TBL_AMR_AGENCY_USERS AAU ON AAU.AGENCY_NAME=NMC.AGENCYTYPE
                                          LEFT JOIN TBL_AMR_NEWMETER_INSTALLATION NMI ON NMC.CAN=NMI.CAN
                                          WHERE AAU.AGENCY_MOBILENO=:pMobileNo AND NMC.QUARTERTYPE='Quarter1'", con);
                OracleParameter param1 = new OracleParameter();
                param1.ParameterName = ":pMobileNo";
                param1.Value = mobileNo;
                cmd.Parameters.Add(param1);
                OracleDataReader reader = cmd.ExecuteReader();
                dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                con.Close();
            }
        }

        internal object GetNewMeterInstallationCans(string mobileNo, int listType)
        {
            try
            {
                con.Open();
                if (listType == 1)
                    whereCondition = " AND AMR.CAN NOT IN(SELECT CAN FROM TBL_AMR_NEWMETER_INSTALLATION WHERE QUARTERTYPE = 'Quarter1')";
                else
                    if (listType == 2)
                    whereCondition = " AND AMR.CAN IN(SELECT CAN FROM TBL_AMR_NEWMETER_INSTALLATION WHERE QUARTERTYPE = 'Quarter1')";
                else
                    whereCondition = string.Empty;
                string query = @"SELECT AMR.CAN,MSC.FIRSTNAME || MSC.MIDDLENAME || MSC.LASTNAME CONSUMER_NAME,SC.SPGHMCHNO || ',' || SC.SPSTREET || ',' || SC.SPAREA || ',' || SC.SPPINCODE CONSUMER_ADDRESS,
                                 SC.SPMOBILENO MOBILE_NO, SDM.DIVISIONCODE,SDM.SECTIONNAME,SC.RBSTCATEGORYID CATEGORY, WS.DESCRIPTION PIPE_SIZE, AMR.QUARTERTYPE
                                 FROM TBL_AMR_AGENCY_USERS USR JOIN TBL_AMR_NEWMTRINSTALATION_CANS AMR ON AMR.AGENCYTYPE = USR.AGENCY_NAME
                                 INNER JOIN CTLHMWSSBTMP.MDM_SHR_SERVICECONN SC ON SC.CAN = AMR.CAN INNER JOIN CTLHMWSSBTMP.MDM_SHR_CONSUMER MSC
                                 ON MSC.PKEY = SC.CONSUMERPKEY INNER JOIN CTLHMWSSBTMP.MDM_SHR_WATERSIZE WS ON WS.CODE = SC.WATERSIZEPKEY
                                 INNER JOIN CTLHMWSSBTMP.MDM_SHR_SECTODIVMAPPING SDM ON SDM.SECTIONCODE = AMR.SECT WHERE AGENCY_MOBILENO =:pMobileNo
                                 AND USR.ISACTIVE = 'Y'AND AMR.QUARTERTYPE = 'Quarter1' " + whereCondition + "";
                cmd = new OracleCommand(query, con);
                OracleParameter param1 = new OracleParameter();
                param1.ParameterName = ":pMobileNo";
                param1.Value = mobileNo;
                cmd.Parameters.Add(param1);
                OracleDataReader reader = cmd.ExecuteReader();
                dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                con.Close();
            }
        }

        internal object GetCansListforCaptureReadingOld(string mobileNo)
        {
            try
            {
                con.Open();
                cmd = new OracleCommand(@"SELECT AMR.CAN,MSC.FIRSTNAME||MSC.MIDDLENAME||MSC.LASTNAME CONSUMER_NAME,SC.SPHOUSENUMBER||','||SC.SPSTREET||','||SC.SPAREA||','||SC.SPPINCODE CONSUMER_ADDRESS,
                                          SC.SPMOBILENO MOBILE_NO,AMR.DIVISIONCODE,AMR.SECTIONNAME,SC.RBSTCATEGORYID CATEGORY,WS.DESCRIPTION PIPE_SIZE,AMR.QUARTERTYPE,
                                          LWB.PRESENTMETERREADING PREVIOUSMETERREADING,TO_CHAR(LWB.PRESENTREADINGDATE,'DD-Mon-YYYY')PREVIOUSREADINGDATE,
                                          (TRUNC(SYSDATE)-(TRUNC(LWB.PRESENTREADINGDATE)))DAYS,LWB.CONSUNITS CONSUMPTIONUNITS,LWB.NOOFFLATSFORMSB,MCC.LATITUDE,MCC.LONGITUDE
                                          FROM TBL_AMR_AGENCY_USERS USR JOIN TBL_AMR_AGENCYFLAGGEDCANS AMR ON AMR.AGENCYTYPE=USR.AGENCY_NAME
                                          INNER JOIN CTLHMWSSBTMP.MDM_SHR_SERVICECONN SC ON SC.CAN=AMR.CAN INNER JOIN CTLHMWSSBTMP.MDM_SHR_CONSUMER MSC
                                          ON MSC.PKEY=SC.CONSUMERPKEY INNER JOIN CTLHMWSSBTMP.MDM_SHR_WATERSIZE WS ON WS.CODE=SC.WATERSIZEPKEY
                                          LEFT JOIN CTLHMWSSBTMP.RBS_LATESTWATERBILL LWB ON LWB.CAN=AMR.CAN
                                          LEFT JOIN CTLHMWSSBTMP.EIF_MCC_CANS_LAT_LONG MCC ON MCC.CAN=AMR.CAN
                                          WHERE AGENCY_MOBILENO=:pMobileNo AND USR.ISACTIVE='Y'AND AMR.QUARTERTYPE='Quarter1'  
                                          AND (TRUNC(SYSDATE)-TRUNC(LWB.PRESENTREADINGDATE))>25 AND LWB.BILLTYPE='M'
                                          AND AMR.CAN NOT IN(SELECT CAN FROM TBL_AMR_CAPTUREMETER_READINGS 
                                          WHERE(TRUNC(SYSDATE) - TRUNC(CAPTUREDDATE)) < EXTRACT(DAY FROM LAST_DAY(SYSDATE)))", con);
                //TRUNC(LWB.PRESENTREADINGDATE)PREVIOUSMETERREADINGDATE,,LWB.TARIFFCATEGORY
                //AND AMR.CAN NOT IN (SELECT CAN FROM TBL_AMR_CAPTUREMETER_READINGS WHERE (TRUNC(SYSDATE)-TRUNC(CAPTUREDDATE))>EXTRACT(DAY FROM LAST_DAY(SYSDATE)))", con);
                //AND AMR.CAN NOT IN(SELECT CAN FROM TBL_AMR_CAPTUREMETER_READINGS WHERE(TRUNC(SYSDATE) - TRUNC(CAPTUREDDATE)) > EXTRACT(DAY FROM LAST_DAY(SYSDATE)))", con);
                //AND AMR.CAN NOT IN(SELECT CAN FROM TBL_AMR_CAPTUREMETER_READINGS)", con);

                //cmd = new OracleCommand(@"SELECT HVC.CAN,MSC.FIRSTNAME||MSC.MIDDLENAME||MSC.LASTNAME CONSUMER_NAME,
                //                          SC.SPHOUSENUMBER||','||SC.SPSTREET||','||SC.SPAREA||','||SC.SPPINCODE CONSUMER_ADDRESS,
                //                          SC.SPMOBILENO MOBILE_NO,SDM.DIVNCODE DIVISIONCODE,SDM.SECTNAME SECTIONNAME,SC.RBSTCATEGORYID CATEGORY,
                //                          WS.DESCRIPTION PIPE_SIZE,'Quarter1' QUARTERTYPE,LWB.PRESENTMETERREADING PREVIOUSMETERREADING,
                //                          TO_CHAR(LWB.PRESENTREADINGDATE,'DD-Mon-YYYY')PREVIOUSREADINGDATE,
                //                          (TRUNC(SYSDATE)-(TRUNC(LWB.PRESENTREADINGDATE)))DAYS,LWB.CONSUNITS CONSUMPTIONUNITS,LWB.NOOFFLATSFORMSB,
                //                          HV.METER_IMG_LATTITUDE LATITUDE,HV.METER_IMG_LONGITUDE LONGITUDE
                //                          FROM TBL_HIGHVALUE_CANS HVC
                //                          INNER JOIN CTLHMWSSBTMP.MDM_SHR_SERVICECONN SC ON SC.CAN=HVC.CAN
                //                          INNER JOIN CTLHMWSSBTMP.MDM_SHR_WATERSIZE WS ON WS.CODE=SC.WATERSIZEPKEY
                //                          INNER JOIN CTLHMWSSBTMP.MDM_SHR_CONSUMER MSC  ON MSC.PKEY=SC.CONSUMERPKEY
                //                          INNER JOIN CTLHMWSSBTMP.MCC_SECT_SUBDIVN_DIVN_CIRCLE SDM ON SDM.SECTOUID=SC.ORGUNITPKEY
                //                          LEFT JOIN CTLHMWSSBTMP.RBS_LATESTWATERBILL LWB ON LWB.CAN=HVC.CAN
                //                          LEFT JOIN TBL_SURVEY_HMWSSB_HIGHVALUE_CANS HV ON HV.CAN=HVC.CAN
                //                          WHERE WS.DESCRIPTION>=40", con);

                OracleParameter param1 = new OracleParameter();
                param1.ParameterName = ":pMobileNo";
                param1.Value = mobileNo;
                cmd.Parameters.Add(param1);
                OracleDataReader reader = cmd.ExecuteReader();
                dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                con.Close();
            }
        }

        internal object GetCansListforCaptureReading(string mobileNo)
        {
            try
            {
                //dt = GetData(@"SELECT HVC.CAN,MSC.FIRSTNAME||MSC.MIDDLENAME||MSC.LASTNAME CONSUMER_NAME,
                //                          SC.SPHOUSENUMBER||','||SC.SPSTREET||','||SC.SPAREA||','||SC.SPPINCODE CONSUMER_ADDRESS,
                //                          SC.SPMOBILENO MOBILE_NO,SDM.DIVNCODE DIVISIONCODE,SDM.SECTNAME SECTIONNAME,CAT.CODE||'_'||CAT.DESCRIPTION CATEGORY,
                //                          WS.DESCRIPTION PIPE_SIZE,'Quarter1' QUARTERTYPE,LWB.PRESENTMETERREADING PREVIOUSMETERREADING,
                //                          TO_CHAR(LWB.PRESENTREADINGDATE,'DD-Mon-YYYY')PREVIOUSREADINGDATE,
                //                          (TRUNC(SYSDATE)-(TRUNC(LWB.PRESENTREADINGDATE)))DAYS,LWB.CONSUNITS CONSUMPTIONUNITS,LWB.NOOFFLATSFORMSB,
                //                          HV.METER_IMG_LATTITUDE LATITUDE,HV.METER_IMG_LONGITUDE LONGITUDE
                //                          FROM TBL_HIGHVALUE_CANS HVC
                //                          INNER JOIN CTLHMWSSBTMP.MDM_SHR_SERVICECONN SC ON SC.CAN=HVC.CAN
                //                          INNER JOIN CTLHMWSSBTMP.MDM_SHR_WATERSIZE WS ON WS.CODE=SC.WATERSIZEPKEY
                //                          INNER JOIN CTLHMWSSBTMP.MDM_SHR_CONSUMER MSC  ON MSC.PKEY=SC.CONSUMERPKEY
                //                          INNER JOIN CTLHMWSSBTMP.MCC_SECT_SUBDIVN_DIVN_CIRCLE SDM ON SDM.SECTOUID=SC.ORGUNITPKEY
                //                          INNER JOIN CTLHMWSSBTMP.MDM_SHR_RBSTARIFFCATEGORIES CAT ON CAT.CODE=SC.RBSTCATEGORYID
                //                          LEFT JOIN CTLHMWSSBTMP.RBS_LATESTWATERBILL LWB ON LWB.CAN=HVC.CAN
                //                          LEFT JOIN TBL_SURVEY_HMWSSB_HIGHVALUE_CANS HV ON HV.CAN=HVC.CAN
                //                          WHERE WS.DESCRIPTION>=40 AND HVC.CAN NOT IN(SELECT CAN FROM TBL_AMR_CAPTUREMETER_READINGS 
                //                          WHERE(TRUNC(SYSDATE) - TRUNC(CAPTUREDDATE)) < EXTRACT(DAY FROM LAST_DAY(SYSDATE)))");
                con.Open();
                cmd = new OracleCommand(@"SELECT DISTINCT SC.CAN,MSC.FIRSTNAME|| MSC.MIDDLENAME||MSC.LASTNAME CONSUMER_NAME,SC.SPHOUSENUMBER|| ','|| SC.SPSTREET|| ','|| SC.SPAREA|| ',' || SC.SPPINCODE CONSUMER_ADDRESS,
                               SC.SPMOBILENO MOBILE_NO,SDM.DIVNCODE DIVISIONCODE,SDM.SECTNAME SECTIONNAME,CAT.CODE|| '_'||CAT.DESCRIPTION CATEGORY,WS.DESCRIPTION PIPE_SIZE,'QUARTER1' QUARTERTYPE,
                               LWB.PRESENTMETERREADING PREVIOUSMETERREADING,TO_CHAR(LWB.PRESENTREADINGDATE, 'DD-MON-YYYY')PREVIOUSREADINGDATE,                                ( TRUNC(SYSDATE) - ( TRUNC(
                               LWB.PRESENTREADINGDATE) ) )DAYS,LWB.CONSUNITS CONSUMPTIONUNITS,LWB.NOOFFLATSFORMSB,MCC.LATITUDE,MCC.LONGITUDE
                               FROM CTLHMWSSBTMP.MDM_SHR_SERVICECONN SC 
                               INNER JOIN CTLHMWSSBTMP.MDM_SHR_WATERSIZE            WS ON WS.CODE = SC.WATERSIZEPKEY
                               INNER JOIN CTLHMWSSBTMP.MDM_SHR_CONSUMER             MSC ON MSC.PKEY = SC.CONSUMERPKEY
                               INNER JOIN CTLHMWSSBTMP.MCC_SECT_SUBDIVN_DIVN_CIRCLE SDM ON SDM.SECTOUID = SC.ORGUNITPKEY
                               INNER JOIN CTLHMWSSBTMP.MDM_SHR_RBSTARIFFCATEGORIES  CAT ON CAT.CODE = SC.RBSTCATEGORYID
                               LEFT JOIN CTLHMWSSBTMP.RBS_LATESTWATERBILL          LWB ON LWB.CAN = SC.CAN
                               LEFT JOIN CTLHMWSSBTMP.EIF_MCC_CANS_LAT_LONG        MCC ON MCC.CAN = SC.CAN
                               LEFT JOIN TBL_AMR_AGENCY_USERS USR ON USR.AGENCY_NAME=SC.AGENCYTYPE
                               WHERE
                               SC.RBSTCATEGORYID NOT IN ('D','DM','DP','DS','O','M1','M2','M4','U','MS','FS','RC','CH','PS','M0')
                               AND SC.CONNSTATUS<>3  AND SC.HASAMRMETER=1
                                AND SC.CAN NOT IN (
                                    SELECT
                                        CAN
                                    FROM
                                        TBL_AMR_CAPTUREMETER_READINGS
                                    WHERE
                                        (TRUNC(SYSDATE) - TRUNC(CAPTUREDDATE) ) < 25 GROUP BY CAN
                                ) AND USR.AGENCY_MOBILENO=:pMobileNo", con);
                //(TRUNC(SYSDATE) - TRUNC(CAPTUREDDATE) ) < EXTRACT(DAY FROM LAST_DAY(SYSDATE)) GROUP BY CAN
                OracleParameter param1 = new OracleParameter();
                param1.ParameterName = ":pMobileNo";
                param1.Value = mobileNo;
                cmd.Parameters.Add(param1);
                OracleDataReader reader = cmd.ExecuteReader();
                dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                con.Close();
            }
        }

        internal object GetReviewCansListforCaptureReading(string mobileNo)
        {
            try
            {
                con.Open();
                cmd = new OracleCommand(@"SELECT CMR.PKEY,SC.CAN,MSC.FIRSTNAME|| MSC.MIDDLENAME || MSC.LASTNAME CONSUMER_NAME,SC.SPHOUSENUMBER
                                        || ','|| SC.SPSTREET || ',' || SC.SPAREA || ',' || SC.SPPINCODE CONSUMER_ADDRESS,SC.SPMOBILENO MOBILE_NO,
                                        SDM.DIVNCODE DIVISIONCODE,SDM.SECTNAME SECTIONNAME,CAT.CODE|| '_'|| CAT.DESCRIPTION CATEGORY,WS.DESCRIPTION PIPE_SIZE,
                                        CMR.PRESENTMETERREADING   PREVIOUSMETERREADING,TRUNC(CMR.CAPTUREDDATE)PREVIOUSMETERREADINGDATE,CMR.PRESENTCONSUMPTIONUNITS
                                        CONSUMPTIONUNITS,LWB.NOOFFLATSFORMSB,CMR.MTRIMGLATITUDE LATITUDE,CMR.MTRIMGLONGITUDE LONGITUDE
                                    FROM
                                        TBL_AMR_CAPTUREMETER_READINGS        CMR
                                        INNER JOIN CTLHMWSSBTMP.MDM_SHR_SERVICECONN     SC ON SC.CAN = CMR.CAN
                                        LEFT JOIN CTLHMWSSBTMP.RBS_LATESTWATERBILL          LWB ON LWB.CAN = SC.CAN
                                        INNER JOIN CTLHMWSSBTMP.MDM_SHR_WATERSIZE            WS ON WS.CODE = SC.WATERSIZEPKEY
                                        INNER JOIN CTLHMWSSBTMP.MDM_SHR_RBSTARIFFCATEGORIES  CAT ON CAT.CODE = SC.RBSTCATEGORYID
                                        INNER JOIN CTLHMWSSBTMP.MCC_SECT_SUBDIVN_DIVN_CIRCLE SDM ON SDM.SECTOUID = SC.ORGUNITPKEY
                                        INNER JOIN CTLHMWSSBTMP.MDM_SHR_CONSUMER             MSC ON MSC.PKEY = SC.CONSUMERPKEY
                                        INNER JOIN TBL_AMR_AGENCY_USERS USR ON USR.AGENCY_MOBILENO=CMR.CAPTUREDBY
                                    WHERE
                                        REVIEWFLAG=1 AND USR.AGENCY_MOBILENO=:pMobileNo", con);
                //(TRUNC(SYSDATE) - TRUNC(CAPTUREDDATE) ) < EXTRACT(DAY FROM LAST_DAY(SYSDATE)) GROUP BY CAN
                OracleParameter param1 = new OracleParameter();
                param1.ParameterName = ":pMobileNo";
                param1.Value = mobileNo;
                cmd.Parameters.Add(param1);
                OracleDataReader reader = cmd.ExecuteReader();
                dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                con.Close();
            }
        }

        public bool CaptureMeterReadingDetails(CaptureMeterReading values)
        {
            try
            {
                if (CheckDuplicateCAN(values.CAN))
                {
                    captureMtrReadingReferenceNo = GenerateReferenceNoforCaptureMeterReading();
                    captureMtrReadingImgPath = SaveCapturMeterReadingImagePath(values.MTRIMGPATH, values.CAN, captureMtrReadingReferenceNo);
                    cmd = new OracleCommand();
                    cmd.Connection = con;
                    cmd.CommandText = "SP_INSERT_AMR_CAPTUREMETER_READINGS";
                    //cmd.CommandText = "SP_INSERT_AMR_NEWMTRINSTLN";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("pCAN", OracleType.VarChar).Value = values.CAN.Trim();
                    cmd.Parameters.Add("pCATEGORY", OracleType.VarChar).Value = values.CATEGORY.Trim();
                    cmd.Parameters.Add("pPREVIOUSMETERREADING", OracleType.Int32).Value = values.PREVIOUSMETERREADING;
                    cmd.Parameters.Add("pPREVIOUSMETERREADINGDATE", OracleType.DateTime).Value = values.PREVIOUSMETERREADINGDATE;
                    cmd.Parameters.Add("pPREVIOUSCONSUMPTIONUNITS", OracleType.Int32).Value = values.PREVIOUSCONSUMPTIONUNITS;
                    cmd.Parameters.Add("pPRESENTMETERREADING", OracleType.Int32).Value = values.PRESENTMETERREADING;
                    cmd.Parameters.Add("pPRESENTMETERREADINGDATE", OracleType.DateTime).Value = DateTime.Now;
                    cmd.Parameters.Add("pPRESENTCONSUMPTIONUNITS", OracleType.Int32).Value = values.PRESENTCONSUMPTIONUNITS;
                    cmd.Parameters.Add("pMTRIMGPATH", OracleType.VarChar).Value = captureMtrReadingImgPath;
                    cmd.Parameters.Add("pMTRIMGLATITUDE", OracleType.VarChar).Value = values.MTRIMGLATITUDE.Trim();
                    cmd.Parameters.Add("pMTRIMGLONGITUDE", OracleType.VarChar).Value = values.MTRIMGLONGITUDE.Trim();
                    cmd.Parameters.Add("pREFERENCENO", OracleType.VarChar).Value = captureMtrReadingReferenceNo;
                    cmd.Parameters.Add("pREFSLNO", OracleType.Number).Value = refSlno;
                    cmd.Parameters.Add("pREFMONTH", OracleType.VarChar).Value = refMonthid;
                    cmd.Parameters.Add("pQUARTERTYPE", OracleType.VarChar).Value = values.QUARTERTYPE.Trim();
                    cmd.Parameters.Add("pCAPTUREDBY", OracleType.VarChar).Value = values.CAPTUREDBY;
                    cmd.Parameters.Add("pCAPTUREDDATE", OracleType.DateTime).Value = DateTime.Now;
                    cmd.Parameters.Add("pREMARKS", OracleType.VarChar).Value = values.REMARKS;
                    cmd.Parameters.Add("pAPPVERSION", OracleType.VarChar).Value = values.APPVERSION;
                    if (values.METERSTATUS.Trim().Equals("true"))
                        cmd.Parameters.Add("pMETERSTATUS", OracleType.VarChar).Value = "Metered";
                    else if (values.METERSTATUS.Trim().Equals("false"))
                        cmd.Parameters.Add("pMETERSTATUS", OracleType.VarChar).Value = "Repaired";
                    else
                        cmd.Parameters.Add("pMETERSTATUS", OracleType.VarChar).Value = values.METERSTATUS.Trim();

                    cmd.Parameters.Add("pREVIEWFLAG", OracleType.Number).Value = DBNull.Value;
                    cmd.Parameters.Add("pREVIEWEDBY", OracleType.VarChar).Value = string.Empty;
                    cmd.Parameters.Add("pREVIEWEDDATE", OracleType.Timestamp).Value = DBNull.Value;
                    cmd.Parameters.Add("pCANREVIEWEDBYAMR", OracleType.Number).Value = DBNull.Value;
                    cmd.Parameters.Add("pREVIEWREMARKSIMAGE", OracleType.VarChar).Value = DBNull.Value;
                    cmd.Parameters.Add("pREVIEWREMARKSREADING", OracleType.VarChar).Value = DBNull.Value;

                    con.Open();
                    saveResult = cmd.ExecuteNonQuery();
                    if (saveResult > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                log.InfoFormat("Exception {0}", ex.ToString());
                log.Error(ex.ToString());
                throw ex;
            }
            finally { con.Close(); }

        }

        private bool CheckDuplicateCAN(string can)
        {
            try
            {
                con.Open();
                cmd = new OracleCommand(@"SELECT * FROM TBL_AMR_CAPTUREMETER_READINGS WHERE CAN=:pCAN AND TRUNC(CAPTUREDDATE)=:pCapturedDate", con);
                OracleParameter param1 = new OracleParameter();
                param1.ParameterName = ":pCAN";
                param1.Value = can;
                cmd.Parameters.Add(param1);
                OracleParameter param2 = new OracleParameter();
                param2.ParameterName = ":pCapturedDate";
                param2.Value = System.DateTime.Now.ToString("dd-MMM-yyyy");
                cmd.Parameters.Add(param2);
                OracleDataReader reader = cmd.ExecuteReader();
                dt = new DataTable();
                dt.Load(reader);
                if (dt.Rows.Count > 0)
                    return false;
                else
                    return true;
            }
            catch (Exception)
            {
                throw;
            }
            finally { con.Close(); }
        }

        private string GenerateReferenceNoforCaptureMeterReading()
        {
            try
            {
                string month = DateTime.Now.ToString("yyyy-MM");
                if (con.State == ConnectionState.Closed)
                {
                    con.Open();
                }
                OracleCommand illcmd = new OracleCommand("SELECT MAX(REFSLNO) AS FORMID FROM TBL_AMR_CAPTUREMETER_READINGS WHERE REFMONTH='" + refMonthid + "'", con);
                OracleDataReader illdr;
                illdr = illcmd.ExecuteReader();
                int num;
                int num1;
                if (illdr.Read() == true)
                {
                    if (System.DBNull.Value == illdr["FormID"])
                    {

                        num = 1;
                        refSlno = num;
                        //referenceNo = month + num;
                        referenceNo = month + "-" + num;
                        // FormID = num.ToString();
                    }
                    else
                    {
                        object splobject = illcmd.ExecuteOracleScalar();
                        string insreslut = splobject.ToString();
                        num1 = Convert.ToInt32(insreslut);
                        num1 = num1 + 1;
                        refSlno = num1; ;
                        //referenceNo = month + num1;
                        referenceNo = month + "-" + num1;
                    }
                }

                return referenceNo;
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }

        }

        private string SaveCapturMeterReadingImagePath(string base64, string can, string referenceNo)
        {
            if (!string.IsNullOrEmpty(base64))
            {
                path = HttpContext.Current.Server.MapPath("~/CapturedMeterReadingImages/" + System.DateTime.Now.ToString("MMM-yyyy"));
                imageName = Images(base64, path, can, referenceNo);
                return imageName;
            }
            else
            {
                return string.Empty;
            }
        }

        public string generate_random()
        {
            Random rnd = new Random();
            int r = rnd.Next(9999);

            string strRandom = r.ToString();

            while (strRandom.ToString().Length < 4)
            {
                strRandom = "0" + strRandom;
            }
            return strRandom;
        }

        public DataTable GetData(string query)
        {
            cmd = new OracleCommand(query);
            using (OracleDataAdapter oda = new OracleDataAdapter())
            {
                cmd.Connection = con;
                oda.SelectCommand = cmd;
                using (DataTable dt = new DataTable())
                {
                    oda.Fill(dt);
                    return dt;
                }
            }
        }
    }
}