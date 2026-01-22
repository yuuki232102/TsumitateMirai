using System;

[Serializable]
public class ResultData
{
    public int maxYear;

    public int finalAsset;
    public int totalPrincipal;
    public int totalProfit;

    public int[] yearAssets0toMax;      // ’·‚³ maxYear+1
    public int[][] monthlyAssets0toMax; // monthlyAssets0toMax[y] ‚Í’·‚³13
}
