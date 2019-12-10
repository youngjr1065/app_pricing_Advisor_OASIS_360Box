using System;
using ILOG.Concert;
using ILOG.CPLEX;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using Common;

namespace Box360_Optimization
{
    public enum InputColumns
    {
        OHUB,
        DHUB,
        MT_VOLUME,
        HUB_MILES,
        AVL_BID_VOL,
        WEEKLY_TURNS,
        TTL_BID_VOL,
        MODEL_MIN,
        MODEL_MAX,
        LANE_COST,
        LANE_REVENUE,
        SCENARIO_ID,
        WIN_PERCENT,
        IMD_RANK
    }

    public enum ModelType
    {
        NonLinear,
        Linear
    }

    public class Modeler
        {
            private INumVar[] LoadedMoves;
            private INumVar[] EmptyMoves;
            private AllModelData theData = new AllModelData();
            public Cplex theModel;
            private double[] TrailerTurns;
            private double[] LaneRevenue;
            private double[] LaneCost;
            private double[] LaneMargin;
            private Int32 scenario;
            private string inputTable;
            private ILinearNumExpr lCap;
            private Int32 trailerCapacity;
            private Int32 i = 0;
            private Int32 inputrecs = 0;
            private ModelingParameters tableSelection;
            private Dictionary<int, string> HubList = new Dictionary<int, string>();

            #region Constructors
            public Modeler()
            {

            }

            //CTOR to build initial Non-Linear model.
            public Modeler(int ScenarioID, ModelingParameters modelTableSelection, Int32 TrailerCapacity)
            {
                inputTable = modelTableSelection.ScenarioInputTable;
                scenario = ScenarioID;
                tableSelection = modelTableSelection;
                GetRecordCount(); //populates inputrecs
                EmptyMoves = new INumVar[inputrecs];
                LoadedMoves = new INumVar[inputrecs];
                TrailerTurns = new double[inputrecs];
                LaneCost = new double[inputrecs];
                LaneRevenue = new double[inputrecs];
                LaneMargin = new double[inputrecs];
                theModel = new Cplex();
                theModel.Name = "Box360";
                trailerCapacity = TrailerCapacity;
                BuildInputData();
                AddMinVol_OrConstraint(modelTableSelection.Minimum_Lane_Volume);
                AddObjective();
                AddCapacityConstraint();
                AddBalanceConstraint();
            }

            //CTOR to build final Linear Model
            public Modeler(AllModelData LinearModel, ModelingParameters modelTableSelection, Cplex cplex, Int32 TrailerCapacity)
            {
                trailerCapacity = TrailerCapacity;
                theModel = cplex;
                tableSelection = modelTableSelection;
                inputrecs = GetLPInputRecordCount();
                scenario = LinearModel.ModelData.ElementAt(0).Value.ScenarioID;
                EmptyMoves = new INumVar[inputrecs];
                LoadedMoves = new INumVar[inputrecs];
                TrailerTurns = new double[inputrecs];
                LaneCost = new double[inputrecs];
                LaneRevenue = new double[inputrecs];
                LaneMargin = new double[inputrecs];

                //NEED TO WRITE PREVIOUS SOLUTION TO INTERIM TABLE.
                ClearTable(tableSelection.InterimSolutionInputTable);
                PopulateSolution(modelTableSelection.InterimSolutionInputTable);
                BuildLPInputData(tableSelection.Minimum_Lane_Volume);

                //NEED TO REBUILD MODEL BASED UPON NON-LINEAR SOLUTION
                AddObjective();
                AddBalanceConstraint();
                AddCapacityConstraint();
            }

            //CTOR for solution table writing
            public Modeler(Int32 Scenario, AllModelData ModelData, ModelingParameters TableSelection, Cplex cplex)
            {
                tableSelection = TableSelection;
                scenario = Scenario;
                theData = ModelData;
                theModel = cplex;
            }
            #endregion

            #region ModelComponents

            public void AddCapacityConstraint()
            {
                theModel.AddLe(theModel.Sum(theModel.ScalProd(TrailerTurns, LoadedMoves), theModel.ScalProd(TrailerTurns, EmptyMoves)), (trailerCapacity * 52), "Capacity");
            }

            public void WritedModelToFile(string PathName)
            {
                theModel.ExportModel(PathName);
            }

            public void AddBalanceConstraint()
            {
                lCap = theModel.LinearNumExpr();

                foreach (var hub in HubList)
                {
                    foreach (var lane in theData.ModelData)
                    {
                        if (hub.Value == lane.Value.OHUB)
                        {
                            lCap.AddTerm(1, lane.Value.LoadedMoves);
                            lCap.AddTerm(1, lane.Value.EmptyMoves);
                        }
                        if (hub.Value == lane.Value.DHUB)
                        {
                            lCap.AddTerm(-1, lane.Value.LoadedMoves);
                            lCap.AddTerm(-1, lane.Value.EmptyMoves);
                        }
                    }
                    theModel.AddEq(lCap, 0, hub.Value);
                    lCap.Clear();
                }
            }

            public void AddMinVol_OrConstraint(int MinVol)
            {
                for (int i = 0; i < EmptyMoves.Length; i++)
                {
                    theModel.Add(theModel.Or(theModel.Ge(EmptyMoves[i], MinVol), theModel.Ge(EmptyMoves[i], 0)));
                }
                for (int i = 0; i < LoadedMoves.Length; i++)
                {
                    theModel.Add(theModel.Or(theModel.Ge(LoadedMoves[i], MinVol), theModel.Ge(LoadedMoves[i], 0)));
                }
            }

            public void AddObjective()
            {
                theModel.AddMaximize(theModel.Diff(theModel.ScalProd(LaneMargin, LoadedMoves), theModel.ScalProd(LaneCost, EmptyMoves)));
            }
            #endregion

            public bool OptimizeModel()
            {
                try
                {
                    if (theModel.Solve())
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (System.Exception)
                {
                    //theModel.GetStatus().ToString();
                    throw;
                }
            }

            public Cplex FinalizeModel()
            {
                return theModel;
            }

            public AllModelData GetModelData()
            {

                return theData;
            }

            private void GetRecordCount()
            {
                using (OleDbConnection conn = GetConnection.BIPSConn)
                {
                    using (OleDbCommand cmd = new OleDbCommand(SQLCommands.GetInputCount(inputTable,scenario), conn))
                    {
                        using (OleDbDataReader reader = cmd.ExecuteReader())
                        {
                            reader.Read();
                            inputrecs = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        }
                    }
                }
            }

            public void ClearTable(string TableName)
            {
                using (OleDbConnection conn = GetConnection.BIPSConn)
                {
                    using (OleDbCommand cmd = new OleDbCommand(
                        "DELETE FROM " + TableName + ";", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            private void ClearTable(string TableName, int Scenario)
            {
                using (OleDbConnection conn = GetConnection.BIPSConn)
                {
                    using (OleDbCommand cmd = new OleDbCommand(
                        "DELETE FROM " + TableName + "WHERE SCENARIO_ID =" + Scenario + ";", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            public void UpdateCompleteScenario(int Scenario, string TableName)
            {
                using (OleDbConnection conn = GetConnection.BIPSConn)
                {
                    using (OleDbCommand cmd = new OleDbCommand(SQLCommands.UpdateScenarioTableComplete(tableSelection.ScenarioTable,scenario), conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            private void BuildInputData()
            {
                i = 0;
                using (OleDbConnection conn = GetConnection.BIPSConn)
                {
                    using (OleDbCommand cmd = new OleDbCommand(SQLCommands.GetInputSQL(inputTable, scenario), conn))
                    {
                        using (OleDbDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                OutputData outputData = new OutputData();
                                outputData.OHUB = reader.IsDBNull((int)InputColumns.OHUB) ? "" : reader.GetString((int)InputColumns.OHUB);
                                outputData.HubMiles = reader.IsDBNull((int)InputColumns.HUB_MILES) ? 0 : reader.GetInt32((int)InputColumns.HUB_MILES);
                                outputData.LaneEmptyVol = reader.IsDBNull((int)InputColumns.MT_VOLUME) ? 0 : reader.GetInt32((int)InputColumns.MT_VOLUME);
                                outputData.AvailableBidVol = reader.IsDBNull((int)InputColumns.AVL_BID_VOL) ? 0 : reader.GetInt32((int)InputColumns.AVL_BID_VOL);
                                outputData.IMLaneRank = reader.IsDBNull((int)InputColumns.IMD_RANK) ? 0 : reader.GetInt32((int)InputColumns.IMD_RANK);
                                outputData.LaneRevenue = reader.IsDBNull((int)InputColumns.LANE_REVENUE) ? 0 : (double)reader.GetDecimal((int)InputColumns.LANE_REVENUE);
                                outputData.LaneCost = reader.IsDBNull((int)InputColumns.LANE_COST) ? 0 : (double)reader.GetDecimal((int)InputColumns.LANE_COST);
                                outputData.LaneProfit = outputData.LaneRevenue - outputData.LaneCost;
                                outputData.LaneWeeklyTrailerTurns = reader.IsDBNull((int)InputColumns.WEEKLY_TURNS) ? 0 : (double)reader.GetDecimal((int)InputColumns.WEEKLY_TURNS);
                                outputData.LaneMinVol = reader.IsDBNull((int)InputColumns.MODEL_MIN) ? 0 : reader.GetInt32((int)InputColumns.MODEL_MIN);
                                outputData.LaneMaxVol = reader.IsDBNull((int)InputColumns.MODEL_MIN) ? 0 : reader.GetInt32((int)InputColumns.MODEL_MIN);
                                outputData.TotalBidVol = reader.IsDBNull((int)InputColumns.TTL_BID_VOL) ? 0 : reader.GetInt32((int)InputColumns.TTL_BID_VOL);
                                outputData.LaneID = i;
                                outputData.ScenarioID = scenario;
                                outputData.DHUB = reader.IsDBNull((int)InputColumns.DHUB) ? "" : reader.GetString((int)InputColumns.DHUB);
                                LoadedMoves[i] = theModel.NumVar(0, outputData.AvailableBidVol, NumVarType.Float, "LM_" + outputData.Lane);
                                outputData.LoadedMoves = LoadedMoves[i];
                                EmptyMoves[i] = theModel.NumVar(0, outputData.LaneEmptyVol, NumVarType.Float, "EM_" + outputData.Lane);
                                outputData.EmptyMoves = EmptyMoves[i];
                                theData.AddModelData(i, outputData);
                                LaneRevenue[i] = outputData.LaneRevenue;
                                LaneCost[i] = outputData.LaneCost;
                                TrailerTurns[i] = outputData.LaneWeeklyTrailerTurns;
                                LaneMargin[i] = LaneRevenue[i] - LaneCost[i];
                                i++;
                            }
                        }
                    }
                    using (OleDbCommand cmd = new OleDbCommand(SQLCommands.GetHubListSQL(inputTable,scenario), conn))
                    {
                        using (OleDbDataReader reader = cmd.ExecuteReader())
                        {
                            i = 0;
                            while (reader.Read())
                            {
                                HubList.Add(i, reader.IsDBNull(0) ? "" : reader.GetString(0));
                                i++;
                            }
                        }
                    }
                }
            }

            private int GetLPInputRecordCount()
            {
                int thecount;
                using (OleDbConnection conn = GetConnection.BIPSConn)
                {
                    using (OleDbCommand cmd = new OleDbCommand(SQLCommands.GetNewLPRecordCount(tableSelection.ScenarioSolutionTable), conn))
                    {
                        using (OleDbDataReader reader = cmd.ExecuteReader())
                        {
                            reader.Read();
                            thecount = reader.GetInt32(0);
                        }
                    }
                }
                return thecount;
            }
            private void BuildLPInputData(int Min_Vol)
            {
                using (OleDbConnection conn = GetConnection.BIPSConn)
                {
                    using (OleDbCommand cmd = new OleDbCommand(SQLCommands.GetNewLPInput(inputTable,scenario,tableSelection.ScenarioNonLinearSolutionTable), conn))
                    {
                        using (OleDbDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                OutputData outputData = new OutputData();
                                outputData.OHUB = reader.IsDBNull((int)InputColumns.OHUB) ? "" : reader.GetString((int)InputColumns.OHUB);
                                outputData.HubMiles = reader.IsDBNull((int)InputColumns.HUB_MILES) ? 0 : reader.GetInt32((int)InputColumns.HUB_MILES);
                                outputData.LaneEmptyVol = reader.IsDBNull((int)InputColumns.MT_VOLUME) ? 0 : reader.GetInt32((int)InputColumns.MT_VOLUME);
                                outputData.AvailableBidVol = reader.IsDBNull((int)InputColumns.AVL_BID_VOL) ? 0 : reader.GetInt32((int)InputColumns.AVL_BID_VOL);
                                outputData.IMLaneRank = reader.IsDBNull((int)InputColumns.IMD_RANK) ? 0 : reader.GetInt32((int)InputColumns.IMD_RANK);
                                outputData.LaneRevenue = reader.IsDBNull((int)InputColumns.LANE_REVENUE) ? 0 : (double)reader.GetDecimal((int)InputColumns.LANE_REVENUE);
                                outputData.LaneCost = reader.IsDBNull((int)InputColumns.LANE_COST) ? 0 : (double)reader.GetDecimal((int)InputColumns.LANE_COST);
                                outputData.LaneProfit = outputData.LaneRevenue - outputData.LaneCost;
                                outputData.LaneWeeklyTrailerTurns = reader.IsDBNull((int)InputColumns.WEEKLY_TURNS) ? 0 : (double)reader.GetDecimal((int)InputColumns.WEEKLY_TURNS);
                                outputData.LaneMinVol = reader.IsDBNull((int)InputColumns.MODEL_MIN) ? 0 : reader.GetInt32((int)InputColumns.MODEL_MIN);
                                outputData.LaneMaxVol = reader.IsDBNull((int)InputColumns.MODEL_MIN) ? 0 : reader.GetInt32((int)InputColumns.MODEL_MIN);
                                outputData.TotalBidVol = reader.IsDBNull((int)InputColumns.TTL_BID_VOL) ? 0 : reader.GetInt32((int)InputColumns.TTL_BID_VOL);
                                outputData.LaneID = i;
                                outputData.ScenarioID = scenario;
                                outputData.DHUB = reader.IsDBNull((int)InputColumns.DHUB) ? "" : reader.GetString((int)InputColumns.DHUB);
                                LoadedMoves[i] = theModel.NumVar(Min_Vol, outputData.AvailableBidVol, NumVarType.Float, "LM_" + outputData.Lane);
                                outputData.LoadedMoves = LoadedMoves[i];
                                EmptyMoves[i] = theModel.NumVar(0, outputData.LaneEmptyVol, NumVarType.Float, "EM_" + outputData.Lane);
                                outputData.EmptyMoves = EmptyMoves[i];
                                theData.AddModelData(i, outputData);
                                LaneRevenue[i] = outputData.LaneRevenue;
                                LaneCost[i] = outputData.LaneCost;
                                TrailerTurns[i] = outputData.LaneWeeklyTrailerTurns;
                                LaneMargin[i] = LaneRevenue[i] - LaneCost[i];
                                i++;
                            }
                        }
                    }
                }
            }

            public void PopulateSolution(string TheSolutionTable)
            {
                //ModelInputData modelInputData = new ModelInputData(scenario, tableSelection);
                using (OleDbConnection conn = GetConnection.BIPSConn)
                {
                    using (OleDbCommand cmd = new OleDbCommand(SQLCommands.WriteToInterimSolutionTable(TheSolutionTable), conn))
                    {
                        //using (OleDbDataReader reader = cmd.ExecuteReader())
                        {
                            cmd.Parameters.Add("@OHUB", OleDbType.VarChar, 10);           //0
                            cmd.Parameters.Add("@DHUB", OleDbType.VarChar, 10);           //1
                            cmd.Parameters.Add("@LOADED_MOVE", OleDbType.Integer, 4);     //2
                            cmd.Parameters.Add("@EMPTY_MOVE", OleDbType.Integer, 4);      //3
                            cmd.Parameters.Add("@LOADED_REV", OleDbType.Decimal);         //4
                            cmd.Parameters[4].Precision = 15;
                            cmd.Parameters[4].Scale = 4;
                            cmd.Parameters.Add("@LOADED_CST", OleDbType.Decimal);         //5
                            cmd.Parameters[5].Precision = 15;
                            cmd.Parameters[5].Scale = 4;
                            cmd.Parameters.Add("@RATE_SENSITIVITY", OleDbType.Double);    //6
                            cmd.Parameters.Add("@EMPTY_CST", OleDbType.Decimal);          //7
                            cmd.Parameters[7].Precision = 15;
                            cmd.Parameters[7].Scale = 4;
                            cmd.Parameters.Add("@AVL_BID_VOL", OleDbType.Integer, 4);     //8
                            cmd.Parameters.Add("@TRANSIT_TIME", OleDbType.Decimal);       //9
                            cmd.Parameters[9].Precision = 15;
                            cmd.Parameters[9].Scale = 4;
                            cmd.Parameters.Add("@RATE", OleDbType.Decimal);               //10
                            cmd.Parameters[10].Precision = 15;
                            cmd.Parameters[10].Scale = 4;
                            cmd.Parameters.Add("@LOAD_MILES", OleDbType.Integer, 4);      //11
                            cmd.Parameters.Add("@RED_COST", OleDbType.Decimal);           //12
                            cmd.Parameters[12].Precision = 15;
                            cmd.Parameters[12].Scale = 4;
                            cmd.Parameters.Add("@SOL_INDEX", OleDbType.Integer, 4);       //13
                            cmd.Parameters.Add("@LOAD_PROFIT", OleDbType.Decimal);        //14
                            cmd.Parameters[14].Precision = 15;
                            cmd.Parameters[14].Scale = 4;
                            cmd.Parameters.Add("@MODEL_MIN_VOLUME", OleDbType.Integer, 4);    //15
                            cmd.Parameters.Add("@MODEL_MAX_VOLUME", OleDbType.Integer, 4);    //16
                            cmd.Parameters.Add("@OBJECT_COEF_LOWER", OleDbType.Double);       //17
                            cmd.Parameters.Add("@OBJECT_COEF_UPPER", OleDbType.Double);       //18
                            cmd.Parameters.Add("@SCENARIO_ID", OleDbType.Integer, 4);         //19
                            cmd.Prepare();

                            foreach (var lane in theData.ModelData)
                            {
                                cmd.Parameters[0].Value = lane.Value.OHUB;
                                cmd.Parameters[1].Value = lane.Value.DHUB;
                                cmd.Parameters[2].Value = theModel.GetValue(lane.Value.LoadedMoves);
                                cmd.Parameters[3].Value = theModel.GetValue(lane.Value.EmptyMoves);
                                cmd.Parameters[4].Value = lane.Value.LaneRevenue;
                                cmd.Parameters[5].Value = lane.Value.LaneCost;
                                cmd.Parameters[6].Value = lane.Value.ReducedCost;
                                cmd.Parameters[7].Value = lane.Value.LaneCost;
                                cmd.Parameters[8].Value = lane.Value.AvailableBidVol;
                                cmd.Parameters[9].Value = lane.Value.LaneWeeklyTrailerTurns;
                                cmd.Parameters[10].Value = lane.Value.WinPct;
                                cmd.Parameters[11].Value = lane.Value.HubMiles;
                                cmd.Parameters[12].Value = lane.Value.ReducedCost;
                                cmd.Parameters[13].Value = lane.Value.SolutionIndex;
                                cmd.Parameters[14].Value = lane.Value.LaneProfit;
                                cmd.Parameters[15].Value = lane.Value.LaneMinVol;
                                cmd.Parameters[16].Value = lane.Value.LaneMaxVol;
                                cmd.Parameters[17].Value = lane.Value.LowerObjectCoef;
                                cmd.Parameters[18].Value = lane.Value.UpperObjectCoef;
                                cmd.Parameters[19].Value = lane.Value.ScenarioID;
                                cmd.ExecuteNonQuery();
                            }

                        }
                    }
                }
            }
       
    }
}
