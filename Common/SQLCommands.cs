using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class SQLCommands
    {
        
        #region SQLStrings

        public static string GetInputSQL(string TheInputTable, int ScenarioID)
        {
            string theSQL;

            theSQL = "SELECT OHUB " +                     //0
                            ", DHUB " +                          //1
                            ", LANE_MT_VOL " +                   //2
                            ", HUB_MIL " +                       //3
                            ", AVL_BID_VOL " +                   //4
                            ", TRT_DAY_Q_BSE " +                 //5
                            ", TOTAL_BID_VOL " +                 //6
                            ", MODEL_MIN_VOL " +                 //7
                            ", MODEL_MAX_VOL " +                 //8
                            ", LANE_COST " +                     //9
                            ", LANE_REV " +                      //10
                            ", SCENARIO_ID " +                   //11
                            ", WIN_PCT " +                       //12
                            ", IMD_RANK " +                      //13
                            "FROM " + TheInputTable + " " +
                            "WHERE SCENARIO_ID=" + ScenarioID + " " +
                            "ORDER BY AVL_BID_VOL DESC;";
            return theSQL;

        }

        public static string GetNewLPInput(string TheInputTable, int ScenarioID, string TheSolutionTable)
        {
            string theSQL = "SELECT SD.OHUB " +             //0
                            ", SD.DHUB " +                  //1
                            ", INP.LANE_MT_VOL " +          //2
                            ", INP.HUB_MIL " +              //3
                            ", SD.AVL_BID_VOL " +           //4
                            ", SD.TRT_DUR " +               //5
                            ", INP.TOTAL_BID_VOL " +        //6
                            ", MAX(SD.MODEL_MAX_VOLUME) AS MAX_VOL " +      //7
                            ", MAX(SD.MODEL_MIN_VOLUME) AS MIN_VOL " +      //8
                            ", SD.LDD_CST " +               //9
                            ", SD.LDD_REV " +               //10
                            ", SD.SCENARIO_ID " +           //11
                            ", INP.WIN_PCT " +              //12
                            ",INP.IMD_RANK " +              //13
                            "FROM " + TheSolutionTable + " SD " +
                            "INNER JOIN " + TheInputTable + " INP " +
                            "ON((SD.DHUB = INP.DHUB) " +
                            "AND(SD.OHUB = INP.OHUB) " +
                            "AND(SD.SCENARIO_ID = INP.SCENARIO_ID)) " +
                            "WHERE (COALESCE(SD.LDD_MV, 0) + COALESCE(SD.EMT_MV, 0)) > 0 " +
                            "AND SD.SCENARIO_ID = " + ScenarioID + " " +
                            "GROUP BY SD.OHUB " +
                            ", SD.DHUB              " +
                            ", INP.LANE_MT_VOL      " +
                            ", INP.HUB_MIL          " +
                            ", SD.AVL_BID_VOL       " +
                            ", SD.TRT_DUR           " +
                            ", INP.TOTAL_BID_VOL    " +
                            ", SD.LDD_CST           " +
                            ", SD.LDD_REV           " +
                            ", SD.SCENARIO_ID       " +
                            ", INP.WIN_PCT          " +
                            ",INP.IMD_RANK; ";

            return theSQL;
        }

        public static string GetNewLPRecordCount(string TheSolutionTable)
        {
            string theSQL = "SELECT COUNT(*) FROM " + TheSolutionTable + " WHERE (COALESCE(LDD_MV, 0) + COALESCE(EMT_MV, 0)) > 0;";

            return theSQL;
        }

        public static string GetHubListSQL(string TheInputTable, int ScenarioID)
        {
            string theSQL;

            theSQL = "SELECT DISTINCT OHUB FROM  " + TheInputTable +
                     " WHERE SCENARIO_ID = " + ScenarioID +
                            " UNION " +
                            "SELECT DISTINCT DHUB FROM " + TheInputTable +
                            " WHERE SCENARIO_ID = " + ScenarioID + " ";

            return theSQL;
        }

        public static string GetInputCount(string TheInputTable, int ScenarioID)
        {
            string theSQL = "SELECT Count(*) FROM " + TheInputTable + " WHERE SCENARIO_ID =" + ScenarioID + " ";

            return theSQL;
        }

        public static string WriteToInterimSolutionTable(string TheSolutionTable)
        {
            string theSQL = "INSERT INTO " + TheSolutionTable + " (OHUB, " +                        //0
                                "                               DHUB, " +                           //1
                                "                               LDD_MV, " +                         //2
                                "                               EMT_MV, " +                         //3
                                "                               LDD_REV, " +                        //4
                                "                               LDD_CST, " +                        //5
                                "                               RATE_SENSITIVITY, " +               //6
                                "                               EMT_CST, " +                        //7
                                "                               AVL_BID_VOL, " +                    //8
                                "                               TRT_DUR, " +                        //9
                                "                               AVG_RAT, " +                        //10
                                "                               LOAD_MILES, " +                     //11
                                "                               RED_COST, " +                       //12
                                "                               SOL_INDEX, " +                      //13
                                "                               LOAD_PROFIT, " +                    //14
                                "                               MODEL_MIN_VOLUME, " +               //15
                                "                               MODEL_MAX_VOLUME, " +               //16
                                "                               OBJECT_COEF_LOWER, " +              //17
                                "                               OBJECT_COEF_UPPER, " +              //18
                                "                               SCENARIO_ID) " +                    //19
                                "VALUES (?, " +
                                "        ?, " +
                                "        ?, " +
                                "        ?, " +
                                "        ?, " +
                                "        ?, " +
                                "        ?, " +
                                "        ?, " +
                                "        ?, " +
                                "        ?, " +
                                "        ?, " +
                                "        ?, " +
                                "        ?, " +
                                "        ?, " +
                                "        ?, " +
                                "        ?, " +
                                "        ?, " +
                                "        ?, " +
                                "        ?, " +
                                "        ?)";

            return theSQL;
        }

        public static string UpdateScenarioTableComplete(string TheScenarioTable, int ScenarioID)
        {
            //DateTime completeTime = DateTime.Now;
            string theSQL = "UPDATE " + TheScenarioTable + " " +
                "SET COMPLETE = 1 " +
                ",COMPLETE_DATE_TIME = '" + DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") +
                "' WHERE SCENARIO_ID = " + ScenarioID + ";";

            return theSQL;

        }

        public static string GetScenarioData(string ScenarioTable)
        {
            string theSQL = "SELECT SCENARIO_ID " +             //0
                        ", TRIM(SCENARIO_NAME) " +          //1
                        ", SCENARIO_DESCRIPTION " +         //2
                        ", COMPLETE " +                     //3
                        ", MODEL_TRAILERS " +               //4
                        ", COMPLETE_DATE_TIME " +           //5
                        ", WIN_PERCENT " +                  //6
                        ", IM_PENALTY " +                   //7
                        ", ALL_TO_DH_MINVOL " +             //8
                        ", ALL_ALL_TO_DH_DH_MINVOL " +      //9
                        ", MIN_LOH " +                      //10
                        "FROM " + ScenarioTable + " ORDER BY SCENARIO_ID";
            return theSQL;
        }
        #endregion
    }
}
