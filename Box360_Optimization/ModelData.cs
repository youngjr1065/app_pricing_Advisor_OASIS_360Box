using ILOG.Concert;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Box360_Optimization
{
    public class InputData
    {
        public string OHUB { get; set; }
        public string DHUB { get; set; }
        public int TotalBidVol { get; set; }
        public int LaneMinVol { get; set; }
        public int LaneMaxVol { get; set; }
        public double LaneCost { get; set; }
        public double LaneRevenue { get; set; }
        public double WinPct { get; set; }
        public int HubMiles { get; set; }
        public int ScenarioID { get; set; }
        public double LaneWeeklyTrailerTurns { get; set; }
        public int AvailableBidVol { get; set; }
        public int LaneEmptyVol { get; set; }
        public int IMLaneRank { get; set; }
        public double OHUBExpectedMT { get; set; }
        public double DHUBExpectedMT { get; set; }

    }

    public class OutputData : InputData
    {
        public int OptLoadedMoves { get; set; }
        public int OptEmptyMoves { get; set; }
        public double ReducedCost { get; set; }
        public double UpperObjectCoef { get; set; }
        public double LowerObjectCoef { get; set; }
        public double LaneProfit { get; set; }
        public int SolutionIndex { get; set; }
        public string Lane { get { return OHUB + "_" + DHUB; } }
        public int LaneID { get; set; }
        public INumVar LoadedMoves { get; set; }
        public INumVar EmptyMoves { get; set; }
        public INumVar LoadeMovesNL { get; set; }
        public INumVar EmptyMovesNL { get; set; }
    }

    public class AllModelData
    {
        public Dictionary<Int32, OutputData> ModelData = new Dictionary<int, OutputData>();

        public void AddModelData(Int32 LaneID, OutputData DataRecord)
        {
            ModelData.Add(LaneID, DataRecord);
        }
    }

    public class ScenarioDataFields
    {
        public int ScenarioID { get; set; }
        public int TotalTrailers { get; set; }
        public string ScenarioName { get; set; }
        public string ScenarioDescription { get; set; }
        public double WinPercent { get; set; }
        public double IMPenalty { get; set; }
        public int MinLOH { get; set; }
        public int ALL_to_DH_Vol { get; set; }
        public int AA_TO_DD_Vol { get; set; }
        public int Complete { get; set; }
    }

        public class ModelingParameters
    {
        public string ScenarioInputTable { get; set; }
        public string InterimSolutionInputTable { get; set; }
        public string ScenarioNonLinearSolutionTable { get; set; }
        public string ScenarioSolutionTable { get; set; }
        public string ScenarioTable { get; set; }
        public int Minimum_Lane_Volume { get; set; }

        //CTOR Builds with default Modeling Parameters/Tables
        public ModelingParameters()
        {
            ScenarioInputTable = "JENG077.BOX360_MASTER_SCENARIO_TEST";
            InterimSolutionInputTable = "JENG077.BOX360_SOL_DAT_INTERIM";
            ScenarioSolutionTable = "JENG077.BOX360_SOL_DAT_TEST";
            ScenarioTable = "JENG077.BOX360_SCENARIO_TEST";
            ScenarioNonLinearSolutionTable = "JENG077.BOX360_SOL_DAT_NONLINEAR";
            Minimum_Lane_Volume = 150;

        }
    }
}
