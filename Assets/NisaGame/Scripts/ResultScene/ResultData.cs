using System;

[Serializable]
public class ResultDatayou
{
    public int maxYear;

    public int finalAsset;
    public int totalPrincipal;
    public int totalProfit;

    public int[] yearAssets0toMax;      // 長さ maxYear+1
    public int[][] monthlyAssets0toMax; // monthlyAssets0toMax[y] は長さ13
}
